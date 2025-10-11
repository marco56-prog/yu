using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.Business;
using AccountingSystem.Models;
using AccountingSystem.Data;

namespace AccountingSystem.WPF.Views;

public partial class CustomersWindow : Window
{
    private readonly ICustomerService _customerService;
    private readonly IServiceProvider _serviceProvider;
    private readonly AccountingDbContext _context;
    private readonly ObservableCollection<Customer> _customers;
    private List<Customer> _allCustomers;

    public CustomersWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _customerService = serviceProvider.GetRequiredService<ICustomerService>();
        _context = serviceProvider.GetRequiredService<AccountingDbContext>();
        _customers = new ObservableCollection<Customer>();
        _allCustomers = new List<Customer>();
        dgCustomers.ItemsSource = _customers;

        Loaded += async (s, e) => await LoadCustomers();
        SetupCurrencyDisplay();
    }

    private void SetupCurrencyDisplay()
    {
        // تحديث عرض العملة من إعدادات النظام
        var currencySymbol = _context.SystemSettings
            .FirstOrDefault(s => s.SettingKey == "CurrencySymbol")?.SettingValue ?? "ج.م";

        Title = $"إدارة العملاء - العملة: {currencySymbol}";
    }

    private async Task LoadCustomers()
    {
        try
        {
            lblStatus.Text = "جاري تحميل بيانات العملاء...";
            var customers = await _customerService.GetAllCustomersAsync();
            _allCustomers = customers.ToList();
            _customers.Clear();

            foreach (var customer in _allCustomers)
            {
                _customers.Add(customer);
            }

            lblCount.Text = $"عدد العملاء: {_customers.Count}";

            // حساب إجمالي الأرصدة بالجنيه المصري
            var totalBalance = _allCustomers.Sum(c => c.Balance);
            var currencySymbol = _context.SystemSettings
                .FirstOrDefault(s => s.SettingKey == "CurrencySymbol")?.SettingValue ?? "ج.م";

            // إضافة عنصر جديد لعرض إجمالي الأرصدة
            if (lblTotalBalance != null)
            {
                lblTotalBalance.Text = $"إجمالي الأرصدة: {totalBalance:N2} {currencySymbol}";
            }

            lblStatus.Text = "تم تحميل البيانات بنجاح";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
            lblStatus.Text = "خطأ في تحميل البيانات";
        }
    }

    private async void btnAdd_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var dialog = scope.ServiceProvider.GetRequiredService<CustomerDialog>();
            dialog.Owner = this;

            if (dialog.ShowDialog() == true)
            {
                await LoadCustomers();
                lblStatus.Text = "تم إضافة العميل الجديد بنجاح";
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إضافة العميل: {ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void btnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (dgCustomers.SelectedItem is Customer selectedCustomer)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dialog = scope.ServiceProvider.GetRequiredService<CustomerDialog>();
                dialog.SetEditMode(selectedCustomer);
                dialog.Owner = this;

                if (dialog.ShowDialog() == true)
                {
                    await LoadCustomers();
                    lblStatus.Text = $"تم تحديث بيانات العميل '{selectedCustomer.CustomerName}' بنجاح";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث بيانات العميل: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            MessageBox.Show("يرجى اختيار عميل للتعديل", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void btnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (dgCustomers.SelectedItem is Customer selectedCustomer)
        {
            var result = MessageBox.Show(
                $"هل أنت متأكد من حذف العميل '{selectedCustomer.CustomerName}'؟\n\n" +
                $"الرصيد الحالي: {selectedCustomer.Balance:N2} ج.م\n\n" +
                "تحذير: هذا الإجراء قد يؤثر على المعاملات السابقة!",
                "تأكيد الحذف",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await _customerService.DeleteCustomerAsync(selectedCustomer.CustomerId);
                    await LoadCustomers();
                    lblStatus.Text = $"تم حذف العميل '{selectedCustomer.CustomerName}' بنجاح";

                    MessageBox.Show("تم حذف العميل بنجاح!", "نجح",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في حذف العميل: {ex.Message}", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        else
        {
            MessageBox.Show("يرجى اختيار عميل للحذف", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void btnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadCustomers();
        lblStatus.Text = "تم تحديث البيانات";
    }

    private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = txtSearch.Text.Trim();

        if (string.IsNullOrEmpty(searchText))
        {
            _customers.Clear();
            foreach (var customer in _allCustomers)
            {
                _customers.Add(customer);
            }
        }
        else
        {
            var filteredCustomers = _allCustomers.Where(c =>
                c.CustomerName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                (c.Phone != null && c.Phone.Contains(searchText)) ||
                (c.Email != null && c.Email.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                (c.Address != null && c.Address.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            ).ToList();

            _customers.Clear();
            foreach (var customer in filteredCustomers)
            {
                _customers.Add(customer);
            }
        }

        lblCount.Text = $"عدد العملاء: {_customers.Count}";

        // تحديث إجمالي الأرصدة للنتائج المفلترة
        var totalBalance = _customers.Sum(c => c.Balance);
        var currencySymbol = _context.SystemSettings
            .FirstOrDefault(s => s.SettingKey == "CurrencySymbol")?.SettingValue ?? "ج.م";

        if (lblTotalBalance != null)
        {
            lblTotalBalance.Text = $"إجمالي الأرصدة: {totalBalance:N2} {currencySymbol}";
        }
    }

    private void dgCustomers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgCustomers.SelectedItem != null)
        {
            btnEdit_Click(sender, e);
        }
    }

    private void btnViewTransactions_Click(object sender, RoutedEventArgs e)
    {
        if (dgCustomers.SelectedItem is Customer selectedCustomer)
        {
            // عرض معاملات العميل المالية
            var message = $"معاملات العميل: {selectedCustomer.CustomerName}\n" +
                         $"الرصيد الحالي: {selectedCustomer.Balance:N2} ج.م\n\n" +
                         "سيتم تطوير نافذة تفصيلية لعرض جميع المعاملات قريباً";

            MessageBox.Show(message, "معاملات العميل",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("يرجى اختيار عميل لعرض معاملاته", "تنبيه",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _context?.Dispose();
    }
}