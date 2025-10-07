using System.Windows;
using System.Windows.Controls;
using AccountingSystem.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views
{
    public partial class CategoriesWindow : Window
    {
        public CategoriesWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();
            DataContext = serviceProvider.GetRequiredService<CategoriesViewModel>();
        }

        // فتح التعديل بدبل-كليك على الصف (اختياري)
        private void Row_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (DataContext is CategoriesViewModel vm && vm.EditCommand.CanExecute((sender as DataGridRow)?.DataContext))
                vm.EditCommand.Execute((sender as DataGridRow)?.DataContext);
        }
    }
}
