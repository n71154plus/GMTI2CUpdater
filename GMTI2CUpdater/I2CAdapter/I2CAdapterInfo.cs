using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GMTI2CUpdater.I2CAdapter
{
    /// <summary>
    /// 描述可用 I2C / AUX 介面的基本資訊與來源屬性。
    /// </summary>
    public class I2CAdapterInfo
    {
        /// <summary>
        /// 介面顯示名稱，通常供 UI 列表呈現使用。
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 更完整的描述文字，例如裝置型號或來源驅動。
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 指出存取此介面是否需要系統管理員權限。
        /// </summary>
        public bool IsNeedPrivilege { get; set; }

        /// <summary>
        /// 介面來源是否為顯示器（而非 GPU 或其他匯流排）。
        /// </summary>
        public bool IsFromDisplay { get; set; }

        /// <summary>
        /// 由底層驅動提供的顯示器或裝置控制代碼。
        /// </summary>
        public IntPtr DisplayHandle;

        /// <summary>
        /// 監視器的唯一識別碼，供比對或同步用途。
        /// </summary>
        public uint MonitorUid { get; set; }

        /// <summary>
        /// GPU 裝置索引，對應底層 API 的裝置序號。
        /// </summary>
        public int DeviceIndex;

        /// <summary>
        /// 介面在裝置上的輸出序號，例如連接埠編號。
        /// </summary>
        public int OutputIndex;

        /// <summary>
        /// 便於偵錯或 UI 呈現的文字格式，預設回傳 <see cref="Name"/>。
        /// </summary>
        public override string ToString()
        {
            return Name;
        }
    }
}
