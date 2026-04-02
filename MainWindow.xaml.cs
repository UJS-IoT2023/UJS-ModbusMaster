using System.Windows;
using UJS_ModbusMaster.ViewModels;

namespace UJS_ModbusMaster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            this.DataContext = _viewModel;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            _viewModel?.Dispose();
            base.OnClosing(e);
        }
    }
}
