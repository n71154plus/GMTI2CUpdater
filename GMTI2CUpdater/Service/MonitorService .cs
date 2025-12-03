using GMTI2CUpdater.Helper;
using Microsoft.Win32;


namespace GMTI2CUpdater.Service
{
    public class MonitorService : IDisposable
    {
        private int _monitorCount;
        public event Action<bool> MonitorChanged;

        public MonitorService()
        {
            _monitorCount = MonitorHelper.GetMonitorCount();
            SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        }

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

        public void Dispose()
        {
            SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        }
    }
}
