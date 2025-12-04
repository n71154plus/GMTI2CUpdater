using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace GMTI2CUpdater
{
    /// <summary>
    /// 以可捲動十六進位檢視呈現 byte 陣列，並支援多個控制項之間的同步捲動與差異顯示。
    /// </summary>
    public partial class HexViewControl : UserControl
    {
        private const int BytesPerLine = 16;

        public IReadOnlyList<string> ColumnHeaders { get; }
        private ScrollViewer _scrollViewer;
        private bool _isInternalScrollChange;
        private string _registeredGroupKey;

        // 捲動同步群組：Key = SyncScrollKey
        private static readonly Dictionary<string, List<WeakReference<HexViewControl>>> _syncGroups
            = new();

        /// <summary>
        /// 建構控制項，準備欄位標題與行集合並訂閱載入/卸載事件。
        /// </summary>
        public HexViewControl()
        {
            InitializeComponent();
            ColumnHeaders = Enumerable.Range(0, BytesPerLine)
                              .Select(i => i.ToString("X2"))
                              .ToList();
            Lines = new ObservableCollection<HexLineViewModel>();

            Loaded += HexViewControl_Loaded;
            Unloaded += HexViewControl_Unloaded;
        }

        /// <summary>
        /// 給 XAML 綁定用的行集合
        /// </summary>
        public ObservableCollection<HexLineViewModel> Lines { get; }

        #region 依賴屬性

        public byte[] Data
        {
            get => (byte[])GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
        public bool[] DefinedMap
        {
            get => (bool[])GetValue(DefinedMapProperty);
            set => SetValue(DefinedMapProperty, value);
        }

        public static readonly DependencyProperty DefinedMapProperty =
            DependencyProperty.Register(
                nameof(DefinedMap),
                typeof(bool[]),
                typeof(HexViewControl),
                new PropertyMetadata(null, OnDefinedMapChanged));

        private static void OnDefinedMapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (HexViewControl)d;
            control.RebuildLines();
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(byte[]),
                typeof(HexViewControl),
                new PropertyMetadata(null, OnDataChanged));

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (HexViewControl)d;
            control.RebuildLines();
        }

        /// <summary>
        /// 此視圖的基底位址（顯示在左邊位址欄）
        /// </summary>
        public int BaseAddress
        {
            get => (int)GetValue(BaseAddressProperty);
            set => SetValue(BaseAddressProperty, value);
        }

        public static readonly DependencyProperty BaseAddressProperty =
            DependencyProperty.Register(
                nameof(BaseAddress),
                typeof(int),
                typeof(HexViewControl),
                new PropertyMetadata(0, OnBaseAddressChanged));

        private static void OnBaseAddressChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (HexViewControl)d;
            control.RebuildLines();
        }

        /// <summary>
        /// 用來比對差異的資料來源，例如 Before / Target
        /// </summary>
        public byte[] HighlightDiffSource
        {
            get => (byte[])GetValue(HighlightDiffSourceProperty);
            set => SetValue(HighlightDiffSourceProperty, value);
        }

        public static readonly DependencyProperty HighlightDiffSourceProperty =
            DependencyProperty.Register(
                nameof(HighlightDiffSource),
                typeof(byte[]),
                typeof(HexViewControl),
                new PropertyMetadata(null, OnDiffSourceChanged));

        private static void OnDiffSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (HexViewControl)d;
            control.RebuildLines();
        }

        /// <summary>
        /// 是否顯示與 HighlightDiffSource 的差異
        /// </summary>
        public bool ShowDiff
        {
            get => (bool)GetValue(ShowDiffProperty);
            set => SetValue(ShowDiffProperty, value);
        }

        public static readonly DependencyProperty ShowDiffProperty =
            DependencyProperty.Register(
                nameof(ShowDiff),
                typeof(bool),
                typeof(HexViewControl),
                new PropertyMetadata(false, OnShowDiffChanged));

        private static void OnShowDiffChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (HexViewControl)d;
            control.RebuildLines();
        }

        /// <summary>
        /// 同步捲動的群組 Key（同樣 Key 的 HexView 會同步）
        /// </summary>
        public string SyncScrollKey
        {
            get => (string)GetValue(SyncScrollKeyProperty);
            set => SetValue(SyncScrollKeyProperty, value);
        }

        public static readonly DependencyProperty SyncScrollKeyProperty =
            DependencyProperty.Register(
                nameof(SyncScrollKey),
                typeof(string),
                typeof(HexViewControl),
                new PropertyMetadata(null, OnSyncScrollKeyChanged));

        private static void OnSyncScrollKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (HexViewControl)d;
            control.UpdateSyncRegistration(e.OldValue as string, e.NewValue as string);
        }

        /// <summary>
        /// 是否啟用捲動同步
        /// </summary>
        public bool SyncScrollEnabled
        {
            get => (bool)GetValue(SyncScrollEnabledProperty);
            set => SetValue(SyncScrollEnabledProperty, value);
        }

        public static readonly DependencyProperty SyncScrollEnabledProperty =
            DependencyProperty.Register(
                nameof(SyncScrollEnabled),
                typeof(bool),
                typeof(HexViewControl),
                new PropertyMetadata(false));

        #endregion

        #region 生命週期

        private void HexViewControl_Loaded(object sender, RoutedEventArgs e)
        {
            _scrollViewer = FindScrollViewer();
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += ScrollViewer_ScrollChanged;
            }

            if (!string.IsNullOrWhiteSpace(SyncScrollKey))
            {
                RegisterToGroup(SyncScrollKey);
            }

            RebuildLines();
        }

        private void HexViewControl_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged -= ScrollViewer_ScrollChanged;
            }

            if (!string.IsNullOrWhiteSpace(_registeredGroupKey))
            {
                UnregisterFromGroup(_registeredGroupKey);
                _registeredGroupKey = null;
            }
        }

        private ScrollViewer FindScrollViewer()
        {
            // 直接拿 XAML 上的 PART_ScrollViewer
            return PART_ScrollViewer;
        }

        #endregion

        #region 捲動同步

        /// <summary>
        /// 當其中一個控制項捲動時觸發，將捲動位置同步到同群組的其他控制項。
        /// </summary>
        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isInternalScrollChange)
                return;

            if (!SyncScrollEnabled)
                return;

            if (string.IsNullOrWhiteSpace(_registeredGroupKey))
                return;

            if (Math.Abs(e.VerticalChange) < double.Epsilon)
                return;

            SyncGroupScroll(_registeredGroupKey, this, e.VerticalOffset);
        }

        /// <summary>
        /// 處理 SyncScrollKey 變更時的註冊與取消註冊邏輯。
        /// </summary>
        private void UpdateSyncRegistration(string oldKey, string newKey)
        {
            if (!string.IsNullOrWhiteSpace(oldKey))
            {
                UnregisterFromGroup(oldKey);
            }

            if (!string.IsNullOrWhiteSpace(newKey))
            {
                RegisterToGroup(newKey);
            }
        }

        /// <summary>
        /// 將控制項加入指定的同步群組，避免重複加入相同實例。
        /// </summary>
        private void RegisterToGroup(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            _registeredGroupKey = key;

            if (!_syncGroups.TryGetValue(key, out var list))
            {
                list = new List<WeakReference<HexViewControl>>();
                _syncGroups[key] = list;
            }

            foreach (var wr in list)
            {
                if (wr.TryGetTarget(out var ctrl) && ReferenceEquals(ctrl, this))
                    return;
            }

            list.Add(new WeakReference<HexViewControl>(this));
        }

        /// <summary>
        /// 從同步群組移除控制項，並在群組為空時清理集合。
        /// </summary>
        private void UnregisterFromGroup(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (!_syncGroups.TryGetValue(key, out var list))
                return;

            list.RemoveAll(wr =>
            {
                if (!wr.TryGetTarget(out var ctrl))
                    return true;
                return ReferenceEquals(ctrl, this);
            });

            if (list.Count == 0)
            {
                _syncGroups.Remove(key);
            }
        }

        /// <summary>
        /// 將指定群組內其他控制項的捲動位置更新到同樣的偏移量。
        /// </summary>
        private static void SyncGroupScroll(string key, HexViewControl sender, double verticalOffset)
        {
            if (!_syncGroups.TryGetValue(key, out var list))
                return;

            for (int i = list.Count - 1; i >= 0; i--)
            {
                var wr = list[i];
                if (!wr.TryGetTarget(out var ctrl))
                {
                    list.RemoveAt(i);
                    continue;
                }

                if (ReferenceEquals(ctrl, sender))
                    continue;

                if (!ctrl.SyncScrollEnabled)
                    continue;

                if (ctrl._scrollViewer == null)
                    continue;

                ctrl._isInternalScrollChange = true;
                try
                {
                    ctrl._scrollViewer.ScrollToVerticalOffset(verticalOffset);
                }
                finally
                {
                    ctrl._isInternalScrollChange = false;
                }
            }
        }

        #endregion

        #region 行資料生成

        /// <summary>
        /// 根據 Data 與差異設定重新建立行與位元組的 ViewModel 集合。
        /// </summary>
        private void RebuildLines()
        {
            Lines.Clear();

            var data = Data;
            if (data == null || data.Length == 0)
            {
                System.Diagnostics.Debug.WriteLine("HexViewControl: Data is null or empty");
                return;
            }

            var highlight = HighlightDiffSource;
            bool showDiff = ShowDiff;
            int length = data.Length;
            int baseAddr = BaseAddress;

            var definedMap = DefinedMap; // 可能是 null

            for (int offset = 0; offset < length; offset += BytesPerLine)
            {
                int count = Math.Min(BytesPerLine, length - offset);
                var line = new HexLineViewModel(baseAddr + offset);

                for (int i = 0; i < count; i++)
                {
                    int index = offset + i;
                    byte value = data[index];

                    // true = 有定義；false = 未定義，要顯示 XX
                    bool isDefined = definedMap == null || (index < definedMap.Length && definedMap[index]);
                    bool isUndefined = !isDefined;

                    bool isDiff = false;
                    if (showDiff && highlight != null && isDefined) // 只有定義過的 byte 才做 diff
                    {
                        if (index >= highlight.Length)
                            isDiff = true;
                        else
                            isDiff = highlight[index] != value;
                    }

                    var vm = new HexByteViewModel(index, value, isDiff)
                    {
                        IsUndefined = isUndefined
                    };

                    line.Bytes.Add(vm);
                }

                Lines.Add(line);
            }

            System.Diagnostics.Debug.WriteLine($"HexViewControl: RebuildLines 完成，Lines.Count = {Lines.Count}");
        }


        #endregion
    }
}
