using System.Windows;

namespace GMTI2CUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
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
}
}