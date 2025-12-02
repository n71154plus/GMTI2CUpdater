using System.IO;
using System.Globalization;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GMTI2CUpdater.I2CAdapter;
using System.Windows;

namespace GMTI2CUpdater
{
    public partial class MainWindowViewModel : ObservableObject
    {
        // ====== 這些會自動產生 public 屬性 ======
        [ObservableProperty] private I2CAdapterInfo selectedAdapter;
        [ObservableProperty] private string deviceAddress;
        [ObservableProperty] private string pageSize;
        [ObservableProperty] private string totalSize;
        [ObservableProperty] private int baseAddress;

        [ObservableProperty] private bool[] beforeDefinedMap;
        [ObservableProperty] private bool[] targetDefinedMap;
        [ObservableProperty] private bool[] afterDefinedMap;

        [ObservableProperty] private byte[] beforeData;
        [ObservableProperty] private byte[] targetData;
        [ObservableProperty] private byte[] afterData;

        [ObservableProperty] private string beforeChecksum;
        [ObservableProperty] private string targetChecksum;
        [ObservableProperty] private string afterChecksum;

        [ObservableProperty] private string beforeRangeText;
        [ObservableProperty] private string targetRangeText;
        [ObservableProperty] private string afterRangeText;

        [ObservableProperty] private bool isSyncScrollEnabled;
        [ObservableProperty] private bool showDiffWithBefore;
        [ObservableProperty] private bool showDiffWithTarget;

        [ObservableProperty] private string hexFileName;
        [ObservableProperty] private string fillMode;
        [ObservableProperty] private string customFillValue;

        [ObservableProperty] private string i2CStatusText;
        [ObservableProperty] private string statusMessage;
        [ObservableProperty] private double progress;

        [ObservableProperty] private List<I2CAdapterInfo> adapterInfos;

        // Log 集合本身用 ObservableCollection 即可
        public ObservableCollection<string> LogItems { get; } = new();

        public MainWindowViewModel()
        {
            // 初始一般設定
            //SelectedBus = "I2C-1";
            DeviceAddress = "0x50";
            PageSize = "16";
            TotalSize = "256";
            BaseAddress = 0;

            HexFileName = "(Demo 模擬)";
            FillMode = "0xFF";
            CustomFillValue = "FF";

            IsSyncScrollEnabled = true;
            ShowDiffWithBefore = false;
            ShowDiffWithTarget = false;

            I2CStatusText = "I2C: Demo 模式";
            StatusMessage = "Ready";
            Progress = 0;

            InitDemoDataCore();
            AdapterInfos = I2CAdapterManger.GetAvailableDisplays();
            if (AdapterInfos.Count > 0) 
                SelectedAdapter = AdapterInfos[0];
        }

        // ====== Commands（會自動產生 XxxCommand 屬性）======

        // 對應 XAML: ReadBeforeCommand
        [RelayCommand]
        private void ReadBefore()
        {
            MessageBox.Show(selectedAdapter.Name);
            InitDemoDataCore();
        }
        private byte GetFillByte()
        {
            // FillMode 可以是 "0xFF" / "0x00" / "自訂"，或你之後改成別的
            if (string.Equals(FillMode, "0xFF", StringComparison.OrdinalIgnoreCase))
                return 0xFF;

            if (string.Equals(FillMode, "0x00", StringComparison.OrdinalIgnoreCase))
                return 0x00;

            // 自訂十六進位值
            if (!string.IsNullOrWhiteSpace(CustomFillValue) &&
                byte.TryParse(CustomFillValue.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
                return b;

            // fallback
            return 0xFF;
        }
        public void LoadHexFromFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            try
            {
                var lines = File.ReadAllLines(filePath);

                // 解析 HEX
                var image = IntelHexParser.Parse(lines);

                HexFileName = Path.GetFileName(filePath);
                Log($"載入 HEX 檔：{HexFileName}，資料範圍 0x{image.MinAddress:X8} - 0x{image.MaxAddress:X8}");

                if (image.Data.Count == 0)
                {
                    StatusMessage = "HEX 檔沒有任何資料";
                    return;
                }

                // 根據 HEX 自動決定 BaseAddress / TotalSize
                int baseAddr = image.MinAddress;
                int size = image.MaxAddress - image.MinAddress + 1;

                BaseAddress = baseAddr;
                TotalSize = size.ToString(CultureInfo.InvariantCulture);

                // 準備 Target buffer & DefinedMap
                byte fill = GetFillByte();
                var buffer = new byte[size];
                var defined = new bool[size];

                for (int i = 0; i < size; i++)
                {
                    buffer[i] = fill;
                    defined[i] = false;   // 預設都是「未定義」 → HexView 顯示 XX
                }

                // 把 HEX 資料套進 buffer
                foreach (var kv in image.Data)
                {
                    int absoluteAddress = kv.Key;
                    int offset = absoluteAddress - baseAddr;

                    if (offset < 0 || offset >= size)
                        continue; // 理論上不會發生

                    buffer[offset] = kv.Value;
                    defined[offset] = true;   // 這個位址在 HEX 有描述
                }

                TargetData = buffer;
                TargetDefinedMap = defined;
                TargetRangeText = FormatRange(BaseAddress, buffer.Length);
                TargetChecksum = CalculateChecksum(TargetData, TargetDefinedMap);
                // Demo：讓 BeforeData 長度跟 HEX 一致，方便比對
                if (BeforeData == null || BeforeData.Length != buffer.Length)
                {
                    Log("BeforeData 長度與 HEX 影像不同，Demo 模式自動重新初始化 BeforeData 以配合 HEX 範圍。");
                    int oldBase = BaseAddress;
                    InitDemoDataCore();        // 使用目前 TotalSize 建出 BeforeData
                    BaseAddress = oldBase;
                    BeforeRangeText = FormatRange(BaseAddress, BeforeData.Length);
                }

                ShowDiffWithBefore = true;
                StatusMessage = "HEX 載入完成";
                Log($"TargetData 已建立 ({buffer.Length} bytes)，未在 HEX 描述的位址在畫面上會顯示為 'XX'。");
            }
            catch (Exception ex)
            {
                Log($"載入 HEX 失敗：{ex.Message}");
                StatusMessage = "HEX 載入失敗";
            }
        }

        // 對應 XAML: LoadHexCommand
        [RelayCommand]
        private void LoadHex()
        {
            var dlg = new OpenFileDialog
            {
                Filter = "Intel HEX (*.hex)|*.hex|All files (*.*)|*.*"
            };

            if (dlg.ShowDialog() == true)
            {
                LoadHexFromFile(dlg.FileName);
            }
        }



        // 對應 XAML: UpdateCommand
        [RelayCommand]
        private void Update()
        {
            SimulateUpdateCore();
        }

        // 對應 XAML: ReadAfterCommand
        [RelayCommand]
        private void ReadAfter()
        {
            SimulateReadAfterCore();
        }

        // 對應 XAML: CompareCommand
        [RelayCommand]
        private void Compare()
        {
            CompareTargetAndAfterCore();
        }

        // 對應 XAML: ClearLogCommand
        [RelayCommand]
        private void ClearLog()
        {
            LogItems.Clear();
        }

        // ====== 下面是 Demo 用的核心邏輯（private，不會產生 Command）======
        private bool[] CreateAllTrue(int length)
        {
            var arr = new bool[length];
            for (int i = 0; i < length; i++)
                arr[i] = true;
            return arr;
        }
        /// <summary>
        /// 初始化 BeforeData (模式: 0,1,2,...)
        /// </summary>
        private void InitDemoDataCore()
        {
            int size = GetTotalSizeOrDefault();
            var before = new byte[size];

            for (int i = 0; i < size; i++)
                before[i] = (byte)(i & 0xFF);

            BeforeData = before;
            BeforeDefinedMap = CreateAllTrue(size);   // 新增這行
            BeforeRangeText = FormatRange(BaseAddress, size);

            Log("初始化 BeforeData，模式: 0x00,0x01,...");
        }

        /// <summary>
        /// 模擬「載入 HEX」：以 BeforeData 為基礎改幾個 Byte
        /// </summary>
        private void GenerateTargetFromBeforeCore()
        {
            if (BeforeData == null || BeforeData.Length == 0)
                InitDemoDataCore();

            var size = BeforeData.Length;
            var target = new byte[size];
            Array.Copy(BeforeData, target, size);

            var defined = new bool[size]; // 預設全 false = 未定義 (XX)

            // Demo: 假裝 HEX 只描述 offset 0x20 ~ 0x2F 的 16 bytes
            for (int i = 0; i < 16 && (32 + i) < size; i++)
            {
                int index = 32 + i;
                target[index] ^= 0xFF;   // 隨便改一下值，為了看差異
                defined[index] = true;   // 這 16 個位置是「有在 HEX 描述」的
            }

            TargetData = target;
            TargetDefinedMap = defined;   // 重點

            TargetRangeText = FormatRange(BaseAddress, size);

            HexFileName = "(Demo) 模擬載入 HEX (只定義 0x20~0x2F)";
            ShowDiffWithBefore = true;

            Log("模擬載入 HEX：TargetData 只在 offset 0x20~0x2F 有定義，其餘顯示為 XX。");
        }


        /// <summary>
        /// 模擬「開始更新」：不實際寫 I2C，只是改狀態 + Progress
        /// </summary>
        private void SimulateUpdateCore()
        {
            if (TargetData == null)
            {
                Log("更新失敗：尚未有 TargetData，請先按『載入欲更新資料 (HEX)』。");
                return;
            }

            StatusMessage = "模擬寫入中...";
            Progress = 0;
            Log("開始模擬 Update：假裝將 TargetData 寫入 I2C 裝置。");

            // Demo: 直接跳 100%
            Progress = 100;
            StatusMessage = "模擬寫入完成";
            Log("模擬 Update 完成。");
        }

        /// <summary>
        /// 模擬「更新後回讀」：以 TargetData 為基礎，故意改 1 個 Byte
        /// </summary>
        private void SimulateReadAfterCore()
        {
            if (TargetData == null)
            {
                Log("回讀失敗：尚未有 TargetData，請先按『載入欲更新資料 (HEX)』。");
                return;
            }

            var size = TargetData.Length;
            var after = new byte[size];
            Array.Copy(TargetData, after, size);

            // Demo: 故意讓某一個位址有差異，例如 offset 0x40
            if (size > 0x40)
            {
                after[0x40] ^= 0x01;
                Log("模擬回讀 AfterData，並在 offset 0x0040 故意製造 1 個 Byte 差異。");
            }
            else
            {
                Log("模擬回讀 AfterData (資料過短，略過製造差異)。");
            }

            AfterData = after;
            AfterRangeText = FormatRange(BaseAddress, size);

            // Demo: 先假裝 After 跟 Target 一樣只知道同一段位址
            AfterDefinedMap = TargetDefinedMap != null ? (bool[])TargetDefinedMap.Clone() : null;

            ShowDiffWithTarget = true;
            StatusMessage = "ReadBack (Demo) 完成";

        }

        /// <summary>
        /// 比對 TargetData 與 AfterData，寫入 Log
        /// </summary>
        private void CompareTargetAndAfterCore()
        {
            if (TargetData == null || AfterData == null)
            {
                Log("Compare 失敗：Target 或 After 為 null，請先完成模擬載入 & 回讀。");
                return;
            }

            int size = Math.Min(TargetData.Length, AfterData.Length);
            int diffCount = 0;

            for (int i = 0; i < size; i++)
            {
                if (TargetData[i] != AfterData[i])
                {
                    diffCount++;
                    if (diffCount <= 10)
                    {
                        Log($"差異 {diffCount}: 位址 0x{BaseAddress + i:X4}, " +
                            $"Target=0x{TargetData[i]:X2}, After=0x{AfterData[i]:X2}");
                    }
                }
            }

            if (diffCount == 0)
            {
                Log("Compare: Target 與 After 完全一致 (Demo)。");
                StatusMessage = "Verify OK (Demo)";
            }
            else
            {
                Log($"Compare: 共 {diffCount} 個位元組不一致。");
                StatusMessage = "Verify Failed (Demo)";
            }
        }

        private int GetTotalSizeOrDefault()
        {
            if (int.TryParse(TotalSize, out var size) && size > 0)
                return size;

            return 256;
        }

        private string CalculateChecksum(byte[] data, bool[] defineMaps)
        {
            if (data == null || data.Length == 0 ||
                defineMaps == null || defineMaps.Length == 0 || 
                defineMaps.Length != data.Length)
                return "-";

            ushort sum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (!defineMaps[i])
                    continue;   // 未定義的位址不計入 checksum
                byte b = data[i];
                sum += b;
            }

            //short checksum = (sum & 0xFFFF);
            return $"0x{sum:X4}";
        }
        private string FormatRange(int baseAddr, int size)
        {
            if (size <= 0) return "-";
            int end = baseAddr + size - 1;
            return $"0x{baseAddr:X2}-0x{end:X2} ({size} bytes)";
        }

        private void Log(string message)
        {
            LogItems.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        }
    }
}
