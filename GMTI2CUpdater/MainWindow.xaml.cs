using System.Windows;

namespace GMTI2CUpdater
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //DataContext = new MainWindowViewModel();
        }
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