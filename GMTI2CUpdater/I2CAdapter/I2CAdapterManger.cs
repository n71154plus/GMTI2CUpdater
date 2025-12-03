using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace GMTI2CUpdater.I2CAdapter
{
    static class I2CAdapterManger
    {
        public static List<I2CAdapterBase> GetAvailableDisplays()
        {
            var list = new List<I2CAdapterBase>();

            // 1. nVIDIA NVAPI
            try
            {
                using var nvidia = new Hardware.NvidiaApi();
                var nvList = nvidia.GetAvailableDisplays();
                list.AddRange(nvList.Select(nv => new NvidiaI2CAdapter(nv)).ToList());
            }
            catch
            {
                // ignore NVAPI error
            }

            // 2. Intel IGCL
            try
            {
                using var igcl = new Hardware.IntelIGCLApi();
                var igclList = igcl.GetAvailableDisplays();
                list.AddRange(igclList.Select(cl => new IntelIGCLI2CAdapter(cl)).ToList());
            }
            catch
            {
                // ignore IGCL error
            }
            try
            {
                using var igfx = new Hardware.IntelIGFXApi();
                var igfxlList = igfx.GetAvailableDisplays();
                list.AddRange(igfxlList.Select(fx => new IntelIGFXI2CAdapter(fx)).ToList());
            }
            catch
            {
                // ignore IGCL error
            }

            return list;
        }
    }
}
