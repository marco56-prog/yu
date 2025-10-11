using System.Windows;

namespace AccountingSystem.WPF.Views
{
    public partial class SupplierReportsWindow : Window
    {
        public SupplierReportsWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
