using System.Windows;

namespace AccountingSystem.WPF.Views
{
    public partial class CashierWindow : Window
    {
        public CashierWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CancelInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("هل تريد إلغاء الفاتورة الحالية؟", "تأكيد الإلغاء", 
                               MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                // إلغاء الفاتورة
                MessageBox.Show("تم إلغاء الفاتورة بنجاح", "تم الإلغاء", 
                               MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}