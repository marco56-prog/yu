using System.Windows;

namespace AccountingSystem.WPF.Views
{
    public partial class StockAdjustmentWindow : Window
    {
        public StockAdjustmentWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

