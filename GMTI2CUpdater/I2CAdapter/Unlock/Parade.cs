using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMTI2CUpdater.I2CAdapter.Unlock
{
    internal class Parade : TCONUnlockBase
    {
        private readonly I2CAdapterBase adapter;
        public override string Name => "Parade Common";
        public Parade(I2CAdapterBase adapter) : base(adapter)
        {
            this.adapter = adapter;
        }



        public override void Lock(byte deviceAddress = 0x00)
        {
            adapter.WriteDpcd(0x0480, [0x50]);
            adapter.WriteDpcd(0x0480, [0x41]);
            adapter.WriteDpcd(0x0480, [0x52]);
            adapter.WriteDpcd(0x0480, [0x41]);
            adapter.WriteDpcd(0x0480, [0x44]);
            adapter.WriteDpcd(0x0480, [0x45]);
            adapter.WriteDpcd(0x0480, [0x2d]);
            adapter.WriteDpcd(0x0480, [0x46]);
            adapter.WriteDpcd(0x0480, [0x57]);
            adapter.WriteDpcd(0x0480, [0x2d]);
            adapter.WriteDpcd(0x0480, [0x44]);
            adapter.WriteDpcd(0x0480, [0x50]);
            adapter.WriteDpcd(0x0480, [0x00]);
            adapter.WriteDpcd(0x0480, [0x06]);
            adapter.WriteDpcd(0x0480, [0x03]);
            adapter.WriteDpcd(0x0480, [0x03]);

            adapter.WriteDpcd(0x0482, [0xC0]);
            adapter.WriteDpcd(0x048B, [0xE0]);
            adapter.WriteDpcd(0x048E, [0x00]);
            adapter.WriteDpcd(0x0480, [0x00]);
        }

        public override void Unlock(byte deviceAddress = 0x00)
        {
            adapter.WriteDpcd(0x0480, [0x50]);
            adapter.WriteDpcd(0x0480, [0x41]);
            adapter.WriteDpcd(0x0480, [0x52]);
            adapter.WriteDpcd(0x0480, [0x41]);
            adapter.WriteDpcd(0x0480, [0x44]);
            adapter.WriteDpcd(0x0480, [0x45]);
            adapter.WriteDpcd(0x0480, [0x2d]);
            adapter.WriteDpcd(0x0480, [0x46]);
            adapter.WriteDpcd(0x0480, [0x57]);

            adapter.WriteDpcd(0x0480, [0x2d]);
            adapter.WriteDpcd(0x0480, [0x44]);
            adapter.WriteDpcd(0x0480, [0x50]);
            adapter.WriteDpcd(0x0480, [0x00]);
            adapter.WriteDpcd(0x0480, [0x06]);
            adapter.WriteDpcd(0x0480, [0x03]);
            adapter.WriteDpcd(0x0480, [0x03]);
            byte status = adapter.ReadDpcd(0x480, 1)[0];
            adapter.WriteDpcd(0x048B, [0x90]);
            byte ddc = (byte)(deviceAddress | 0x01);
            adapter.WriteDpcd(0x048E, [ddc]);
        }
    }
}
