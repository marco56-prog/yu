using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.Business;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views;

public partial class CustomerDialog : Window
{
    private readonly ICustomerService _customerService;
    private Customer? _customer;
    private bool _isEditMode;

    // Constructor for DI
    public CustomerDialog(ICustomerService customerService)
    {
        InitializeComponent();
        _customerService = customerService;
        _customer = null;
        _isEditMode = false;

        lblTitle.Text = "إضافة عميل جديد";
        txtBalance.Visibility = Visibility.Collapsed;
        var grid = (Grid)((Grid)Content).Children.OfType<Grid>().First();
        var balanceLabel = grid.Children.OfType<TextBlock>()
            .FirstOrDefault(t => t.Text == "الرصيد الحالي:");
        if (balanceLabel != null)
            balanceLabel.Visibility = Visibility.Collapsed;
    }

    // Constructor for editing existing customer
    public CustomerDialog(ICustomerService customerService, Customer customer)
    {
        InitializeComponent();
        _customerService = customerService;
        _customer = customer;
        _isEditMode = true;

        lblTitle.Text = "تعديل بيانات العميل";
        LoadCustomerData();
    }

    public void SetEditMode(Customer customer)
    {
        _customer = customer;
        _isEditMode = true;
        lblTitle.Text = "تعديل بيانات العميل";
        LoadCustomerData();
    }

    private void LoadCustomerData()
    {
        if (_customer != null)
        {
            txtCustomerName.Text = _customer.CustomerName;
            txtAddress.Text = _customer.Address ?? "";
            txtPhone.Text = _customer.Phone ?? "";
            txtEmail.Text = _customer.Email ?? "";
            txtBalance.Text = _customer.Balance.ToString("N2");
            chkIsActive.IsChecked = _customer.IsActive;
        }
    }

    private async void btnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(txtCustomerName.Text))
            {
                MessageBox.Show("يرجى إدخال اسم العميل", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                txtCustomerName.Focus();
                return;
            }

            if (_isEditMode && _customer != null)
            {
                // تعديل العميل
                _customer.CustomerName = txtCustomerName.Text.Trim();
                _customer.Address = txtAddress.Text.Trim();
                _customer.Phone = txtPhone.Text.Trim();
                _customer.Email = txtEmail.Text.Trim();
                _customer.IsActive = chkIsActive.IsChecked ?? true;

                await _customerService.UpdateCustomerAsync(_customer);
                MessageBox.Show("تم تحديث بيانات العميل بنجاح", "نجح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // إضافة عميل جديد
                var newCustomer = new Customer
                {
                    CustomerName = txtCustomerName.Text.Trim(),
                    Address = txtAddress.Text.Trim(),
                    Phone = txtPhone.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    IsActive = chkIsActive.IsChecked ?? true
                };

                await _customerService.CreateCustomerAsync(newCustomer);
                MessageBox.Show("تم إضافة العميل بنجاح", "نجح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }

            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ البيانات: {ex.Message}", "خطأ",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}