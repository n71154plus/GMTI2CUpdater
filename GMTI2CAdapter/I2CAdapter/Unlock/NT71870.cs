using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMTI2CUpdater.I2CAdapter.Unlock
{
    internal class NT71870 : TCONUnlockBase
    {
        private readonly I2CAdapterBase adapter;
        public override string Name => "Novatek NT71870";
        public NT71870(I2CAdapterBase adapter) : base(adapter)
        {
            this.adapter = adapter;
        }
        public override void Unlock(byte deviceAddress = 0x00)
        {
            adapter.WriteDpcd(0x0102, [0xC0]);
            adapter.WriteDpcd(0x048B, [0x18]);
            adapter.WriteI2CUInt16Index(0xC0, 0x0A26, [0xC1]);

        }

        public override void Lock(byte deviceAddress = 0x00)
        {
            adapter.WriteI2CUInt16Index(0xC0, 0x0A26, [0xC1]);
            adapter.WriteDpcd(0x048B, [0x00]);
            adapter.WriteDpcd(0x0102, [0x81]);
        }


    }
}
