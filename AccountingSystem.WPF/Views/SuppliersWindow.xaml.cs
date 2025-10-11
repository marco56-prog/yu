using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.Business;
using AccountingSystem.Models;
using AccountingSystem.Data;

namespace AccountingSystem.WPF.Views;

public partial class SuppliersWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISupplierService _supplierService;
    private readonly AccountingDbContext _context;
    private readonly ObservableCollection<Supplier> _suppliers;
    private List<Supplier> _allSuppliers;

    public SuppliersWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _supplierService = serviceProvider.GetRequiredService<ISupplierService>();
        _context = serviceProvider.GetRequiredService<AccountingDbContext>();
        _suppliers = new ObservableCollection<Supplier>();
        _allSuppliers = new List<Supplier>();
        dgSuppliers.ItemsSource = _suppliers;

        Loaded += async (s, e) => await LoadData();
        SetupCurrencyDisplay();
    }

    private void SetupCurrencyDisplay()
    {
        var currencySymbol = _context.SystemSettings
            .FirstOrDefault(s => s.SettingKey == "CurrencySymbol")?.SettingValue ?? "ج.م";

        Title = $"إدارة الموردين - العملة: {currencySymbol}";
    }

    private async Task LoadData()
    {
        try
        {
            lblStatus.Text = "جاري تحميل بيانات الموردين...";
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            _allSuppliers = suppliers.ToList();
            _suppliers.Clear();

            foreach (var supplier in _allSuppliers)
            {
                _suppliers.Add(supplier);
            }

            lblCount.Text = $"عدد الموردين: {_suppliers.Count}";

            // حساب إجمالي الأرصدة بالجنيه المصري
            var totalBalance = _allSuppliers.Sum(s => s.Balance);
            var currencySymbol = _context.SystemSettings
                .FirstOrDefault(s => s.SettingKey == "CurrencySymbol")?.SettingValue ?? "ج.م";

            lblStatus.Text = "تم تحميل بيانات الموردين بنجاح";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
            lblStatus.Text = "خطأ في تحميل البيانات";
        }
    }

    private void btnAdd_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // سيتم إنشاء SupplierDialog لاحقاً
            MessageBox.Show("سيتم إضافة نافذة المورد الجديد قريباً", "قيد التطوير",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إضافة المورد: {ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgSuppliers.SelectedItem is Supplier selectedSupplier)
        {
            try
            {
                MessageBox.Show($"تحديث بيانات المورد: {selectedSupplier.SupplierName}", "قيد التطوير",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث المورد: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show("يرجى اختيار مورد للتعديل", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void btnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgSuppliers.SelectedItem is Supplier selectedSupplier)
        {
            var result = MessageBox.Show(
                $"هل أنت متأكد من حذف المورد '{selectedSupplier.SupplierName}'؟\n\n" +
                $"الرصيد الحالي: {selectedSupplier.Balance:N2} ج.م",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _supplierService.DeleteSupplierAsync(selectedSupplier.SupplierId);
                    await LoadData();
                    lblStatus.Text = $"تم حذف المورد '{selectedSupplier.SupplierName}' بنجاح";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف المورد: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("يرجى اختيار مورد للحذف", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void btnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadData();
        lblStatus.Text = "تم تحديث البيانات";
    }

    private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = txtSearch.Text.Trim();

        if (string.IsNullOrEmpty(searchText))
        {
            _suppliers.Clear();
            foreach (var supplier in _allSuppliers)
            {
                _suppliers.Add(supplier);
            }
        }
        else
        {
            var filteredSuppliers = _allSuppliers.Where(s =>
                s.SupplierName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (s.Phone != null && s.Phone.Contains(searchText)) ||
                (s.Email != null && s.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (s.Address != null && s.Address.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            _suppliers.Clear();
            foreach (var supplier in filteredSuppliers)
            {
                _suppliers.Add(supplier);
            }
        }

        lblCount.Text = $"عدد الموردين: {_suppliers.Count}";
    }

    private void dgSuppliers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgSuppliers.SelectedItem != null)
        {
            btnEdit_Click(sender, e);
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _context?.Dispose();
    }
}