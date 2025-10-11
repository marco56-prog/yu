using System.Windows;

namespace AccountingSystem.WPF.Views
{
    public partial class CustomerReportsWindow : Window
    {
        public CustomerReportsWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
