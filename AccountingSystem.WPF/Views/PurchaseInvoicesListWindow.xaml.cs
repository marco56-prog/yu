using AccountingSystem.Models;
using AccountingSystem.Business;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views
{
    public partial class PurchaseInvoicesListWindow : Window
    {
        private readonly IPurchaseInvoiceService _invoiceService;
        private readonly ISupplierService _supplierService;
        private readonly IServiceProvider _serviceProvider;

        private readonly ObservableCollection<PurchaseInvoice> _all = new();
        private ICollectionView _view = null!;
        private readonly CultureInfo _eg = new("ar-EG");
        private readonly DispatcherTimer _searchDebounce = new() { Interval = TimeSpan.FromMilliseconds(220) };

        public PurchaseInvoicesListWindow(IPurchaseInvoiceService invoiceService, ISupplierService supplierService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
            _supplierService = supplierService ?? throw new ArgumentNullException(nameof(supplierService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _searchDebounce.Tick += (_, __) =>
            {
                _searchDebounce.Stop();
                RefreshFilters();
            };

            SetupUI();
            _ = LoadInvoicesAsync();
        }

        private void SetupUI()
        {
            _view = CollectionViewSource.GetDefaultView(_all);
            dgInvoices.ItemsSource = _view;

            // إعداد الفلاتر
            cmbStatusFilter.Items.Add("جميع الحالات");
            cmbStatusFilter.Items.Add("مسودة");
            cmbStatusFilter.Items.Add("مرحّلة");
            cmbStatusFilter.Items.Add("ملغية");
            cmbStatusFilter.SelectedIndex = 0;

            // تواريخ افتراضية
            dpFromDate.SelectedDate = DateTime.Today.AddMonths(-1);
            dpToDate.SelectedDate = DateTime.Today;
        }

        private async Task LoadInvoicesAsync()
        {
            try
            {
                dgLoading.Visibility = Visibility.Visible;
                dgInvoices.Visibility = Visibility.Collapsed;

                var invoices = await _invoiceService.GetAllPurchaseInvoicesAsync();
                _all.Clear();
                
                foreach (var inv in invoices.OrderByDescending(x => x.InvoiceDate))
                {
                    _all.Add(inv);
                }

                // تحميل الموردين للفلتر
                await LoadSuppliersAsync();
                
                RefreshFilters();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الفواتير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                dgLoading.Visibility = Visibility.Collapsed;
                dgInvoices.Visibility = Visibility.Visible;
            }
        }

        private async Task LoadSuppliersAsync()
        {
            try
            {
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                cmbSupplierFilter.Items.Clear();
                cmbSupplierFilter.Items.Add(new { SupplierId = 0, SupplierName = "جميع الموردين" });
                
                foreach (var supplier in suppliers)
                {
                    cmbSupplierFilter.Items.Add(supplier);
                }
                
                cmbSupplierFilter.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل الموردين: {ex.Message}");
            }
        }

        private void RefreshFilters()
        {
            try
            {
                if (_view == null) return;

                using (_view.DeferRefresh())
                {
                    _view.Filter = item =>
                    {
                        if (item is not PurchaseInvoice inv) return false;

                        // فلتر التاريخ
                        if (dpFromDate.SelectedDate.HasValue && inv.InvoiceDate.Date < dpFromDate.SelectedDate.Value.Date)
                            return false;
                        if (dpToDate.SelectedDate.HasValue && inv.InvoiceDate.Date > dpToDate.SelectedDate.Value.Date)
                            return false;

                        // فلتر الحالة
                        if (cmbStatusFilter.SelectedIndex > 0)
                        {
                            var selectedStatus = GetStatusFromIndex(cmbStatusFilter.SelectedIndex);
                            if (inv.Status != selectedStatus) return false;
                        }

                        // فلتر المورد
                        if (cmbSupplierFilter.SelectedItem != null)
                        {
                            var selectedSupplier = cmbSupplierFilter.SelectedItem;
                            var supplierId = selectedSupplier.GetType().GetProperty("SupplierId")?.GetValue(selectedSupplier);
                            if (supplierId is int id && id > 0 && inv.SupplierId != id)
                                return false;
                        }

                        // فلتر البحث النصي
                        if (!string.IsNullOrWhiteSpace(txtSearch.Text))
                        {
                            var searchText = Normalize(txtSearch.Text);
                            var invoiceSearchText = BuildSearchIndex(inv);
                            if (!invoiceSearchText.Contains(searchText))
                                return false;
                        }

                        // فلتر المبلغ
                        if (TryParseDecimal(txtMinAmount.Text, out var minAmount) && inv.NetTotal < minAmount)
                            return false;
                        if (TryParseDecimal(txtMaxAmount.Text, out var maxAmount) && inv.NetTotal > maxAmount)
                            return false;

                        return true;
                    };
                }

                // إحصائيات
                var filtered = _view.Cast<PurchaseInvoice>().ToList();
                lblCount.Text = $"عدد الفواتير: {filtered.Count}";
                lblTotal.Text = $"الإجمالي: {filtered.Sum(x => x.NetTotal).ToString("C", _eg)}";
                lblPaid.Text = $"المدفوع: {filtered.Sum(x => x.PaidAmount).ToString("C", _eg)}";
                lblRemaining.Text = $"المتبقي: {filtered.Sum(x => x.RemainingAmount).ToString("C", _eg)}";

                // تلوين الصفوف
                ApplyRowColors();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في الفلترة: {ex.Message}");
            }
        }

        private void ApplyRowColors()
        {
            try
            {
                dgInvoices.UpdateLayout();
                
                for (int i = 0; i < dgInvoices.Items.Count; i++)
                {
                    if (dgInvoices.ItemContainerGenerator.ContainerFromIndex(i) is not DataGridRow row) continue;
                    if (dgInvoices.Items[i] is not PurchaseInvoice inv) continue;

                    row.Background = inv.Status switch
                    {
                        InvoiceStatus.Draft => System.Windows.Media.Brushes.LightYellow,
                        InvoiceStatus.Posted => System.Windows.Media.Brushes.LightGreen,
                        InvoiceStatus.Cancelled => System.Windows.Media.Brushes.LightPink,
                        _ => System.Windows.Media.Brushes.White
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تلوين الصفوف: {ex.Message}");
            }
        }

        private static InvoiceStatus GetStatusFromIndex(int index) => index switch
        {
            1 => InvoiceStatus.Draft,
            2 => InvoiceStatus.Posted,
            3 => InvoiceStatus.Cancelled,
            _ => InvoiceStatus.Draft
        };

        private bool TryParseDecimal(string? text, out decimal value)
        {
            text = Normalize(text ?? "");
            return decimal.TryParse(text, NumberStyles.Any, _eg, out value) ||
                   decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value) ||
                   decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        // ===== معالجات الأحداث =====
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshFilters();

        private void Filter_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchDebounce.Stop();
            _searchDebounce.Start();
        }

        private void Filter_CheckChanged(object sender, RoutedEventArgs e) => RefreshFilters();

        private void dpFromDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => RefreshFilters();
        private void dpToDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e) => RefreshFilters();

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadInvoicesAsync();
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var scope = _serviceProvider.CreateScope();
                var window = scope.ServiceProvider.GetRequiredService<PurchaseInvoiceWindow>();
                window.Owner = Window.GetWindow(this);
                window.Closed += (_, __) => { scope.Dispose(); Task.Run(() => RefreshInvoices()); };
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح فاتورة شراء جديدة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RefreshInvoices()
        {
            await LoadInvoicesAsync();
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            var inv = dgInvoices.SelectedItem as PurchaseInvoice;
            if (inv == null)
            {
                MessageBox.Show(Constants.MSG_SELECT_INVOICE, Constants.CAPTION_WARNING, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            try
            {
                var scope = _serviceProvider.CreateScope();
                var window = scope.ServiceProvider.GetRequiredService<PurchaseInvoiceWindow>();
                window.Owner = Window.GetWindow(this);
                window.Closed += (_, __) => { scope.Dispose(); Task.Run(() => RefreshInvoices()); };
                
                // سيتم إضافة تحميل الفاتورة لاحقاً
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnPost_Click(object sender, RoutedEventArgs e)
        {
            var inv = dgInvoices.SelectedItem as PurchaseInvoice;
            if (inv == null)
            {
                MessageBox.Show(Constants.MSG_SELECT_INVOICE, Constants.CAPTION_WARNING, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (inv.IsPosted)
            {
                MessageBox.Show("الفاتورة مرحّلة بالفعل.", Constants.CAPTION_WARNING, MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"تأكيد ترحيل فاتورة الشراء {inv.InvoiceNumber}؟",
                                "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                await _invoiceService.PostPurchaseInvoiceAsync(inv.PurchaseInvoiceId);
                await LoadInvoicesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل الترحيل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            var inv = dgInvoices.SelectedItem as PurchaseInvoice;
            if (inv == null)
            {
                MessageBox.Show(Constants.MSG_SELECT_INVOICE, Constants.CAPTION_WARNING, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (inv.Status == InvoiceStatus.Cancelled)
            {
                MessageBox.Show("الفاتورة ملغية بالفعل.", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show($"هل تريد إلغاء فاتورة الشراء {inv.InvoiceNumber}؟",
                                "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                await _invoiceService.CancelPurchaseInvoiceAsync(inv.PurchaseInvoiceId);
                await LoadInvoicesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل الإلغاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void dgInvoices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            btnView_Click(sender, new RoutedEventArgs());
        }

        private void dgInvoices_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                btnView_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Delete)
            {
                btnCancel_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        // ===== أدوات بحث عربية/عامة =====
        private static string Normalize(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            var s = input.Trim();

            // أرقام عربية/فارسي -> لاتينية
            s = s
                .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9')
                .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
                .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9');

            // إزالة التشكيل والتطويل
            s = Regex.Replace(s, "[\\u064B-\\u0652\\u0670\\u0640]", "");

            // توحيد حروف
            s = s.Replace('أ', 'ا').Replace('إ', 'ا').Replace('آ', 'ا');
            s = s.Replace('ة', 'ه').Replace('ى', 'ي');

            s = s.ToLowerInvariant();
            return s;
        }

        private static string BuildSearchIndex(PurchaseInvoice inv)
        {
            var parts = new[]
            {
                inv.InvoiceNumber ?? "",
                inv.Supplier?.SupplierName ?? "",
                StatusToArabic(inv.Status), // ← إضافة العربي
                inv.Status.ToString(),      // وخلّي الإنجليزي برضه
                inv.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                inv.NetTotal.ToString("0.##", CultureInfo.InvariantCulture),
                inv.PaidAmount.ToString("0.##", CultureInfo.InvariantCulture),
                inv.RemainingAmount.ToString("0.##", CultureInfo.InvariantCulture)
            };
            return Normalize(string.Join(" ", parts));
        }

        private static string StatusToArabic(InvoiceStatus status) => status switch
        {
            InvoiceStatus.Draft => "مسودة",
            InvoiceStatus.Posted => "مرحلة",
            InvoiceStatus.Cancelled => "ملغية",
            _ => "غير معروف"
        };

        private static class Constants
        {
            public const string MSG_SELECT_INVOICE = "برجاء اختيار فاتورة أولاً.";
            public const string CAPTION_WARNING = "تنبيه";
        }
    }
}