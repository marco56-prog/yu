using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using AccountingSystem.Business;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views
{
    public partial class SalesReturnWindow : Window
    {
        private readonly ISalesInvoiceService _salesInvoiceService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly IPriceHistoryService _priceHistoryService;
        private readonly AccountingDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        private readonly ObservableCollection<SalesReturnDetail> _returnDetails;
        private readonly ObservableCollection<Customer> _customers;
        private readonly ObservableCollection<SalesInvoice> _originalInvoices;
        private ICollectionView _customersView;
        private ICollectionView _invoicesView;

        private readonly CultureInfo _culture;
        private decimal _taxRatePercent = 15m;

        private SalesReturn? _currentReturn;
        private SalesInvoice? _selectedOriginalInvoice;

        private const string TitleWarning = "تحذير";
        private const string TitleInfo = "تنبيه";

        private bool _isClosingConfirmed = false;

        public SalesReturnWindow(IServiceProvider serviceProvider)
        {
            try
            {
                InitializeComponent();

                _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                _salesInvoiceService = serviceProvider.GetRequiredService<ISalesInvoiceService>();
                _customerService = serviceProvider.GetRequiredService<ICustomerService>();
                _productService = serviceProvider.GetRequiredService<IProductService>();
                _priceHistoryService = serviceProvider.GetRequiredService<IPriceHistoryService>();
                _context = serviceProvider.GetRequiredService<AccountingDbContext>();

                // تنظيف شامل للمتتبع عند إنشاء النافذة
                _context.ChangeTracker.Clear();

                _returnDetails = new ObservableCollection<SalesReturnDetail>();
                _customers = new ObservableCollection<Customer>();
                _originalInvoices = new ObservableCollection<SalesInvoice>();

                // CollectionViews للفلترة الديناميكية
                _customersView = CollectionViewSource.GetDefaultView(_customers);
                _invoicesView = CollectionViewSource.GetDefaultView(_originalInvoices);

                _culture = new CultureInfo("ar-EG");
                _culture.NumberFormat.CurrencySymbol = "ج.م";

                dgItems.ItemsSource = _returnDetails;
                cmbCustomer.ItemsSource = _customersView;
                cmbOriginalInvoice.ItemsSource = _invoicesView;

                EnableEditMode(false);

                // إغلاق آمن
                Closing += SalesReturnWindow_Closing;

                // نظام Select All تلقائي لجميع TextBox
                EventManager.RegisterClassHandler(typeof(TextBox),
                    UIElement.GotKeyboardFocusEvent,
                    new KeyboardFocusChangedEventHandler(TextBox_GotKeyboardFocus));
                EventManager.RegisterClassHandler(typeof(TextBox),
                    UIElement.PreviewMouseLeftButtonDownEvent,
                    new MouseButtonEventHandler(TextBox_PreviewMouseLeftButtonDown), true);

                // تحميل البيانات
                Loaded += async (_, __) => await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل تهيئة نافذة مرتجع البيع:\n{ex}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // ===== تحميل البيانات =====
        private async Task LoadDataAsync()
        {
            try
            {
                _context.ChangeTracker.Clear();

                await LoadTaxRateAsync();
                _context.ChangeTracker.Clear();

                // العملاء
                var customers = await _customerService.GetAllCustomersAsync();
                _customers.Clear();
                foreach (var c in customers) _customers.Add(c);

                lblReturnNumber.Text = "سيتم التوليد تلقائياً عند الحفظ";
                dpReturnDate.SelectedDate = DateTime.Now;

                PrepareNewReturn();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadTaxRateAsync()
        {
            try
            {
                var setting = await _context.SystemSettings.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SettingKey == "TaxRate");
                var s = setting?.SettingValue;
                if (!string.IsNullOrWhiteSpace(s) &&
                    (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out var r) ||
                     decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out r)))
                {
                    _taxRatePercent = r;
                }
            }
            catch
            {
                // نحتفظ بالقيمة الافتراضية 15%
            }
        }

        // ===== مساعد تحويل آمن =====
        private static string NormalizeDigits(string? s) => s?
            .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
            .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9')
            .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
            .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
            .Replace('٫', '.') ?? "0";

        private bool TryParseDecimal(string? text, out decimal value)
        {
            text = NormalizeDigits(text ?? "0");
            return decimal.TryParse(text, NumberStyles.Any, _culture, out value) ||
                   decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value) ||
                   decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        // ===== نظام Select All وEnter=Tab =====
        private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox tb) tb.SelectAll();
        }

        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox tb && !tb.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                tb.Focus();
                tb.SelectAll();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            var element = Keyboard.FocusedElement as FrameworkElement;
            if (element == null) return;

            if (element is ComboBox cb)
            {
                e.Handled = true;
                if (cb.IsDropDownOpen && cb.SelectedIndex < 0 && cb.Items.Count > 0)
                    cb.SelectedIndex = 0;
                cb.IsDropDownOpen = false;
                MoveFocusToNextAndSelect();
                return;
            }

            if (element is TextBox)
            {
                e.Handled = true;
                MoveFocusToNextAndSelect();
                return;
            }

            e.Handled = true;
            MoveFocusToNextAndSelect();
        }

        private void MoveFocusToNextAndSelect()
        {
            var request = new TraversalRequest(FocusNavigationDirection.Next);
            var current = Keyboard.FocusedElement as UIElement;
            current?.MoveFocus(request);

            if (Keyboard.FocusedElement is TextBox nextTb)
                nextTb.SelectAll();
            else if (Keyboard.FocusedElement is ComboBox nextCb)
                nextCb.IsDropDownOpen = true;
        }

        // ===== أحداث العملاء =====
        private void cmbCustomer_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => CustomerSelectionChanged(sender, e);

        private async void CustomerSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (cmbCustomer?.SelectedItem is Customer customer)
            {
                lblPreviousBalance.Content = customer.Balance.ToString("C", _culture);
                await LoadCustomerInvoicesAsync(customer.CustomerId);
            }
            else
            {
                _originalInvoices.Clear();
                cmbOriginalInvoice.SelectedIndex = -1;
            }
        }

        private async Task LoadCustomerInvoicesAsync(int customerId)
        {
            try
            {
                // تحميل الفواتير المرحّلة للعميل
                var invoices = await _context.SalesInvoices
                    .Include(i => i.Items).ThenInclude(d => d.Product)
                    .Include(i => i.Items).ThenInclude(d => d.Unit)
                    .Where(i => i.CustomerId == customerId && i.Status == InvoiceStatus.Confirmed)
                    .OrderByDescending(i => i.InvoiceDate)
                    .Take(50) // نحدد آخر 50 فاتورة
                    .ToListAsync();

                _originalInvoices.Clear();
                foreach (var inv in invoices)
                {
                    _originalInvoices.Add(inv);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل فواتير العميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbCustomer_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                _customersView.Filter = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Customer dropdown error: {ex.Message}");
            }
        }

        private void cmbCustomer_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            try
            {
                var combo = (ComboBox)sender;
                var searchText = combo.Text + e.Text;

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var filterText = searchText.ToLowerInvariant();
                    _customersView.Filter = item =>
                        item is Customer cust &&
                        cust.CustomerName?.ToLowerInvariant().Contains(filterText) == true;

                    if (!combo.IsDropDownOpen)
                        combo.IsDropDownOpen = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Customer filter error: {ex.Message}");
            }
        }

        // ===== أحداث الفاتورة الأصلية =====
        private void cmbOriginalInvoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbOriginalInvoice.SelectedItem is SalesInvoice invoice)
            {
                _selectedOriginalInvoice = invoice;
                LoadOriginalInvoiceItems();
            }
        }

        private void LoadOriginalInvoiceItems()
        {
            try
            {
                if (_selectedOriginalInvoice?.Items == null) return;

                dgOriginalItems.ItemsSource = _selectedOriginalInvoice.Items;

                // عرض معلومات الفاتورة
                if (pnlOriginalInvoiceInfo != null)
                {
                    pnlOriginalInvoiceInfo.Visibility = Visibility.Visible;
                    lblOriginalInvoiceNumber.Text = _selectedOriginalInvoice.InvoiceNumber ?? "";
                    lblOriginalInvoiceDate.Text = _selectedOriginalInvoice.InvoiceDate.ToString("yyyy/MM/dd");
                    lblOriginalInvoiceTotal.Text = _selectedOriginalInvoice.NetTotal.ToString("C", _culture);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل تفاصيل الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===== إضافة بند مرتجع =====
        private void btnAddFromOriginal_Click(object sender, RoutedEventArgs e)
        {
            if (dgOriginalItems.SelectedItem is not SalesInvoiceItem originalItem)
            {
                MessageBox.Show("يرجى اختيار بند من الفاتورة الأصلية", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // التحقق من الكمية المرتجعة
                if (!TryParseDecimal(txtReturnQuantity.Text, out var returnQty) || returnQty <= 0)
                {
                    MessageBox.Show("يرجى إدخال كمية مرتجعة صالحة", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (returnQty > originalItem.Quantity)
                {
                    MessageBox.Show("الكمية المرتجعة تتجاوز الكمية الأصلية", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من الكمية المرتجعة مسبقاً
                var previouslyReturned = _returnDetails.Where(r => r.OriginalInvoiceDetailId == originalItem.SalesInvoiceItemId)
                    .Sum(r => r.Quantity);

                if (previouslyReturned + returnQty > originalItem.Quantity)
                {
                    MessageBox.Show($"إجمالي الكمية المرتجعة ({previouslyReturned + returnQty}) يتجاوز الكمية الأصلية ({originalItem.Quantity})",
                        TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var reason = txtReturnReason.Text.Trim();
                if (string.IsNullOrWhiteSpace(reason))
                {
                    MessageBox.Show("يرجى إدخال سبب الإرجاع", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var returnDetail = new SalesReturnDetail
                {
                    ProductId = originalItem.ProductId,
                    Product = originalItem.Product,
                    UnitId = originalItem.UnitId,
                    Unit = originalItem.Unit,
                    Quantity = returnQty,
                    UnitPrice = originalItem.UnitPrice,
                    TotalPrice = returnQty * originalItem.UnitPrice,
                    ReturnReason = reason,
                    OriginalInvoiceDetailId = originalItem.SalesInvoiceItemId
                };

                _returnDetails.Add(returnDetail);
                CalculateReturnTotals();
                ClearReturnInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إضافة البند: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteItem_Click(object sender, RoutedEventArgs e) => RemoveItemClick(sender, e);

        private void RemoveItemClick(object? sender, RoutedEventArgs e)
        {
            _context.ChangeTracker.Clear();

            if (!btnAddFromOriginal.IsEnabled)
            {
                MessageBox.Show("يجب تفعيل وضع التعديل أولاً", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sender is Button btn && btn.Tag is SalesReturnDetail detailFromTag)
            {
                _returnDetails.Remove(detailFromTag);
                CalculateReturnTotals();
                return;
            }

            if (dgItems.SelectedItem is SalesReturnDetail detail)
            {
                _returnDetails.Remove(detail);
                CalculateReturnTotals();
            }
            else
            {
                MessageBox.Show("يرجى تحديد البند المراد حذفه", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ClearReturnInputs()
        {
            txtReturnQuantity.Text = "1";
            txtReturnReason.Text = "";
            dgOriginalItems.SelectedIndex = -1;
        }

        private void CalculateReturnTotals()
        {
            try
            {
                if (lblSubTotal == null || lblTaxAmount == null || lblNetTotal == null) return;

                var subTotal = _returnDetails.Sum(d => d.TotalPrice);
                var taxAmount = decimal.Round(subTotal * (_taxRatePercent / 100m), 2);
                var netTotal = subTotal + taxAmount;

                lblSubTotal.Content = subTotal.ToString("C", _culture);
                lblTaxAmount.Content = taxAmount.ToString("C", _culture);
                lblNetTotal.Content = netTotal.ToString("C", _culture);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CalculateReturnTotals error: {ex.Message}");
            }
        }

        // ===== حفظ/طباعة =====
        private async Task<SalesReturn?> SaveReturnAsync()
        {
            try
            {
                _context.ChangeTracker.Clear();

                if (cmbCustomer.SelectedItem is not Customer customer)
                {
                    MessageBox.Show("يرجى اختيار العميل", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                if (_selectedOriginalInvoice == null)
                {
                    MessageBox.Show("يرجى اختيار الفاتورة الأصلية", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                if (!_returnDetails.Any())
                {
                    MessageBox.Show("يرجى إضافة بنود للمرتجع", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                var subTotal = _returnDetails.Sum(d => d.TotalPrice);
                var taxAmount = decimal.Round(subTotal * (_taxRatePercent / 100m), 2);
                var netTotal = subTotal + taxAmount;

                var detailsForSave = _returnDetails.Select(d => new SalesReturnDetail
                {
                    ProductId = d.ProductId,
                    UnitId = d.UnitId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    TotalPrice = d.TotalPrice,
                    ReturnReason = d.ReturnReason,
                    OriginalInvoiceDetailId = d.OriginalInvoiceDetailId
                }).ToList();

                var salesReturn = new SalesReturn
                {
                    ReturnDate = dpReturnDate.SelectedDate ?? DateTime.Now,
                    CustomerId = customer.CustomerId,
                    OriginalSalesInvoiceId = _selectedOriginalInvoice.SalesInvoiceId,
                    SubTotal = subTotal,
                    TaxAmount = taxAmount,
                    NetTotal = netTotal,
                    Notes = txtNotes.Text,
                    Status = InvoiceStatus.Draft,
                    CreatedBy = 1,
                    SalesReturnDetails = detailsForSave
                };

                SetBusy(true);

                // TODO: إنشاء SalesReturnService
                // var result = await _salesReturnService.CreateSalesReturnAsync(salesReturn);

                // مؤقتاً حفظ مباشر في قاعدة البيانات
                _context.SalesReturns.Add(salesReturn);
                await _context.SaveChangesAsync();

                _currentReturn = salesReturn;
                lblReturnNumber.Text = $"RET-{salesReturn.SalesReturnId}";

                EnableEditMode(false);
                UpdateButtonStates();

                MessageBox.Show("تم حفظ مرتجع البيع بنجاح!", "نجح الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
                return salesReturn;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ المرتجع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool isBusy)
        {
            btnSave.IsEnabled = !isBusy;
            btnPrint.IsEnabled = !isBusy;
            btnCancel.IsEnabled = !isBusy;
            btnAddFromOriginal.IsEnabled = !isBusy; // SalesReturn has no posting state currently
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e) => await SaveReturnAsync();

        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintCurrentReturn();

        private void PrintCurrentReturn()
        {
            try
            {
                if (_currentReturn?.SalesReturnId <= 0)
                {
                    MessageBox.Show("يجب حفظ المرتجع أولاً قبل الطباعة!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                MessageBox.Show("ميزة طباعة مرتجعات البيع ستتوفر قريباً", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // ===== زر جديد =====
        private void btnNew_Click(object sender, RoutedEventArgs e) => NewClick(sender, e);

        private void NewClick(object? sender, RoutedEventArgs e)
        {
            if (HasUnsavedChanges())
            {
                var result = MessageBox.Show(
                    "هناك بيانات غير محفوظة. هل تريد حفظ المرتجع قبل إنشاء مرتجع جديد؟",
                    "حفظ المرتجع",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Dispatcher.Invoke(async () =>
                    {
                        var saved = await SaveReturnAsync();
                        if (saved != null) PrepareNewReturn();
                    });
                    return;
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    return;
                }
            }

            PrepareNewReturn();
        }

        private void PrepareNewReturn()
        {
            ClearReturnForm();
            EnableEditMode(true);
            UpdateButtonStates();
        }

        private void ClearReturnForm()
        {
            _returnDetails.Clear();
            _currentReturn = null;
            _selectedOriginalInvoice = null;
            cmbCustomer.SelectedIndex = -1;
            cmbOriginalInvoice.SelectedIndex = -1;
            txtNotes.Text = "";
            dpReturnDate.SelectedDate = DateTime.Now;
            ClearReturnInputs();
            CalculateReturnTotals();
            lblReturnNumber.Text = "سيتم التوليد تلقائياً عند الحفظ";

            if (pnlOriginalInvoiceInfo != null)
                pnlOriginalInvoiceInfo.Visibility = Visibility.Collapsed;

            EnableEditMode(true);
        }

        // ===== إغلاق آمن =====
        private async void SalesReturnWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_isClosingConfirmed) return;

            if (!HasUnsavedChanges()) return;

            var result = MessageBox.Show(
                "هناك بيانات غير محفوظة. هل تريد حفظها قبل الخروج؟",
                "حفظ البيانات",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Cancel)
            {
                e.Cancel = true;
                return;
            }

            if (result == MessageBoxResult.Yes)
            {
                e.Cancel = true;
                var saved = await SaveReturnAsync();
                if (saved == null) return;

                _isClosingConfirmed = true;
                Close();
            }
        }

        private bool HasUnsavedChanges()
        {
            if (_currentReturn == null)
            {
                bool anyDetail = _returnDetails.Any();
                bool hasCustomer = cmbCustomer?.SelectedItem is Customer;
                bool hasNotes = !string.IsNullOrWhiteSpace(txtNotes?.Text);
                return anyDetail || hasCustomer || hasNotes;
            }

            return false; // للبساطة، مرتجعات محفوظة لا تتغير
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => Close();

        // ===== تعديل =====
        private void EnableEditMode(bool enabled)
        {
            cmbCustomer.IsEnabled = enabled;
            cmbOriginalInvoice.IsEnabled = enabled;
            dpReturnDate.IsEnabled = enabled;
            txtNotes.IsEnabled = enabled;

            btnAddFromOriginal.IsEnabled = enabled;
            txtReturnQuantity.IsEnabled = enabled;
            txtReturnReason.IsEnabled = enabled;

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var hasReturn = _currentReturn != null;
            btnPrint.IsEnabled = hasReturn;
        }

        // ===== اختصارات لوحة المفاتيح =====
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.S && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    btnSave_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.N && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    btnNew_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.P && e.KeyboardDevice.Modifiers == ModifierKeys.Control)
                {
                    btnPrint_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == Key.Escape)
                {
                    btnCancel_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnKeyDown error: {ex.Message}");
            }

            base.OnKeyDown(e);
        }
    }
}