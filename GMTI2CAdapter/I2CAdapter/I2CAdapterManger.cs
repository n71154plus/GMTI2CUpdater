using System.Collections.Generic;
using System.Linq;
using GMTI2CUpdater.I2CAdapter.Hardware;

namespace GMTI2CUpdater.I2CAdapter
{
    /// <summary>
    /// 集中處理各家顯示卡 I2C 介面的列舉邏輯。
    /// </summary>
    public static class I2CAdapterManger
    {
        public static List<I2CAdapterBase> GetAvaiableI2CAdapter()
        {
            var list = new List<I2CAdapterBase>();
            var usbi2clist = GetAvaiableUsbI2CAdapter();
            var displaylist = GetAvailableDisplays();
            list.AddRange(usbi2clist);
            list.AddRange(displaylist);
            return list;
        }

        public static List<I2CAdapterBase> GetAvaiableUsbI2CAdapter()
        {
            var list = new List<I2CAdapterBase>();
            try
            {
                if (HidDevice.Exists(0x04B4, 0xF232))
                {
                    list.Add(new Cy8C24894Adapter(
                        new I2CAdapterInfo
                        {
                            Name = "Usb I2C Adapter:CY8C24894",
                            IsFromDisplay = false,
                            IsNeedPrivilege = false,
                        },
                        0x04B4,
                        0xF232
                    ));
                }
            }
            catch
            {
                //ignore Usb I2C Adapter:CY8C24894 Error
            }

            return list;
        }

        /// <summary>
        /// 嘗試從 NVIDIA 與 Intel API 取得可用的顯示介面並組成清單。
        /// </summary>
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
