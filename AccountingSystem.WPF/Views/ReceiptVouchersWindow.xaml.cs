using System.Windows;

namespace AccountingSystem.WPF.Views;

/// <summary>
/// نافذة سندات القبض
/// </summary>
public partial class ReceiptVouchersWindow : Window
{
    public ReceiptVouchersWindow()
    {
        InitializeComponent();
    }

    private void btnNewReceipt_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم فتح نافذة إنشاء سند قبض جديد قريباً!",
            "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnSearch_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم إضافة البحث المتقدم قريباً!",
            "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}