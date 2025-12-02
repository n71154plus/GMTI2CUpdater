using CommunityToolkit.Mvvm.ComponentModel;

namespace GMTI2CUpdater
{
    public partial class HexByteViewModel : ObservableObject
    {
        public HexByteViewModel(int offset, byte value, bool isDiff)
        {
            Offset = offset;
            Value = value;
            this.isDiff = isDiff;
        }

        /// <summary>
        /// 在整個 Data 陣列中的索引 (0-based)
        /// </summary>
        public int Offset { get; }

        /// <summary>
        /// 該位元組的實際值（如果有的話）
        /// </summary>
        public byte Value { get; }

        /// <summary>
        /// 正常情況底下的十六進位文字（兩位，補 0）
        /// </summary>
        public string HexText => Value.ToString("X2");

        // 來自 IntelHex 沒有描述的位址 → 未定義
        [ObservableProperty]
        private bool isUndefined;

        // 是否與比對來源不同
        [ObservableProperty]
        private bool isDiff;

        /// <summary>
        /// 給 UI 用的顯示文字：未定義顯示 "XX"，否則顯示 HexText
        /// </summary>
        public string DisplayText => IsUndefined ? "XX" : HexText;
    }
}
