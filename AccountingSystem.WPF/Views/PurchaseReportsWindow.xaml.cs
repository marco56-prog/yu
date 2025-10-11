using System.Windows;

namespace AccountingSystem.WPF.Views
{
    public partial class PurchaseReportsWindow : Window
    {
        public PurchaseReportsWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
