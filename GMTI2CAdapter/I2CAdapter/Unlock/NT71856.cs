using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMTI2CUpdater.I2CAdapter.Unlock
{
    internal class NT71856 : TCONUnlockBase
    {
        private readonly I2CAdapterBase adapter;
        public override string Name => "Novatek NT71856";
        public NT71856(I2CAdapterBase adapter) : base(adapter)
        {
            this.adapter = adapter;
        }
        public override void Unlock(byte deviceAddress = 0x00)
        {
            byte[] mode = adapter.ReadDpcd(0x04C1, 1);
            mode[0] |= 0x04;
            mode[0] &= 0xF7;
            adapter.WriteDpcd(0x04C1, mode);
            adapter.WriteDpcd(0x0102, [0xC0]);
            byte[] reg0204 = adapter.ReadI2CUInt16Index(0xC0,0x0204, 1);
            reg0204[0] &= 0xF0;
            adapter.WriteI2CUInt16Index(0xC0, 0x0204, reg0204);
        }

        public override void Lock(byte deviceAddress = 0x00)
        {
            byte[] mode = adapter.ReadDpcd(0x04C1, 1);
            mode[0] |= 0x0C;
            adapter.WriteDpcd(0x04C1, mode);
        }


    }
}
