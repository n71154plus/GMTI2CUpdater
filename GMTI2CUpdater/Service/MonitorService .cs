using GMTI2CUpdater.Helper;
using System;
using Microsoft.Win32;


namespace GMTI2CUpdater.Service
{
    /// <summary>
    /// 監聽螢幕連線狀態變化，並透過事件通知訂閱者。
    /// </summary>
    public class MonitorService : IDisposable
    {
        /// <summary>
        /// 目前偵測到的螢幕數量。
        /// </summary>
        private int _monitorCount;
        /// <summary>
        /// 螢幕數量變更事件，布林值代表是否為新增。
        /// </summary>
        public event Action<bool>? MonitorChanged;

        /// <summary>
        /// 建構服務並立即記錄初始螢幕數，同時訂閱系統事件。
        /// </summary>
        public MonitorService()
        {
            _monitorCount = MonitorHelper.GetMonitorCount();
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

        /// <summary>
        /// 系統顯示設定變更時觸發，計算新舊數量並發出事件。
        /// </summary>
        private void OnDisplaySettingsChanged(object sender, EventArgs e)
        {
            int newCount = MonitorHelper.GetMonitorCount();
            if (newCount != _monitorCount)
            {
                bool added = newCount > _monitorCount;
                MonitorChanged?.Invoke(added);
                _monitorCount = newCount;
            }
        }

        /// <summary>
        /// 解除事件訂閱，避免資源洩漏。
        /// </summary>
        public void Dispose()
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        }
    }
}
