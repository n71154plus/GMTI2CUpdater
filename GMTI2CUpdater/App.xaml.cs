using GMTI2CUpdater.Service;
using System.Configuration;
using System.Data;
using System.Windows;

namespace GMTI2CUpdater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// 單例監視器服務，用來偵測螢幕變化並通知 ViewModel 重新整理可用的顯示介面。
        /// </summary>
        private MonitorService? _monitorService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // 初始化監視器偵測服務並建立主要的 ViewModel 與視窗。
            _monitorService = new MonitorService();

            var vm = new MainWindowViewModel(_monitorService);
            var win = new MainWindow { DataContext = vm };
            win.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 確保退出時釋放監視器事件訂閱。
            _monitorService?.Dispose();
            base.OnExit(e);
        }
    }


}
