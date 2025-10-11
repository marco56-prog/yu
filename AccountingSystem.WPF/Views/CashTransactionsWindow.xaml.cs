using System.Windows;

namespace AccountingSystem.WPF.Views
{
    public partial class CashTransactionsWindow : Window
    {
        public CashTransactionsWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

