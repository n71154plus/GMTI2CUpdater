using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMTI2CUpdater.I2CAdapter.Unlock
{
    internal class NT71837 : TCONUnlockBase
    {
        private readonly I2CAdapterBase adapter;
        public override string Name => "Novatek NT71837";
        public NT71837(I2CAdapterBase adapter) : base(adapter)
        {
            this.adapter = adapter;
        }
        public override void Unlock(byte deviceAddress = 0x00)
        {
            adapter.WriteDpcd(0x0102, [0x00]);
            adapter.WriteI2CByteIndex(0xC8, 0x10, [0x01]);
            adapter.WriteI2CByteIndex(0xC8, 0x29, [0x00]);
            adapter.WriteI2CByteIndex(0xC8, 0x04, [0x80]);
            adapter.WriteDpcd(0x0102, [0xC0]);
        }

        public override void Lock(byte deviceAddress = 0x00)
        {

        }


    }
}
