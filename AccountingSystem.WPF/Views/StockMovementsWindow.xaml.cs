using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views;

public partial class StockMovementsWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ObservableCollection<StockMovement> _movements;

    public StockMovementsWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _movements = new ObservableCollection<StockMovement>();
        dgStockMovements.ItemsSource = _movements;
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            lblStatus.Text = "جاري تحميل البيانات...";
            // سيتم تحميل حركات المخزون
            lblStatus.Text = "تم تحميل البيانات بنجاح";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ");
        }
    }

    private void Filter_Changed(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // تطبيق الفلاتر
    }

    private void btnRefresh_Click(object sender, RoutedEventArgs e)
    {
        LoadData();
    }

    private void btnClearFilter_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم مسح الفلاتر", "معلومات");
    }

    private void btnExport_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم تصدير البيانات", "معلومات");
    }
}