using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMTI2CUpdater.I2CAdapter
{
    public class I2CAdapterInfo
    {
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool IsNeedPrivilege { get; set; }
        public bool IsFromDisplay { get; set; }
        public IntPtr DisplayHandle;

        public uint MonitorUid { get; set; }
        public int DeviceIndex;
        public int OutputIndex;

        public override string ToString()
        {
            return Name;
        }
    }
}
