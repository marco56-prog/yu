using System.Windows;

namespace AccountingSystem.WPF.Views;

/// <summary>
/// نافذة سندات الصرف
/// </summary>
public partial class PaymentVouchersWindow : Window
{
    public PaymentVouchersWindow()
    {
        InitializeComponent();
    }

    private void btnNewPayment_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم فتح نافذة إنشاء سند صرف جديد قريباً!", 
            "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnSearch_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم إضافة البحث المتقدم قريباً!", 
            "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}