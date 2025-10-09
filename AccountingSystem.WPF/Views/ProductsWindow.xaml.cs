using AccountingSystem.Business;
using AccountingSystem.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace AccountingSystem.WPF.Views
{
    public partial class ProductsWindow : Window
    {
        // The constructor now accepts the ViewModel directly.
        public ProductsWindow(ProductViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        // This event handler is for a feature not yet implemented in the ViewModel.
        // It will be removed or refactored in a future step.
        private void btnStockAdjustment_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("تسوية المخزون هي ميزة قيد التطوير.", "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // This is also a placeholder until the category filter is implemented in the ViewModel.
        private void cmbCategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // The filtering logic is now handled by the ViewModel's ProductsView.
            // This event handler can be removed once the ComboBox is bound to the ViewModel.
        }
    }
}