using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMTI2CUpdater.I2CAdapter.Unlock
{
    internal class Analogix : TCONUnlockBase
    {
        private readonly I2CAdapterBase adapter;
        public override string Name => "Analogix Common";
        public Analogix(I2CAdapterBase adapter) : base(adapter)
        {
            this.adapter = adapter;
        }
        public override void Unlock(byte deviceAddress = 0x00)
        {
            adapter.WriteDpcd(0x04F5, [0x41,0x56,0x4F,0x20,0x16]);
            adapter.WriteDpcd(0x04F0, [0x0E, 0x00, 0x00, 0x30, 0x09]);

        }

        public override void Lock(byte deviceAddress = 0x00)
        {

        }


    }
}
