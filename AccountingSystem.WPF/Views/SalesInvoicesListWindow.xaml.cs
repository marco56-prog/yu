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
    public partial class SalesInvoicesListWindow : Window
    {
        private readonly ISalesInvoiceService _invoiceService;
        private readonly ICustomerService _customerService;
        private readonly IServiceProvider _serviceProvider;

        private readonly ObservableCollection<SalesInvoice> _all = new();
        private ICollectionView _view = null!;
        private readonly CultureInfo _eg = new("ar-EG");
        private readonly DispatcherTimer _searchDebounce = new() { Interval = TimeSpan.FromMilliseconds(220) };
        private bool _isRefreshScheduled;

        public SalesInvoicesListWindow(ISalesInvoiceService invoiceService, ICustomerService customerService, IServiceProvider serviceProvider)
        {
            InitializeComponent();
            _invoiceService = invoiceService ?? throw new ArgumentNullException(nameof(invoiceService));
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            _searchDebounce.Tick += (_, __) => { _searchDebounce.Stop(); RefreshFilters(); };

            // Bind DataGrid to the collection so items appear
            dgInvoices.ItemsSource = _all;

            Loaded += async (_, __) =>
            {
                InitDefaultDates();
                await LoadCustomersAsync();
                await LoadInvoicesAsync();
                SetupView();
            };
        }

        // ملاحظة: استخدم منشئ DI المُسجّل في App، ولا تعتمد على App.Services مباشرةً هنا

        private void InitDefaultDates()
        {
            var now = DateTime.Now.Date;
            dpToDate.SelectedDate = now;
            dpFromDate.SelectedDate = DateTime.SpecifyKind(new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local), DateTimeKind.Local);
        }

        private async Task LoadCustomersAsync()
        {
            try
            {
                cmbCustomer.Items.Clear();
                // عنصر "جميع العملاء"
                cmbCustomer.Items.Add(new Customer { CustomerId = 0, CustomerName = "جميع العملاء" });

                var customers = await _customerService.GetAllCustomersAsync();
                foreach (var c in customers.OrderBy(c => c.CustomerName))
                    cmbCustomer.Items.Add(c);

                cmbCustomer.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر تحميل العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetupView()
        {
            _view = CollectionViewSource.GetDefaultView(_all);
            _view.Filter = FilterInvoice;
            ScheduleViewRefresh();
        }

        private async Task LoadInvoicesAsync()
        {
            try
            {
                if (lblStatus != null) lblStatus.Text = "جارِ التحميل...";
                Cursor = Cursors.Wait;

                _all.Clear();
                var data = await _invoiceService.GetAllSalesInvoicesAsync();
                foreach (var inv in data.OrderByDescending(i => i.InvoiceDate).ThenByDescending(i => i.InvoiceNumber))
                    _all.Add(inv);

                ScheduleViewRefresh();

                // اختيار أول سطر إن مفيش اختيار
                if (dgInvoices.SelectedItem == null && dgInvoices.Items.Count > 0)
                    dgInvoices.SelectedIndex = 0;

                if (lblStatus != null) lblStatus.Text = "تم التحميل";
            }
            catch (Exception ex)
            {
                if (lblStatus != null) lblStatus.Text = "فشل التحميل";
                MessageBox.Show($"تعذر تحميل الفواتير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        // خريطة تحويل الحالة (عربي ← Enum)
        private static bool TryMapArabicStatusToEnum(string? statusTag, out InvoiceStatus desired)
        {
            desired = default;
            if (string.IsNullOrWhiteSpace(statusTag)) return false;

            // normalize بسيط
            statusTag = statusTag.Trim();

            return statusTag switch
            {
                "مسودة" => Set(out desired, InvoiceStatus.Draft),
                "مؤكدة" => Set(out desired, InvoiceStatus.Confirmed),
                "ملغية" => Set(out desired, InvoiceStatus.Cancelled),
                _ => false
            };

            static bool Set(out InvoiceStatus d, InvoiceStatus v) { d = v; return true; }
        }

        // تحويل من Enum للعربي
        private static string StatusToArabic(InvoiceStatus st) => st switch
        {
            InvoiceStatus.Draft => "مسودة",
            InvoiceStatus.Confirmed => "مؤكدة",
            InvoiceStatus.Cancelled => "ملغية",
            _ => st.ToString()
        };

        // فلترة شاملة
        private bool FilterInvoice(object? obj)
        {
            if (obj is not SalesInvoice inv) return false;

            // تاريخ
            var from = dpFromDate.SelectedDate ?? DateTime.MinValue;
            var to = dpToDate.SelectedDate?.AddDays(1).AddTicks(-1) ?? DateTime.MaxValue;
            if (inv.InvoiceDate < from || inv.InvoiceDate > to) return false;

            // عميل
            var selCustomer = cmbCustomer.SelectedItem as Customer;
            if (selCustomer != null && selCustomer.CustomerId > 0 &&
                (inv.Customer == null || inv.Customer.CustomerId != selCustomer.CustomerId))
                return false;

            // حالة
            var statusItem = cmbStatus.SelectedItem as ComboBoxItem;
            var statusTag = statusItem?.Tag?.ToString();
            if (!string.IsNullOrWhiteSpace(statusTag) &&
                TryMapArabicStatusToEnum(statusTag, out var desired) &&
                inv.Status != desired)
                return false;

            // مرحل فقط
            if (chkPostedOnly.IsChecked == true && !inv.IsPosted) return false;

            // بحث عام
            var q = Normalize(txtSearch.Text);
            if (string.IsNullOrWhiteSpace(q)) return true;

            var tokens = q.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0) return true;

            var index = BuildSearchIndex(inv);
            return tokens.All(t => index.Contains(t));
        }

        // تحديث الإجمالي والعدد
        private void UpdateTotalsAndStatus()
        {
            if (_view == null) return;

            decimal total = 0;
            int count = 0;

            foreach (SalesInvoice inv in _view)
            {
                total += inv.NetTotal;
                count++;
            }

            lblTotalSales.Text = total.ToString("N2", _eg);
            lblCount.Text = count.ToString(_eg);
        }

        // ========== أحداث الفلاتر ==========
        private void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e) => RefreshFilters();

        private void Filter_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchDebounce.Stop();
            _searchDebounce.Start();
        }

        private void Filter_CheckChanged(object sender, RoutedEventArgs e) => RefreshFilters();

        private void RefreshFilters()
        {
            if (_view == null) return;

            ScheduleViewRefresh();
        }

        private void ScheduleViewRefresh()
        {
            if (_view == null) return;

            if (_isRefreshScheduled)
                return;

            _isRefreshScheduled = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    if (_view == null) return;

                    // تحديث CollectionView بطريقة آمنة
                    // تجنب الوصول إلى CurrentItem أثناء التحديث
                    // Use helper to safely refresh the CollectionView and avoid DeferRefresh race conditions
                    try
                    {
                        AccountingSystem.WPF.Helpers.CollectionViewHelper.SafeRefresh(_view);
                    }
                    catch
                    {
                        // SafeRefresh already swallows expected transient errors; ensure totals are attempted later
                        Dispatcher.BeginInvoke(new Action(() => { try { UpdateTotalsAndStatus(); } catch { } }), DispatcherPriority.Background);
                    }

                    // تحديث الإجماليات بعد اكتمال عملية التحديث
                    UpdateTotalsAndStatus();
                }
                finally
                {
                    _isRefreshScheduled = false;
                }
            }), DispatcherPriority.Background);
        }

        // ========== أحداث الواجهة ==========
        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadInvoicesAsync();
        }

        private void dgInvoices_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            btnView_Click(sender, e);
        }

        private void dgInvoices_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                btnView_Click(sender, e);
            }
            else if (e.Key == Key.Delete)
            {
                e.Handled = true;
                btnCancel_Click(sender, e);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.N)
            {
                btnNew_Click(sender, e);
            }
            else if (e.Key == Key.F5)
            {
                btnRefresh_Click(sender, e);
            }
            else if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.F)
            {
                txtSearch.Focus();
                txtSearch.SelectAll();
                e.Handled = true;
            }
        }

        // ========== أزرار الإجراءات ==========
        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder: فتح نافذة إنشاء فاتورة جديدة (لاحقًا)
            MessageBox.Show("فتح نافذة إنشاء فاتورة جديدة...", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var inv = dgInvoices.SelectedItem as SalesInvoice;
            if (inv == null)
            {
                MessageBox.Show(Constants.MSG_SELECT_INVOICE, Constants.CAPTION_WARNING, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            // Placeholder: افتح نافذة تعديل الفاتورة inv
            MessageBox.Show($"تعديل الفاتورة: {inv.InvoiceNumber}", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void btnView_Click(object sender, RoutedEventArgs e)
        {
            var inv = dgInvoices.SelectedItem as SalesInvoice;
            if (inv == null)
            {
                MessageBox.Show(Constants.MSG_SELECT_INVOICE, Constants.CAPTION_WARNING, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var scope = _serviceProvider.CreateScope();
                var window = scope.ServiceProvider.GetRequiredService<SalesInvoiceWindow>();
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
            var inv = dgInvoices.SelectedItem as SalesInvoice;
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

            if (MessageBox.Show($"تأكيد ترحيل الفاتورة {inv.InvoiceNumber}؟",
                                "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                await _invoiceService.PostSalesInvoiceAsync(inv.SalesInvoiceId);
                await LoadInvoicesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل الترحيل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            var inv = dgInvoices.SelectedItem as SalesInvoice;
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

            if (MessageBox.Show($"هل تريد إلغاء الفاتورة {inv.InvoiceNumber}؟",
                                "تأكيد", MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes)
                return;

            try
            {
                await _invoiceService.CancelSalesInvoiceAsync(inv.SalesInvoiceId);
                await LoadInvoicesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل الإلغاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task RefreshInvoices()
        {
            await LoadInvoicesAsync();
        }

        // ========== أدوات بحث عربية/عامة ==========
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

        private static string BuildSearchIndex(SalesInvoice inv)
        {
            var parts = new[]
            {
                inv.InvoiceNumber ?? "",
                inv.Customer?.CustomerName ?? "",
                StatusToArabic(inv.Status), // ← إضافة العربي
                inv.Status.ToString(),      // وخلّي الإنجليزي برضه
                inv.InvoiceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                inv.NetTotal.ToString("0.##", CultureInfo.InvariantCulture),
                inv.PaidAmount.ToString("0.##", CultureInfo.InvariantCulture),
                inv.RemainingAmount.ToString("0.##", CultureInfo.InvariantCulture)
            };
            return Normalize(string.Join(" ", parts));
        }

        private static class Constants
        {
            public const string MSG_SELECT_INVOICE = "برجاء اختيار فاتورة أولاً.";
            public const string CAPTION_WARNING = "تنبيه";
        }
    }
}
