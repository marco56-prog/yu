using System.Windows;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views
{
    public partial class ProductEntryDialog : Window
    {
        public ProductEntryDialog(Product product)
        {
            InitializeComponent();
            DataContext = product;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}