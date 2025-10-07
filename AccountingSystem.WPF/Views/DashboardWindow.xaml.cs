using System.Windows;
using AccountingSystem.WPF.ViewModels;

namespace AccountingSystem.WPF.Views
{
    public partial class DashboardWindow : Window
    {
        private readonly DashboardViewModel _vm;

        public DashboardWindow(DashboardViewModel vm)
        {
            InitializeComponent();
            _vm = vm;
            DataContext = _vm;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // تحميل أولي
            await _vm.LoadDataAsync();
        }
    }
}
