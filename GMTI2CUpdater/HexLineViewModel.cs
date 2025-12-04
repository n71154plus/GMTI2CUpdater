using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GMTI2CUpdater
{
    /// <summary>
    /// 表示 Hex 檢視中的一行資料，包含該行位址與 16 個位元組的呈現模型。
    /// </summary>
    public partial class HexLineViewModel : ObservableObject
    {
        /// <summary>
        /// 建立新的資料行並初始化位址與位元組集合。
        /// </summary>
        /// <param name="address">該行開頭的絕對位址。</param>
        public HexLineViewModel(int address)
        {
            Address = address;
            Bytes = new ObservableCollection<HexByteViewModel>();
        }

        /// <summary>
        /// 此行開頭的實際位址
        /// </summary>
        public int Address { get; }

        /// <summary>
        /// 顯示用位址文字，例如 0000, 0010
        /// </summary>
        public string AddressText => Address.ToString("X4");

        /// <summary>
        /// 這一行的 bytes（最多 16 個）
        /// </summary>
        public ObservableCollection<HexByteViewModel> Bytes { get; }
    }
}
