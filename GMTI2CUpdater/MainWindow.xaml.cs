using System;
using System.Windows;
using System.Windows.Interop;
using System.Threading;
using GMTI2CUpdater.I2CAdapter.Hardware;

namespace GMTI2CUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private UsbInfoManager.UsbDeviceNotificationRegistration? _usbNotifier;
        /// <summary>
        /// 初始化主視窗並套用 XAML 定義的 UI 元件。
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            //DataContext = new MainWindowViewModel();
        }

        /// <summary>
        /// 拖曳檔案經過視窗時，判斷是否支援的副檔名並更新游標效果。
        /// </summary>
        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0)
                {
                    string ext = System.IO.Path.GetExtension(files[0]);
                    if (string.Equals(ext, ".hex", StringComparison.OrdinalIgnoreCase))
                    {
                        e.Effects = DragDropEffects.Copy;
                        e.Handled = true;
                        return;
                    }
                }
            }

            e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// 拖放 HEX 檔案到視窗時觸發，將檔案路徑傳遞給 ViewModel 進行載入。
        /// </summary>
        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
                return;

            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files == null || files.Length == 0)
                return;

            string filePath = files[0];
            string ext = System.IO.Path.GetExtension(filePath);
            if (!string.Equals(ext, ".hex", StringComparison.OrdinalIgnoreCase))
                return;

            if (DataContext is MainWindowViewModel vm)
            {
                vm.LoadHexFromFile(filePath);
            }
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var source = (HwndSource)PresentationSource.FromVisual(this)!;

            _usbNotifier = UsbInfoManager.RegisterUsbDeviceNotifications(source.Handle);
            source.AddHook(_usbNotifier.WndProc);

            _usbNotifier.UsbAttached += (s, path) =>
            {
                // 這裡就是 USB 插入事件
                // path 會是像 \\?\USB#VID_XXXX&PID_YYYY#... 這樣的裝置路徑
                Dispatcher.Invoke(() =>
                {
                    if (DataContext is MainWindowViewModel vm)
                    {
                        string lower = path.ToLowerInvariant();
                        if (lower.Contains("vid_04b4") && lower.Contains("pid_f232"))
                        {
                            Thread.Sleep(1000);
                            vm.RefreshUsbAdapter();
                            vm.Log("偵測到Usb I2C Adapter:CY8C24894 USB裝置Plug");
                        }

                    }

                });
            };

            _usbNotifier.UsbRemoved += (s, path) =>
            {
                // 這裡就是 USB 拔除事件
                Dispatcher.Invoke(() =>
                {
                    if (DataContext is MainWindowViewModel vm)
                    {
                        string lower = path.ToLowerInvariant();
                        if (lower.Contains("vid_04b4") && lower.Contains("pid_f232"))
                        {
                            Thread.Sleep(1000);
                            vm.RefreshUsbAdapter();
                            vm.Log("偵測到Usb I2C Adapter:CY8C24894 USB裝置UnPlug");
                        }

                    }
                });
            };
        }

        protected override void OnClosed(EventArgs e)
        {
            _usbNotifier?.Dispose();
            _usbNotifier = null;
            base.OnClosed(e);
        }

    }
}