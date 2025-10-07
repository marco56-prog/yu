using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.Business;
using AccountingSystem.Models;
using AccountingSystem.Data;

namespace AccountingSystem.WPF.Views;

public partial class SalesReportsWindow : Window
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IReportService _reportService;
    private readonly ICustomerService _customerService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly CultureInfo _culture;

    public SalesReportsWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _reportService = serviceProvider.GetRequiredService<IReportService>();
        _customerService = serviceProvider.GetRequiredService<ICustomerService>();
        _unitOfWork = serviceProvider.GetRequiredService<IUnitOfWork>();
        
        // إعداد الثقافة المصرية
        _culture = new CultureInfo("ar-EG");
        _culture.NumberFormat.CurrencySymbol = "ج.م";
        
        Loaded += async (s, e) => await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            // تحميل العملاء للفلتر
            var customers = await _customerService.GetAllCustomersAsync();
            cmbCustomer.ItemsSource = customers.ToList();
            
            // تعيين التواريخ الافتراضية
            dpFromDate.SelectedDate = DateTime.Today.AddMonths(-1);
            dpToDate.SelectedDate = DateTime.Today;
            
            // تحميل التقرير الافتراضي
            await GenerateReportAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task GenerateReportAsync()
    {
        try
        {
            if (!dpFromDate.SelectedDate.HasValue || !dpToDate.SelectedDate.HasValue)
            {
                MessageBox.Show("يرجى تحديد التواريخ", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var fromDate = dpFromDate.SelectedDate.Value;
            var toDate = dpToDate.SelectedDate.Value.AddDays(1).AddSeconds(-1); // نهاية اليوم
            
            Customer? selectedCustomer = cmbCustomer.SelectedItem as Customer;
            int? customerId = selectedCustomer?.CustomerId;

            // توليد تقرير المبيعات
            var report = await _reportService.GenerateSalesReportAsync(fromDate, toDate, customerId);

            // عرض النتائج
            lblTotalSales.Text = report.NetSales.ToString("C", _culture);
            lblInvoiceCount.Text = report.InvoiceCount.ToString("N0");
            lblAverageInvoice.Text = report.AverageInvoiceValue.ToString("C", _culture);
            lblTotalDiscount.Text = report.TotalDiscount.ToString("C", _culture);
            
            // عرض الفواتير في الجدول
            dgReport.ItemsSource = report.Invoices.Select(i => new
            {
                InvoiceNumber = i.InvoiceNumber,
                InvoiceDate = i.InvoiceDate.ToString("dd/MM/yyyy"),
                CustomerName = i.Customer?.CustomerName ?? "غير محدد",
                SubTotal = i.SubTotal.ToString("C", _culture),
                TaxAmount = i.TaxAmount.ToString("C", _culture),
                DiscountAmount = i.DiscountAmount.ToString("C", _culture),
                NetTotal = i.NetTotal.ToString("C", _culture),
                Status = i.Status.ToString()
            }).ToList();

        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في توليد التقرير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void GenerateReportClick(object sender, RoutedEventArgs e)
    {
        await GenerateReportAsync();
    }

    private void PrintClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم تنفيذ الطباعة لاحقاً", "قريباً", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ExportClick(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم تنفيذ التصدير لاحقاً", "قريباً", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // طرق الأحداث المطلوبة للأزرار في XAML
    private async void btnGenerate_Click(object sender, RoutedEventArgs e)
    {
        await GenerateReportAsync();
    }

    private void btnExportExcel_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم تنفيذ التصدير إلى Excel لاحقاً", "قريباً", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnExportPDF_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم تنفيذ التصدير إلى PDF لاحقاً", "قريباً", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnPrint_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show("سيتم تنفيذ الطباعة لاحقاً", "قريباً", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    #region Interactive Report Events

    private void dgReport_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (dgReport.SelectedItem is SalesInvoice invoice)
        {
            OpenInvoiceDetails(invoice);
        }
    }

    private void btnOpenInvoice_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).DataContext is SalesInvoice invoice)
        {
            OpenInvoiceDetails(invoice);
        }
    }

    private void btnOpenCustomer_Click(object sender, RoutedEventArgs e)
    {
        if (((Button)sender).DataContext is SalesInvoice invoice)
        {
            OpenCustomerDetails(invoice.CustomerId);
        }
    }

    private void OpenInvoiceDetails(SalesInvoice invoice)
    {
        try
        {
            var invoiceWindow = new SalesInvoiceWindow(_serviceProvider);
            invoiceWindow.Show();
            
            MessageBox.Show($"فتح الفاتورة رقم: {invoice.InvoiceNumber}\nالتاريخ: {invoice.InvoiceDate:dd/MM/yyyy}\nالعميل: {invoice.Customer?.CustomerName ?? "غير محدد"}", 
                "تفاصيل الفاتورة", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في فتح الفاتورة: {ex.Message}", "خطأ", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenCustomerDetails(int customerId)
    {
        try
        {
            var customerWindow = new CustomersWindow(_serviceProvider);
            customerWindow.Show();
            
            MessageBox.Show($"سيتم فتح بيانات العميل رقم: {customerId}\nميزة البحث المباشر عن العميل ستُضاف قريباً", 
                "معلومات", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في فتح بيانات العميل: {ex.Message}", "خطأ", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    #endregion
}