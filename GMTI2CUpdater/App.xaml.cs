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
        private MonitorService _monitorService;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _monitorService = new MonitorService();

            var vm = new MainWindowViewModel(_monitorService);
            var win = new MainWindow { DataContext = vm };
            win.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _monitorService?.Dispose();
            base.OnExit(e);
        }
    }


}
