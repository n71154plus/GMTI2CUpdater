using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace GMTI2CUpdater
{
    public partial class HexLineViewModel : ObservableObject
    {
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
