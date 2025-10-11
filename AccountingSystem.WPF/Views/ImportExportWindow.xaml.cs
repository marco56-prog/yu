using System.Windows;

namespace AccountingSystem.WPF.Views
{
    public partial class ImportExportWindow : Window
    {
        public ImportExportWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
