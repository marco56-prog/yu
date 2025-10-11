using System.Windows;

namespace AccountingSystem.WPF.Views;

/// <summary>
/// نافذة طلبات الشراء
/// </summary>
public partial class PurchaseOrdersWindow : Window
{
    public PurchaseOrdersWindow()
    {
        InitializeComponent();
    }

    private void btnNewPurchaseOrder_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم فتح نافذة إنشاء طلب شراء جديد قريباً!",
            "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnSearch_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم إضافة البحث المتقدم قريباً!",
            "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnApprove_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم إضافة ميزة اعتماد الطلبات قريباً!",
            "قيد التطوير", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}