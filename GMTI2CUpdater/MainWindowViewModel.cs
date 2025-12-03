using System.IO;
using System.Globalization;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GMTI2CUpdater.I2CAdapter;
using System.Windows;
using GMTI2CUpdater.Helper;
using GMTI2CUpdater.Service;
using GMTI2CUpdater.I2CAdapter.Unlock;
using System.Windows.Markup;
using System;

namespace GMTI2CUpdater
{
    public partial class MainWindowViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool hasMonitorChanged;
        [ObservableProperty]
        private string monitorStatus;




        // ====== 這些會自動產生 public 屬性 ======
        [ObservableProperty] private bool adviceNeedAdmin;
        [ObservableProperty] private bool isAdmin;
        [ObservableProperty] private I2CAdapterBase selectedAdapter;
        [ObservableProperty] private List<TCONUnlockBase> tCONUnlockBases;
        [ObservableProperty] private TCONUnlockBase selectedAdaptertCONUnlock;

        [ObservableProperty] private byte[] deviceAddressCollection;
        [ObservableProperty] private byte selectedDeviceAddress;

        [ObservableProperty] private byte deviceAddress;
        [ObservableProperty] private int totalSize;
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

        [ObservableProperty] private List<I2CAdapterBase> adapterInfos;

        // Log 集合本身用 ObservableCollection 即可
        public ObservableCollection<string> LogItems { get; } = new();

        public MainWindowViewModel(MonitorService monitorSvc)
        {
            // 初始一般設定
            //SelectedBus = "I2C-1";
            monitorSvc.MonitorChanged += OnMonitorChanged;
            IsAdmin = PrivilegeHelper.IsRunAsAdministrator();
            BaseAddress = 0;

            HexFileName = "";
            FillMode = "0x00";
            CustomFillValue = "0x00";

            IsSyncScrollEnabled = true;
            ShowDiffWithBefore = false;
            ShowDiffWithTarget = false;

            I2CStatusText = "";
            StatusMessage = "Ready";
            Progress = 0;
            InitDeviceAddressCollection();
            //InitDemoDataCore();
            RefreshMonitor();
            var ini = new IniFile("config.ini");
            string HexFile = ini.Get("Target", "Filename", "");
            if (File.Exists(HexFile))
            {
                LoadHexFromFile(HexFile);
            }
        }
        private void InitDeviceAddressCollection()
        {
            DeviceAddressCollection = new byte[128];
            for (byte i = 0; i < 128; i++)
            {
                DeviceAddressCollection[i] = (byte)(i << 1);
            }
            var ini = new IniFile("config.ini");
            byte? deviceaddressfromIni = HexHelper.ParseHexByteOrNull(ini.Get("I2CSpec", "DeviceAddress", "0x00"));
            if (deviceaddressfromIni != null)
                SelectedDeviceAddress = deviceaddressfromIni.Value;
        }
        private void OnMonitorChanged(bool added)
        {
            RefreshMonitor();
        }
        private void RefreshMonitor()
        {
            AdapterInfos = I2CAdapterManger.GetAvailableDisplays();
            if (AdapterInfos.Count > 0)
                SelectedAdapter = AdapterInfos[0];
        }
        partial void OnSelectedAdapterChanged(I2CAdapterBase value)
        {
            UpdateAdviceNeedAdmin();
            UpdateUnlockTcon();
        }
        private void UpdateAdviceNeedAdmin()
        {
            // 假設 abc 是 bool，如果是 AdapterInfo.IsNeedPrivilege 就改那個
            AdviceNeedAdmin = (SelectedAdapter?.AdapterInfo.IsNeedPrivilege ?? false) && !IsAdmin;
            // 例：AdviceNeedAdmin = (SelectedAdapter?.AdapterInfo?.IsNeedPrivilege ?? false) && IsAdmin;
        }
        private void UpdateUnlockTcon()
        {
            if (SelectedAdapter == null)
                return;
            TCONUnlockBases = new List<TCONUnlockBase>() {new Parade(SelectedAdapter),new NT71837(SelectedAdapter), new NT71856(SelectedAdapter) };
            var ini = new IniFile("config.ini");
            string TCON = ini.Get("Adapter", "TCON", "");
            SelectedAdaptertCONUnlock = TCONUnlockBases.FirstOrDefault(t => t.Name == TCON);
        }
        // ====== Commands（會自動產生 XxxCommand 屬性）======

        // 對應 XAML: ReadBeforeCommand
        [RelayCommand]
        private void ReadBefore()
        {
            byte deviceaddress = SelectedDeviceAddress;
            if (TotalSize <= 0)
            {
                Log($"請先在Target欄位讀取Hex檔以便設定需要讀取的位址與長度");
                return;
            }
            if (SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock == null)
            {
                Log($"使用I2C over Aux，但未選擇解鎖指令");
                return;
            }
            try
            {
                if (SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock != null)
                    SelectedAdaptertCONUnlock.Unlock(deviceaddress);
                if ((SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock != null) || !SelectedAdapter.AdapterInfo.IsFromDisplay )
                    BeforeData = SelectedAdapter.ReadI2CByteIndex(deviceaddress, (byte)BaseAddress, TotalSize);
                if (SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock != null)
                    SelectedAdaptertCONUnlock.Lock(deviceaddress);
            }
            catch (Exception ex)
            {
                Log($"ReadBefore 失敗：{ex.Message}");
                return;
            }
            Log($"ReadBefore 成功");
            BeforeChecksum = CalculateChecksum(BeforeData, TargetDefinedMap);
            BeforeRangeText = FormatRange(BaseAddress, BeforeData.Length);
            //InitDemoDataCore();
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
                TotalSize = size;

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
            var ini = new IniFile("config.ini");
            byte? I2CUnLockIndex = HexHelper.ParseHexByteOrNull(ini.Get("I2CSpec", "I2CUnLockIndex", "Null"));
            byte? I2CUnlockCMD = HexHelper.ParseHexByteOrNull(ini.Get("I2CSpec", "I2CUnlockCMD", "Null"));
            byte? I2CLockIndex = HexHelper.ParseHexByteOrNull(ini.Get("I2CSpec", "I2CLockIndex", "Null"));
            byte? I2ClockCMD = HexHelper.ParseHexByteOrNull(ini.Get("I2CSpec", "I2CLockCMD", "Null"));
            Progress = 0;
            if (TotalSize <= 0)
            {
                Log($"請先在Target欄位讀取Hex檔以便設定需要讀取的位址與長度");
                return;
            }
            if (SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock == null)
            {
                Log($"使用I2C over Aux，但未選擇解鎖指令");
                return;
            }
            byte deviceaddress = SelectedDeviceAddress;
            try
            {


                if (SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock != null)
                    SelectedAdaptertCONUnlock.Unlock(deviceaddress);
                if ((SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock != null) || !SelectedAdapter.AdapterInfo.IsFromDisplay)
                {
                    if (I2CUnLockIndex.HasValue && I2CUnlockCMD.HasValue)
                        SelectedAdapter.WriteI2CByteIndex(deviceaddress, I2CUnLockIndex.Value, [I2CUnlockCMD.Value]);
                    WriteDiffBytes(SelectedAdapter, deviceaddress, (byte)BaseAddress, BeforeData, TargetData);
                    if (I2CLockIndex.HasValue && I2ClockCMD.HasValue)
                        SelectedAdapter.WriteI2CByteIndex(deviceaddress, I2CLockIndex.Value, [I2ClockCMD.Value]);
                }

                if (SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock != null)
                    SelectedAdaptertCONUnlock.Lock(deviceaddress);
            }
            catch (Exception ex)
            {
                Log($"Update 失敗：{ex.Message}");
                return;
            }
            Log($"Update 成功");
            Progress = 100;
        }
        public void WriteDiffBytes(I2CAdapterBase adapter,
            byte deviceAddress,
            byte baseAddress,
            byte[] beforeData,
            byte[] targetData)
        {
            if (adapter == null) throw new ArgumentNullException(nameof(adapter));
            if (beforeData == null){
                Log("beforeData 為 null，無法進行差異寫入，請先讀取目前資料");
                return;
            }
            if (targetData == null) { 
                Log("targetData 為 null，無法進行差異寫入，，請先載入欲更新的資料");
                return;
            }
            if (beforeData.Length != targetData.Length)
            {
                Log("beforeData 與 targetData 長度不一致，無法進行差異寫入");
                return;
            }
                

            int length = beforeData.Length;

            int runStartIndex = -1; // 目前連續差異區段的起始 index（在陣列裡）
            for (int i = 0; i < length; i++)
            {
                bool isDiff = beforeData[i] != targetData[i] && TargetDefinedMap[i];

                if (isDiff)
                {
                    // 發現差異，且目前沒有正在累積的區段，就開一段
                    if (runStartIndex == -1)
                    {
                        runStartIndex = i;
                    }
                }
                else
                {
                    // 沒差異，且之前有正在累積的區段 -> 把那一段送出去
                    if (runStartIndex != -1)
                    {
                        int runLength = i - runStartIndex;

                        // 計算這一段在 I2C 上的起始位址
                        byte startAddress = (byte)(baseAddress + runStartIndex);

                        // 把 targetData 中這一段差異內容複製出來
                        byte[] buffer = new byte[runLength];
                        Array.Copy(targetData, runStartIndex, buffer, 0, runLength);

                        Log($"準備寫入Device Address:{deviceAddress:X2} Index:{startAddress:X2} Data: {BitConverter.ToString(buffer)}");
                        // ✅ 一次寫入一整段連續的差異
                        adapter.WriteI2CByteIndex(deviceAddress, startAddress, buffer);

                        // 關閉這段
                        runStartIndex = -1;
                    }
                }
            }

            // 處理「最後一段恰好延伸到結尾」的情況
            if (runStartIndex != -1)
            {
                int runLength = length - runStartIndex;
                byte startAddress = (byte)(baseAddress + runStartIndex);

                byte[] buffer = new byte[runLength];
                Array.Copy(targetData, runStartIndex, buffer, 0, runLength);
                Log($"準備寫入Device Address:{deviceAddress:X2} Index:{startAddress:X2} Data: {BitConverter.ToString(buffer)}");
                adapter.WriteI2CByteIndex(deviceAddress, startAddress, buffer);
            }
        }

        // 對應 XAML: ReadAfterCommand
        [RelayCommand]
        private void ReadAfter()
        {
            byte deviceaddress = SelectedDeviceAddress;
            if (TotalSize <= 0)
            {
                Log($"請先在Target欄位讀取Hex檔以便設定需要讀取的位址與長度");
                return;
            }
            if (SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock == null)
            {
                Log($"使用I2C over Aux，但未選擇解鎖指令");
                return;
            }
            try
            {
                if (SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock != null)
                    SelectedAdaptertCONUnlock.Unlock(deviceaddress);
                if ((SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock != null) || !SelectedAdapter.AdapterInfo.IsFromDisplay)
                    AfterData = SelectedAdapter.ReadI2CByteIndex(deviceaddress, (byte)BaseAddress, TotalSize);
                if (SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock != null)
                    SelectedAdaptertCONUnlock.Lock(deviceaddress);
            }
            catch (Exception ex)
            {
                Log($"ReadBefore 失敗：{ex.Message}");
                return;
            }
            AfterChecksum = CalculateChecksum(AfterData, TargetDefinedMap);
            AfterRangeText = FormatRange(BaseAddress, AfterData.Length);

            Log($"ReadBefore 成功");
        }

        // 對應 XAML: CompareCommand
        [RelayCommand]
        private void Compare()
        {
            CompareTargetAndAfterCore();
        }
        [RelayCommand]
        private void WriteEEPROM()
        {
            var ini = new IniFile("config.ini");
            byte? WriteEEPROMIndex = HexHelper.ParseHexByteOrNull(ini.Get("I2CSpec", "WriteEEPROMIndex", "Null"));
            byte? WriteEEPROMCMD = HexHelper.ParseHexByteOrNull(ini.Get("I2CSpec", "WriteEEPROMCMD", "Null"));
            byte? I2CUnLockIndex = HexHelper.ParseHexByteOrNull(ini.Get("I2CSpec", "I2CUnLockIndex", "Null"));
            byte? I2CUnlockCMD = HexHelper.ParseHexByteOrNull(ini.Get("I2CSpec", "I2CUnlockCMD", "Null"));
            byte? I2CLockIndex = HexHelper.ParseHexByteOrNull(ini.Get("I2CSpec", "I2CLockIndex", "Null"));
            byte? I2ClockCMD = HexHelper.ParseHexByteOrNull(ini.Get("I2CSpec", "I2CLockCMD", "Null"));
            //byte ResetEEPROMIndex = HexHelper.ParseHexByte(ini.Get("I2CSpec", "ResetEEPROMIndex", "Null"));
            //byte ResetEEPROMCMD = HexHelper.ParseHexByte(ini.Get("I2CSpec", "ResetEEPROMCMD", "Null"));

            if (WriteEEPROMIndex == null || WriteEEPROMCMD == null)
            {
                Log($"請先在config.ini設定燒錄的Command參數");
                return;
            }

            if (SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock == null)
            {
                Log($"使用I2C over Aux，但未選擇解鎖指令");
                return;
            }
            byte deviceaddress = SelectedDeviceAddress;
            try
            {
                if (SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock != null)
                    SelectedAdaptertCONUnlock.Unlock(deviceaddress);
                if (WriteEEPROMIndex.HasValue && WriteEEPROMCMD.HasValue &&(SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock != null) || !SelectedAdapter.AdapterInfo.IsFromDisplay)
                {
                    if (I2CUnLockIndex.HasValue && I2CUnlockCMD.HasValue)
                        SelectedAdapter.WriteI2CByteIndex(deviceaddress, I2CUnLockIndex.Value, [I2CUnlockCMD.Value]);
                    SelectedAdapter.WriteI2CByteIndex(deviceaddress, WriteEEPROMIndex.Value, [WriteEEPROMCMD.Value]);
                    if (I2CLockIndex.HasValue && I2ClockCMD.HasValue)
                        SelectedAdapter.WriteI2CByteIndex(deviceaddress, I2CLockIndex.Value, [I2ClockCMD.Value]);
                }
                if (SelectedAdapter.AdapterInfo.IsFromDisplay && SelectedAdaptertCONUnlock != null)
                    SelectedAdaptertCONUnlock.Lock(deviceaddress);
            }
            catch (Exception ex)
            {
                Log($"燒錄 失敗：{ex.Message}");
                return;
            }
            Log($"燒錄 成功");
        }
        [RelayCommand]
        private void ApplyAll()
        {
            Log($"Step1：讀取Hex檔");
            var ini = new IniFile("config.ini");
            string HexFile = ini.Get("Target", "Filename", "");
            if (File.Exists(HexFile))
            {
                LoadHexFromFile(HexFile);
            }
            else
            {
                LoadHex();
            }
            Log($"Step2：讀取目前資料");
            ReadBefore();
            Log($"Step3：更新資料");
            Update();
            Log($"Step4：回讀資料");
            ReadAfter();
            if (CompareTargetAndAfterCore())
            {
                Log($"Step5：執行燒錄");
                WriteEEPROM();
            }
            else
            {
                MessageBox.Show("Verify失敗，請確認Target與After資料一致後再進行燒錄","警告",MessageBoxButton.OK,MessageBoxImage.Warning);
            }


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
        /// 模擬「載入 HEX」：以 BeforeData 為基礎改幾個 Byte
        /// </summary>
        private void GenerateTargetFromBeforeCore()
        {

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
        private bool CompareTargetAndAfterCore()
        {
            if (TargetData == null || AfterData == null)
            {
                Log("Compare 失敗：Target 或 After 為 null，請先完成模擬載入 & 回讀。");
                return false;
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
                return true;
            }
            else
            {
                Log($"Compare: 共 {diffCount} 個位元組不一致。");
                StatusMessage = "Verify Failed (Demo)";
                return false;
            }
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
