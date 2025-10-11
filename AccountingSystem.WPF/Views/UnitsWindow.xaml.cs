using System.Collections.ObjectModel;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views;

public partial class UnitsWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ObservableCollection<Unit> _units;

    public UnitsWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _units = new ObservableCollection<Unit>();
        dgUnits.ItemsSource = _units;
        LoadData();
    }

    private void LoadData()
    {
        try
        {
            lblStatus.Text = "جاري تحميل البيانات...";

            // بيانات تجريبية للوحدات الأساسية
            _units.Add(new Unit { UnitId = 1, UnitName = "قطعة", UnitSymbol = "قطعة", IsActive = true });
            _units.Add(new Unit { UnitId = 2, UnitName = "كيلو", UnitSymbol = "كجم", IsActive = true });
            _units.Add(new Unit { UnitId = 3, UnitName = "لتر", UnitSymbol = "لتر", IsActive = true });
            _units.Add(new Unit { UnitId = 4, UnitName = "متر", UnitSymbol = "م", IsActive = true });
            _units.Add(new Unit { UnitId = 5, UnitName = "علبة", UnitSymbol = "علبة", IsActive = true });

            lblCount.Text = $"عدد الوحدات: {_units.Count}";
            lblStatus.Text = "تم تحميل البيانات بنجاح";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ");
        }
    }

    private void btnAdd_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم إضافة وحدة جديدة", "معلومات");
    }

    private void btnEdit_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم تعديل الوحدة", "معلومات");
    }

    private void btnDelete_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم حذف الوحدة", "معلومات");
    }

    private void btnRefresh_Click(object sender, RoutedEventArgs e)
    {
        _units.Clear();
        LoadData();
    }
}