using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AccountingSystem.Business;
using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.WPF.Models;
using AccountingSystem.WPF.Views;

namespace AccountingSystem.WPF.Views
{
    public partial class SalesInvoiceWindow : Window
    {
        private readonly ISalesInvoiceService _salesInvoiceService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly IPriceHistoryService _priceHistoryService;
        private readonly AccountingDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        private readonly ObservableCollection<SalesInvoiceItem> _invoiceDetails = new();
        private readonly ObservableCollection<Customer> _customers = new();
        private readonly ObservableCollection<Product> _products = new();
        private readonly ObservableCollection<Warehouse> _warehouses = new();
        private readonly ObservableCollection<Representative> _representatives = new();
        private ICollectionView _customersView;
        private ICollectionView _productsView;

        private readonly ObservableCollection<Unit> _productUnits = new();
        private readonly ObservableCollection<ProductUnit> _currentProductUnits = new();

        private readonly CultureInfo _culture;
        private decimal _taxRatePercent = 15m;
        private bool _taxOnNetOfDiscount = true;
        private decimal _currentUnitFactor = 1m;

        private SalesInvoice? _currentInvoice;
        private readonly List<int> _invoiceIds = new();

        private bool _isClosingConfirmed = false;
        private bool _justLoaded = true;
        private bool _userOpenedProduct = false;

        private CancellationTokenSource? _cts;

        public SalesInvoiceWindow(IServiceProvider serviceProvider)
        {
            InitializeComponent();

            // تأكد أن الترميز UTF8 (مفيد لو بتكتب ملفات/تقارير)
            Console.OutputEncoding = Encoding.UTF8;

            _serviceProvider     = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _salesInvoiceService = _serviceProvider.GetRequiredService<ISalesInvoiceService>();
            _customerService     = _serviceProvider.GetRequiredService<ICustomerService>();
            _productService      = _serviceProvider.GetRequiredService<IProductService>();
            _priceHistoryService = _serviceProvider.GetRequiredService<IPriceHistoryService>();
            _context             = _serviceProvider.GetRequiredService<AccountingDbContext>();

            _customersView = CollectionViewSource.GetDefaultView(_customers);
            _productsView  = CollectionViewSource.GetDefaultView(_products);

            _culture = new CultureInfo("ar-EG");
            _culture.NumberFormat.CurrencySymbol = "ج.م";

            // Bindings
            dgItems.ItemsSource           = _invoiceDetails;
            cmbCustomer.ItemsSource       = _customersView;
            cmbProduct.ItemsSource        = _productsView;
            cmbUnit.ItemsSource           = _productUnits;
            cmbWarehouse.ItemsSource      = _warehouses;
            cmbRepresentative.ItemsSource = _representatives;

            EnableEditMode(false);

            Closing += SalesInvoiceWindow_Closing;
            Loaded  += async (_, __) => await LoadDataAsync();

            // SelectAll لكل TextBox
            EventManager.RegisterClassHandler(typeof(TextBox),
                UIElement.GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler((s, e) => { if (s is TextBox t) t.SelectAll(); }));
            EventManager.RegisterClassHandler(typeof(TextBox),
                UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler((s, e) =>
                {
                    if (s is TextBox t && !t.IsKeyboardFocusWithin) { e.Handled = true; t.Focus(); t.SelectAll(); }
                }), true);

            // Delete
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Delete, (s, e) =>
            {
                if (!btnAddItem.IsEnabled) { ShowWarn("يجب تفعيل وضع التعديل أولاً"); return; }
                if (dgItems.SelectedItem is SalesInvoiceItem row)
                {
                    _invoiceDetails.Remove(row);
                    CalculateInvoiceTotals();
                }
            }));

            // Save/New
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Save, async (s, e) => await SaveInvoiceAsync()));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.New, (s, e) => btnNew_Click(this, new RoutedEventArgs())));
        }

        // ========= تحميل البيانات =========
        private async Task LoadDataAsync()
        {
            try
            {
                SetBusy(true);
                _cts?.Cancel();
                _cts = new CancellationTokenSource();
                var ct = _cts.Token;

                await LoadTaxRateAsync(ct);

                var customersTask = _customerService.GetAllCustomersAsync();
                var productsTask  = _productService.GetAllProductsAsync();
                await Task.WhenAll(customersTask, productsTask);

                _customers.Clear();
                foreach (var c in customersTask.Result) _customers.Add(c);

                _products.Clear();
                foreach (var p in productsTask.Result) _products.Add(p);

                lblInvoiceNumber.Text = "سيتم التوليد تلقائياً عند الحفظ";
                dpInvoiceDate.SelectedDate = DateTime.Now;
                PrepareNewInvoice();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل البيانات: {ex.Message}");
            }
            finally { SetBusy(false); }
        }

        private async Task LoadTaxRateAsync(CancellationToken ct)
        {
            try
            {
                var setting = await _context.SystemSettings.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SettingKey == "TaxRate", ct);
                if (setting != null)
                {
                    var s = setting.SettingValue;
                    if (!string.IsNullOrWhiteSpace(s) &&
                        (decimal.TryParse(s, NumberStyles.Any, CultureInfo.CurrentCulture, out var r) ||
                         decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out r)))
                    { _taxRatePercent = r; }
                }
            }
            catch { /* احتفظ بالافتراضي */ }
        }

        // ========= دورة حياة =========
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            _justLoaded = false;
            cmbProduct.Focus();
            _userOpenedProduct = false;
        }

        // ========= أدوات مساعدة =========
        private static string NormalizeDigits(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "0";
            s = s.Replace('٠','0').Replace('١','1').Replace('٢','2').Replace('٣','3').Replace('٤','4')
                 .Replace('٥','5').Replace('٦','6').Replace('٧','7').Replace('٨','8').Replace('٩','9')
                 .Replace('۰','0').Replace('۱','1').Replace('۲','2').Replace('۳','3').Replace('۴','4')
                 .Replace('۵','5').Replace('۶','6').Replace('۷','7').Replace('۸','8').Replace('۹','9')
                 .Replace('٫','.')
                 .Replace("٬","").Replace(",","");
            return s.Trim();
        }
        private static bool TryParseDecimal(string? text, out decimal value)
        {
            text = NormalizeDigits(text ?? "0");
            return decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out value);
        }
        private void ShowWarn(string msg)  => MessageBox.Show(msg, "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
        private void ShowInfo(string msg)  => MessageBox.Show(msg, "تنبيه",  MessageBoxButton.OK, MessageBoxImage.Information);
        private void ShowError(string msg) => MessageBox.Show(msg, "خطأ",    MessageBoxButton.OK, MessageBoxImage.Error);

        // ========= Enter = Tab (مع استثناءات) =========
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers != ModifierKeys.None) return;
            if (e.Key != Key.Enter) return;
            if (Keyboard.FocusedElement is not FrameworkElement element) return;
            if (element is TextBox tbt && tbt.AcceptsReturn) return;

            e.Handled = true;

            if (ReferenceEquals(element, txtDiscount))
            {
                btnAddItem.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                cmbProduct.Focus();
                _userOpenedProduct = false;
                cmbProduct.IsDropDownOpen = false;
                return;
            }

            if (ReferenceEquals(element, txtQuantity))
            {
                if (TryParseDecimal(txtQuantity.Text, out var quantity) && quantity > 0)
                    MoveFocusToNextAndSelect();
                else { ShowInfo("يرجى إدخال كمية صحيحة"); txtQuantity.SelectAll(); }
                return;
            }

            if (element is ComboBox cb && !ReferenceEquals(cb, cmbUnit))
            {
                if (cb.IsDropDownOpen && cb.SelectedIndex < 0 && cb.Items.Count > 0) cb.SelectedIndex = 0;
                cb.IsDropDownOpen = false;
                MoveFocusToNextAndSelect();
                return;
            }

            MoveFocusToNextAndSelect();
        }
        private static void MoveFocusToNextAndSelect()
        {
            var request = new TraversalRequest(FocusNavigationDirection.Next);
            (Keyboard.FocusedElement as UIElement)?.MoveFocus(request);
            if (Keyboard.FocusedElement is TextBox nextTb) nextTb.SelectAll();
        }

        // ========= فتح القوائم عند التفاعل =========
        private void cmbProduct_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        { if (!_justLoaded && _userOpenedProduct) cmbProduct.IsDropDownOpen = true; }
        private void cmbUnit_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        { if (_userOpenedProduct) cmbUnit.IsDropDownOpen = true; }
        private void cmbProduct_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.F4 ||
                e.Key == Key.Back || e.Key == Key.Delete ||
                (e.Key >= Key.A && e.Key <= Key.Z) ||
                (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9))
            { cmbProduct.IsDropDownOpen = true; _userOpenedProduct = true; }
        }
        private void cmbUnit_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (cmbUnit.IsDropDownOpen && cmbUnit.SelectedIndex < 0 && cmbUnit.Items.Count > 0)
                    cmbUnit.SelectedIndex = 0;
                cmbUnit.IsDropDownOpen = false;
                txtQuantity.Focus();
                txtQuantity.SelectAll();
            }
            else if (e.Key == Key.Down || e.Key == Key.Up || e.Key == Key.F4)
            { cmbUnit.IsDropDownOpen = true; }
        }

        // ========= بحث سريع =========
        private void txtQuickSearch_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            var q = (txtQuickSearch.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(q)) return;

            Product? match =
                _products.FirstOrDefault(p => string.Equals(p.Barcode, q, StringComparison.OrdinalIgnoreCase)
                                           || string.Equals(p.ProductCode, q, StringComparison.OrdinalIgnoreCase))
                ?? _products.FirstOrDefault(p => (p.ProductName ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);

            if (match != null)
            {
                cmbProduct.SelectedItem = match;
                if (match.MainUnit != null)
                    cmbUnit.SelectedItem = _productUnits.FirstOrDefault(u => u.UnitId == match.MainUnitId);
                txtQuantity.Focus();
                txtQuantity.SelectAll();
                txtQuickSearch.Clear();
            }
            else { ShowInfo("لم يتم العثور على منتج مطابق"); txtQuickSearch.SelectAll(); }
        }

        // ========= العملاء =========
        private void cmbCustomer_SelectionChanged(object sender, SelectionChangedEventArgs e) => CustomerSelectionChanged(sender, e);
        private async void CustomerSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (cmbCustomer?.SelectedItem is Customer customer && lblPreviousBalance != null)
            {
                lblPreviousBalance.Text = customer.Balance.ToString("C", _culture);
                if (cmbProduct?.SelectedItem is Product product) await UpdateCustomerLastPriceAsync(product.ProductId);
            }
        }
        private void cmbCustomer_DropDownOpened(object sender, EventArgs e)
        { try { _customersView.Filter = null; } catch { } }
        private void cmbCustomer_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                var combo = (ComboBox)sender;
                var searchText = combo.Text + e.Text;
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var filterText = searchText.ToLowerInvariant();
                    using (_customersView.DeferRefresh())
                    {
                        _customersView.Filter = item => item is Customer cust &&
                            cust.CustomerName?.ToLowerInvariant().Contains(filterText) == true;
                    }
                    if (!combo.IsDropDownOpen) combo.IsDropDownOpen = true;
                }
            }
            catch { }
        }
        private void cmbCustomer_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1) { ShowCustomerSearchDialog(); e.Handled = true; }
            else if (e.Key == Key.Enter) { cmbWarehouse.Focus(); e.Handled = true; }
        }

        // ========= المنتجات =========
        private void cmbProduct_SelectionChanged(object sender, SelectionChangedEventArgs e) => ProductSelectionChanged(sender, e);
        private async void ProductSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (cmbProduct?.SelectedItem is Product product)
            {
                txtStock.Text     = product.CurrentStock.ToString("F2");
                txtUnitPrice.Text = product.SalePrice.ToString("F2");
                await LoadProductUnitsAsync(product.ProductId);
                await UpdatePriceInfoAsync(product.ProductId);
            }
        }
        private void cmbProduct_DropDownOpened(object sender, EventArgs e)
        { try { _productsView.Filter = null; } catch { } }
        private void cmbProduct_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                var combo = (ComboBox)sender;
                var searchText = combo.Text + e.Text;
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var filterText = searchText.ToLowerInvariant();
                    using (_productsView.DeferRefresh())
                    {
                        _productsView.Filter = item => item is Product p &&
                            ((p.ProductName ?? string.Empty).ToLowerInvariant().Contains(filterText) ||
                             (p.ProductCode ?? string.Empty).ToLowerInvariant().Contains(filterText));
                    }
                    if (!combo.IsDropDownOpen) combo.IsDropDownOpen = true;
                    _userOpenedProduct = true;
                }
            }
            catch { }
        }

        private async Task UpdatePriceInfoAsync(int productId)
        {
            try
            {
                if (cmbProduct?.SelectedItem is Product selectedProduct)
                {
                    pnlProductInfo.Visibility = Visibility.Visible;
                    lblSelectedProductName.Text = selectedProduct.ProductName;
                    lblCurrentStock.Text = $"{selectedProduct.CurrentStock:F2} {(selectedProduct.MainUnit?.UnitName ?? "")}";
                }

                var prodForPurchase = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);
                var lastPurchasePrice = prodForPurchase?.PurchasePrice;
                lblLastPurchasePrice.Text = lastPurchasePrice?.ToString("C", _culture) ?? "لا يوجد";
                await UpdateCustomerLastPriceAsync(productId);
            }
            catch
            { lblLastPurchasePrice.Text = "خطأ"; lblLastCustomerPrice.Text = "خطأ"; }
        }

        private async Task UpdateCustomerLastPriceAsync(int productId)
        {
            try
            {
                if (cmbCustomer?.SelectedItem is Customer)
                {
                    var pr = await _context.Products.AsNoTracking().FirstOrDefaultAsync(p => p.ProductId == productId);
                    var lastPrice = pr?.SalePrice;
                    lblLastCustomerPrice.Text = lastPrice.HasValue ? lastPrice.Value.ToString("C", _culture) : "لا يوجد";
                    if (lastPrice.HasValue) txtUnitPrice.Text = lastPrice.Value.ToString("F2");
                }
                else { lblLastCustomerPrice.Text = "-"; }
            }
            catch { lblLastCustomerPrice.Text = "خطأ"; }
        }

        private async Task LoadProductUnitsAsync(int productId)
        {
            try
            {
                var selectedProduct = cmbProduct?.SelectedItem as Product;
                _productUnits.Clear();
                _currentProductUnits.Clear();

                if (selectedProduct?.MainUnit != null) _productUnits.Add(selectedProduct.MainUnit);

                var productUnits = await _context.ProductUnits
                    .AsNoTracking()
                    .Include(pu => pu.Unit)
                    .Where(pu => pu.ProductId == productId && pu.IsActive)
                    .ToListAsync();

                foreach (var pu in productUnits)
                {
                    _currentProductUnits.Add(pu);
                    if (pu.Unit != null && !_productUnits.Any(u => u.UnitId == pu.UnitId))
                        _productUnits.Add(pu.Unit!);
                }

                if (_productUnits.Any()) cmbUnit.SelectedIndex = 0;
            }
            catch { }
        }

        private async void cmbUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdatePriceForSelectedUnit();
            if (cmbProduct?.SelectedItem is Product product) await UpdateCustomerLastPriceAsync(product.ProductId);
            CalculateLineTotal();
        }

        private void UpdatePriceForSelectedUnit()
        {
            if (cmbProduct?.SelectedItem is not Product selectedProduct) return;
            if (cmbUnit.SelectedItem is not Unit selectedUnit) return;

            try
            {
                decimal factor = 1m;
                if (selectedUnit.UnitId == selectedProduct.MainUnitId)
                { txtUnitPrice.Text = selectedProduct.SalePrice.ToString("F2"); }
                else
                {
                    var productUnit = _currentProductUnits.FirstOrDefault(pu => pu.UnitId == selectedUnit.UnitId);
                    if (productUnit != null)
                    {
                        factor = productUnit.ConversionFactor > 0 ? productUnit.ConversionFactor : 1m;
                        txtUnitPrice.Text = (selectedProduct.SalePrice * factor).ToString("F2");
                    }
                    else { txtUnitPrice.Text = selectedProduct.SalePrice.ToString("F2"); }
                }

                _currentUnitFactor = factor;
                txtStock.Text = (selectedProduct.CurrentStock / _currentUnitFactor).ToString("F2");
            }
            catch { txtUnitPrice.Text = "0"; txtStock.Text = "0"; }
        }

        // ========= حسابات =========
        private void QuantityChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) CalculateLineTotal(); }
        private void UnitPriceChanged(object sender, TextChangedEventArgs e) { if (IsLoaded) CalculateLineTotal(); }
        private void DiscountChanged(object sender, TextChangedEventArgs e)   { if (IsLoaded) CalculateLineTotal(); }

        private void CalculateLineTotal()
        {
            try
            {
                if (txtQuantity == null || txtUnitPrice == null || txtDiscount == null || lblTotal == null) return;
                TryParseDecimal(txtQuantity.Text, out var q);
                TryParseDecimal(txtUnitPrice.Text, out var p);
                TryParseDecimal(txtDiscount.Text, out var d);
                var gross   = q * p;
                var lineNet = Math.Max(0, gross - d);
                lblTotal.Text = lineNet.ToString("C", _culture);
            }
            catch { }
        }

        private void txtPaidAmount_TextChanged(object sender, TextChangedEventArgs e)
        { if (IsLoaded) CalculateInvoiceTotals(); }

        private void CalculateInvoiceTotals()
        {
            try
            {
                if (lblSubTotal == null || lblTotalDiscount == null || lblTaxAmount == null || lblNetTotal == null || lblRemainingAmount == null) return;

                foreach (var d in _invoiceDetails)
                {
                    d.TotalPrice = d.Quantity * d.UnitPrice;
                    d.NetAmount  = Math.Max(0, d.TotalPrice - d.DiscountAmount);
                }

                var subTotal      = _invoiceDetails.Sum(d => d.TotalPrice);
                var totalDiscount = _invoiceDetails.Sum(d => d.DiscountAmount);
                var baseForTax    = _taxOnNetOfDiscount ? Math.Max(0, subTotal - totalDiscount) : subTotal;
                var taxAmount     = decimal.Round(baseForTax * (_taxRatePercent / 100m), 2);
                var netTotal      = baseForTax + taxAmount;

                decimal paidAmount = 0;
                if (!string.IsNullOrWhiteSpace(txtPaidAmount.Text)) TryParseDecimal(txtPaidAmount.Text, out paidAmount);
                var remainingAmount = netTotal - paidAmount;

                lblSubTotal.Text        = subTotal.ToString("C", _culture);
                lblTotalDiscount.Text   = totalDiscount.ToString("C", _culture);
                lblTaxAmount.Text       = taxAmount.ToString("C", _culture);
                lblNetTotal.Text        = netTotal.ToString("C", _culture);
                lblRemainingAmount.Text = remainingAmount.ToString("C", _culture);
                lblTotalItems.Text      = $"عدد الأصناف: {_invoiceDetails.Count}";

                if (FindName("lblQuickItemsCount") is TextBlock qi) qi.Text = _invoiceDetails.Count.ToString();
                if (FindName("lblQuickDiscount")  is TextBlock qd) qd.Text  = totalDiscount.ToString("N2");
                if (FindName("lblQuickNet")       is TextBlock qn) qn.Text  = netTotal.ToString("N2");
            }
            catch { }
        }

        // ========= إضافة/حذف =========
        private async void btnAddItem_Click(object sender, RoutedEventArgs e) => await AddItemClickAsync();

        private async Task AddItemClickAsync()
        {
            try
            {
                var selectedCustomer = cmbCustomer.SelectedItem as Customer;

                // نافذة إدخال متقدمة (لو متاحة)
                var dialog = new ItemEntryDialog(selectedCustomer?.CustomerId ?? 0) { Owner = this };
                if (dialog.ShowDialog() == true && dialog.ResultItem != null)
                {
                    var it = dialog.ResultItem;
                    AddOrMergeLine(it.Product!, it.Unit!, it.Quantity, it.UnitPrice, it.DiscountAmount);
                    CalculateInvoiceTotals();
                    if (dialog.SaveAndContinue) await AddItemClickAsync();
                    return;
                }

                // احتياطي: من شريط الإدخال
                if (cmbProduct.SelectedItem is not Product product ||
                    cmbUnit.SelectedItem    is not Unit    unit ||
                    !TryParseDecimal(txtQuantity.Text, out var quantity) ||
                    !TryParseDecimal(txtUnitPrice.Text, out var unitPrice) ||
                    !TryParseDecimal(txtDiscount.Text, out var discount))
                { ShowWarn("يرجى ملء جميع البيانات المطلوبة بشكل صحيح"); return; }

                if (quantity <= 0 || unitPrice < 0 || discount < 0)
                { ShowWarn("قِيَم غير صالحة (كمية > 0، سعر/خصم ≥ 0)"); return; }
                if (discount > quantity * unitPrice)
                { ShowWarn("الخصم يتجاوز إجمالي السطر"); return; }

                var productUnit = _currentProductUnits.FirstOrDefault(pu => pu.UnitId == unit.UnitId);
                var factor = (productUnit?.ConversionFactor > 0 ? productUnit.ConversionFactor : 1m);
                _currentUnitFactor = factor;

                var requestedInBase = quantity * factor;
                var availableInBase = product.CurrentStock;
                if (requestedInBase > availableInBase)
                { ShowWarn("الكمية المطلوبة تتجاوز المخزون المتاح"); return; }

                AddOrMergeLine(product, unit, quantity, unitPrice, discount);
                CalculateInvoiceTotals();
                ClearItemInputs();
            }
            catch (Exception ex) { ShowError($"خطأ في إضافة المنتج: {ex.Message}"); }
        }

        private void AddOrMergeLine(Product product, Unit unit, decimal quantity, decimal unitPrice, decimal discount)
        {
            var existing = _invoiceDetails.FirstOrDefault(d => d.ProductId == product.ProductId && d.UnitId == unit.UnitId);
            if (existing != null)
            {
                existing.Quantity   += quantity;
                existing.TotalPrice  = existing.Quantity * existing.UnitPrice;
                existing.NetAmount   = Math.Max(0, existing.TotalPrice - existing.DiscountAmount);
                dgItems.Items.Refresh();
            }
            else
            {
                _invoiceDetails.Add(new SalesInvoiceItem
                {
                    ProductId = product.ProductId,
                    Product   = product,
                    UnitId    = unit.UnitId,
                    Unit      = unit,
                    Quantity  = quantity,
                    UnitPrice = unitPrice,
                    DiscountAmount = discount,
                    TotalPrice = quantity * unitPrice,
                    NetAmount  = Math.Max(0, (quantity * unitPrice) - discount)
                });
            }
        }

        private void btnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (!btnAddItem.IsEnabled) { ShowWarn("يجب تفعيل وضع التعديل أولاً"); return; }
            if (sender is Button btn && btn.Tag is SalesInvoiceItem detailFromTag)
            { _invoiceDetails.Remove(detailFromTag); CalculateInvoiceTotals(); return; }
            if (dgItems.SelectedItem is SalesInvoiceItem detail)
            { _invoiceDetails.Remove(detail); CalculateInvoiceTotals(); }
            else { ShowWarn("يرجى تحديد البند المراد حذفه"); }
        }

        private void ClearItemInputs()
        {
            cmbProduct.SelectedIndex = -1;
            cmbUnit.SelectedIndex    = -1;
            txtQuantity.Text         = "1";
            txtUnitPrice.Text        = "0";
            txtDiscount.Text         = "0";
            txtStock.Text            = string.Empty;
            lblTotal.Text            = "0.00";

            lblSelectedProductName.Text = "-";
            lblCurrentStock.Text        = "0.00";
            lblLastPurchasePrice.Text   = "-";
            lblLastCustomerPrice.Text   = "-";
            pnlProductInfo.Visibility   = Visibility.Collapsed;

            cmbProduct.Focus();
            _userOpenedProduct = false;
            cmbProduct.IsDropDownOpen = false;
        }

        // ========= حفظ/طباعة =========
        private async Task<SalesInvoice?> SaveInvoiceAsync()
        {
            try
            {
                if (cmbCustomer.SelectedItem is not Customer customer)
                { ShowWarn("يرجى اختيار العميل"); return null; }
                if (!_invoiceDetails.Any())
                { ShowWarn("يرجى إضافة منتجات للفاتورة"); return null; }

                var subTotal      = _invoiceDetails.Sum(d => d.TotalPrice);
                var totalDiscount = _invoiceDetails.Sum(d => d.DiscountAmount);
                var baseForTax    = _taxOnNetOfDiscount ? Math.Max(0, subTotal - totalDiscount) : subTotal;
                var taxAmount     = decimal.Round(baseForTax * (_taxRatePercent / 100m), 2);
                var netTotal      = baseForTax + taxAmount;

                TryParseDecimal(txtPaidAmount.Text, out var paidAmount);

                var detailsForSave = _invoiceDetails.Select(d => new SalesInvoiceItem
                {
                    ProductId      = d.ProductId,
                    UnitId         = d.UnitId,
                    Quantity       = d.Quantity,
                    UnitPrice      = d.UnitPrice,
                    DiscountAmount = d.DiscountAmount,
                    TotalPrice     = d.TotalPrice,
                    NetAmount      = d.NetAmount
                }).ToList();

                var invoice = new SalesInvoice
                {
                    InvoiceDate         = dpInvoiceDate.SelectedDate ?? DateTime.Now,
                    CustomerId          = customer.CustomerId,
                    SubTotal            = subTotal,
                    TaxAmount           = taxAmount,
                    DiscountAmount      = totalDiscount,
                    NetTotal            = netTotal,
                    PaidAmount          = paidAmount,
                    RemainingAmount     = Math.Max(0, netTotal - paidAmount),
                    Notes               = txtNotes.Text,
                    Status              = InvoiceStatus.Draft,
                    CreatedBy           = "system",
                    Items               = detailsForSave
                };

                SetBusy(true);
                var createResult = await _salesInvoiceService.CreateSalesInvoiceAsync(invoice);
                if (createResult?.IsSuccess != true || createResult.Data == null)
                { ShowError($"فشل في حفظ الفاتورة: {createResult?.Message ?? "غير متوقع"}"); return null; }

                var saved = createResult.Data;
                _currentInvoice = saved;
                lblInvoiceNumber.Text = saved.InvoiceNumber;

                EnableEditMode(false);
                UpdateButtonStates();

                // ترحيل بعد الحفظ
                try
                {
                    var posted = await _salesInvoiceService.PostSalesInvoiceAsync(saved.SalesInvoiceId);
                    if (posted != null)
                    {
                        _currentInvoice = posted;
                        var refreshedCustomer = await _customerService.GetCustomerByIdAsync(posted.CustomerId);
                        if (refreshedCustomer != null) lblPreviousBalance.Text = refreshedCustomer.Balance.ToString("C", _culture);
                        await RefreshStocksForInvoiceAsync(posted);

                        _invoiceIds.Clear();
                        var allIds = await _context.SalesInvoices.AsNoTracking()
                            .OrderBy(i => i.SalesInvoiceId)
                            .Select(i => i.SalesInvoiceId).ToListAsync();
                        _invoiceIds.AddRange(allIds);
                        UpdateButtonStates();
                    }
                }
                catch (Exception postEx)
                { ShowInfo($"تم حفظ الفاتورة ولكن فشل الترحيل:\n{postEx.Message}"); }

                await ShowPostSaveDialogAsync(_currentInvoice ?? saved);
                return saved;
            }
            catch (Exception ex) { ShowError($"خطأ في حفظ الفاتورة: {ex.Message}"); return null; }
            finally { SetBusy(false); }
        }

        private async Task RefreshStocksForInvoiceAsync(SalesInvoice invoice)
        {
            try
            {
                if (invoice?.Items == null) return;
                var productIds = invoice.Items.Select(d => d.ProductId).Distinct().ToList();
                foreach (var pid in productIds)
                {
                    var latest = await _productService.GetProductByIdAsync(pid);
                    if (latest == null) continue;
                    var local = _products.FirstOrDefault(p => p.ProductId == pid);
                    if (local != null)
                    {
                        local.CurrentStock = latest.CurrentStock;
                        if (Equals(cmbProduct.SelectedItem, local)) txtStock.Text = local.CurrentStock.ToString("F2");
                    }
                }
            }
            catch { }
        }

        private void SetBusy(bool isBusy)
        {
            btnSave.IsEnabled    = !isBusy;
            btnPrint.IsEnabled   = !isBusy;
            btnCancel.IsEnabled  = !isBusy;
            btnAddItem.IsEnabled = !isBusy && (_currentInvoice == null || !_currentInvoice.IsPosted);
            if (FindName("BusyOverlay") is Grid overlay) overlay.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e) => await SaveInvoiceAsync();

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentInvoice?.SalesInvoiceId <= 0)
                { ShowInfo("يجب حفظ الفاتورة أولاً قبل الطباعة!"); return; }
                var preview = new InvoicePrintPreview(_currentInvoice!, _context);
                preview.ShowDialog();
            }
            catch (Exception ex) { ShowError($"خطأ في الطباعة: {ex.Message}"); }
        }

        // ========= جديد =========
        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            if (HasUnsavedChanges())
            {
                var r = MessageBox.Show("هناك بيانات غير محفوظة. حفظ قبل إنشاء فاتورة جديدة؟", "حفظ الفاتورة", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (r == MessageBoxResult.Yes)
                {
                    Dispatcher.Invoke(async () =>
                    {
                        var saved = await SaveInvoiceAsync();
                        if (saved != null) PrepareNewInvoice();
                    });
                    return;
                }
                else if (r == MessageBoxResult.Cancel) return;
            }
            PrepareNewInvoice();
        }

        private void PrepareNewInvoice()
        {
            ClearInvoiceForm();
            EnableEditMode(true);
            UpdateButtonStates();
        }

        private void ClearInvoiceForm()
        {
            _invoiceDetails.Clear();
            _currentInvoice = null;
            cmbCustomer.SelectedIndex = -1;
            txtNotes.Text     = string.Empty;
            txtPaidAmount.Text= "0";
            dpInvoiceDate.SelectedDate = DateTime.Now;
            ClearItemInputs();
            CalculateInvoiceTotals();
            lblInvoiceNumber.Text = "سيتم التوليد تلقائياً عند الحفظ";
            EnableEditMode(true);
            cmbProduct.Focus();
            _userOpenedProduct = false;
            cmbProduct.IsDropDownOpen = false;
        }

        // ========= إغلاق آمن =========
        private async void SalesInvoiceWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_isClosingConfirmed) return;
            if (!HasUnsavedChanges()) return;

            var result = MessageBox.Show("هناك بيانات غير محفوظة. هل تريد حفظها قبل الخروج؟", "حفظ البيانات", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Cancel) { e.Cancel = true; return; }
            if (result == MessageBoxResult.Yes)
            {
                e.Cancel = true;
                var saved = await SaveInvoiceAsync();
                if (saved == null) return;
                _isClosingConfirmed = true;
                Close();
            }
        }

        private bool HasUnsavedChanges()
        {
            if (_currentInvoice == null)
            {
                bool anyDetail   = _invoiceDetails.Any();
                bool hasCustomer = cmbCustomer?.SelectedItem is Customer;
                bool hasPaid     = TryParseDecimal(txtPaidAmount?.Text, out var paidAmount) && paidAmount != 0;
                bool hasNotes    = !string.IsNullOrWhiteSpace(txtNotes?.Text);
                return anyDetail || hasCustomer || hasPaid || hasNotes;
            }
            if (_currentInvoice.IsPosted) return false;

            var changed =
                   (_currentInvoice.CustomerId != (cmbCustomer?.SelectedItem as Customer)?.CustomerId)
                || (_currentInvoice.InvoiceDate.Date != (dpInvoiceDate.SelectedDate ?? DateTime.Now).Date)
                || (!TryParseDecimal(txtPaidAmount.Text, out var paid) ? _currentInvoice.PaidAmount != 0 : paid != _currentInvoice.PaidAmount)
                || ((_currentInvoice.Notes ?? string.Empty) != (txtNotes.Text ?? string.Empty))
                || (_currentInvoice.Items?.Count != _invoiceDetails.Count)
                || !_currentInvoice.Items.OrderBy(d => (d.ProductId, d.UnitId))
                        .Zip(_invoiceDetails.OrderBy(d => (d.ProductId, d.UnitId)), (a, b) =>
                            a.ProductId == b.ProductId && a.UnitId == b.UnitId && a.Quantity == b.Quantity &&
                            a.UnitPrice == b.UnitPrice && a.DiscountAmount == b.DiscountAmount)
                        .All(eq => eq);
            return changed;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => Close();

        // ========= ما بعد الحفظ =========
        private async Task ShowPostSaveDialogAsync(SalesInvoice savedInvoice)
        {
            try
            {
                if (savedInvoice == null) { ShowError("لا يمكن عرض نافذة ما بعد الحفظ"); return; }
                if (savedInvoice.Customer == null && savedInvoice.CustomerId > 0)
                {
                    var customer = await _context.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.CustomerId == savedInvoice.CustomerId);
                    if (customer != null)
                        savedInvoice.Customer = customer;
                }
                if (string.IsNullOrEmpty(savedInvoice.InvoiceNumber))
                    savedInvoice.InvoiceNumber = $"INV-{savedInvoice.SalesInvoiceId}";

                var dialog = new PostInvoiceDialog(savedInvoice) { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    if (dialog.GetPrintInvoice()) PrintInvoice(savedInvoice);
                    if (dialog.GetCollectPayment() && dialog.GetPaymentAmount() > 0) await ProcessPaymentAsync(savedInvoice, dialog.GetPaymentAmount());
                    if (dialog.GetOpenNewInvoice()) { ClearInvoiceForm(); PrepareNewInvoice(); }
                }
            }
            catch (Exception ex) { ShowError($"خطأ في ما بعد الحفظ: {ex.Message}"); }
        }

        private void PrintInvoice(SalesInvoice invoice)
        {
            try
            {
                if (invoice?.SalesInvoiceId > 0)
                {
                    var preview = new InvoicePrintPreview(invoice, _context);
                    preview.ShowDialog();
                }
                else { ShowWarn("فاتورة غير صالحة للطباعة!"); }
            }
            catch (Exception ex) { ShowError($"خطأ في طباعة الفاتورة: {ex.Message}"); }
        }

        private async Task ProcessPaymentAsync(SalesInvoice invoice, decimal paymentAmount)
        {
            try
            {
                invoice.PaidAmount     += paymentAmount;
                invoice.RemainingAmount = Math.Max(0, invoice.NetTotal - invoice.PaidAmount);
                _context.SalesInvoices.Update(invoice);
                await _context.SaveChangesAsync();
                txtPaidAmount.Text = invoice.PaidAmount.ToString("F2");
                CalculateInvoiceTotals();
                ShowInfo($"تم تسجيل دفعة بمبلغ {paymentAmount.ToString("C", _culture)} بنجاح!");
            }
            catch (Exception ex) { ShowError($"خطأ في تسجيل الدفع: {ex.Message}"); }
        }

        // ========= تنقل بين الفواتير =========
        private async void btnPreviousInvoice_Click(object sender, RoutedEventArgs e) => await NavigateToInvoice(NavigationDirection.Previous);
        private async void btnNextInvoice_Click(object sender, RoutedEventArgs e)     => await NavigateToInvoice(NavigationDirection.Next);

        private enum NavigationDirection { Previous, Next }

        private async Task NavigateToInvoice(NavigationDirection direction)
        {
            try
            {
                if (_currentInvoice == null) { ShowInfo("لا توجد فاتورة حالية للتنقل منها"); return; }
                if (_invoiceIds.Count == 0) await LoadInvoiceIdsAsync();
                var currentIndex = _invoiceIds.IndexOf(_currentInvoice.SalesInvoiceId);
                if (currentIndex == -1) { ShowError("لا يمكن تحديد موقع الفاتورة الحالية"); return; }
                int newIndex = direction == NavigationDirection.Previous ? currentIndex - 1 : currentIndex + 1;
                if (newIndex < 0) { ShowInfo("هذه أول فاتورة"); return; }
                if (newIndex >= _invoiceIds.Count) { ShowInfo("هذه آخر فاتورة"); return; }
                await LoadInvoiceByIdAsync(_invoiceIds[newIndex]);
            }
            catch (Exception ex) { ShowError($"خطأ في التنقل: {ex.Message}"); }
        }

        private async Task LoadInvoiceIdsAsync()
        {
            try
            {
                var ids = await _context.SalesInvoices.AsNoTracking()
                    .OrderBy(i => i.SalesInvoiceId)
                    .Select(i => i.SalesInvoiceId)
                    .ToListAsync();
                _invoiceIds.Clear();
                _invoiceIds.AddRange(ids);
            }
            catch (Exception ex) { ShowError($"خطأ في تحميل قائمة الفواتير: {ex.Message}"); }
        }

        private async Task LoadInvoiceByIdAsync(int invoiceId)
        {
            try
            {
                SetBusy(true);
                var invoice = await _context.SalesInvoices.AsNoTracking()
                    .Include(i => i.Customer)
                    .Include(i => i.Items).ThenInclude(d => d.Product)
                    .Include(i => i.Items).ThenInclude(d => d.Unit)
                    .FirstOrDefaultAsync(i => i.SalesInvoiceId == invoiceId);
                if (invoice == null) { ShowError("لا يمكن العثور على الفاتورة"); return; }
                LoadInvoiceToForm(invoice);
            }
            catch (Exception ex) { ShowError($"خطأ في تحميل الفاتورة: {ex.Message}"); }
            finally { SetBusy(false); }
        }

        private void LoadInvoiceToForm(SalesInvoice invoice)
        {
            try
            {
                _currentInvoice = invoice;
                lblInvoiceNumber.Text      = invoice.InvoiceNumber;
                dpInvoiceDate.SelectedDate = invoice.InvoiceDate;
                cmbCustomer.SelectedItem   = _customers.FirstOrDefault(c => c.CustomerId == invoice.CustomerId);
                txtNotes.Text              = invoice.Notes ?? string.Empty;
                txtPaidAmount.Text         = invoice.PaidAmount.ToString("F2");

                _invoiceDetails.Clear();
                foreach (var detail in invoice.Items) _invoiceDetails.Add(detail);

                CalculateInvoiceTotals();
                EnableEditMode(false);
                UpdateButtonStates();
            }
            catch (Exception ex) { ShowError($"خطأ في عرض الفاتورة: {ex.Message}"); }
        }

        // ========= تعديل =========
        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentInvoice == null) { ShowInfo("لا توجد فاتورة للتعديل"); return; }
                if (_currentInvoice.IsPosted) { ShowWarn("لا يمكن تعديل فاتورة مرحلة"); return; }
                EnableEditMode(true);
                ShowInfo("تم تفعيل وضع التعديل. يمكنك الآن تعديل الفاتورة");
            }
            catch (Exception ex) { ShowError($"خطأ في تفعيل التعديل: {ex.Message}"); }
        }

        private void EnableEditMode(bool enabled)
        {
            cmbCustomer.IsEnabled = enabled;
            dpInvoiceDate.IsEnabled = enabled;
            txtNotes.IsEnabled = enabled;
            txtPaidAmount.IsEnabled = enabled;

            btnAddItem.IsEnabled = enabled;
            cmbProduct.IsEnabled = enabled;
            cmbUnit.IsEnabled = enabled;
            txtQuantity.IsEnabled = enabled;
            txtUnitPrice.IsEnabled = enabled;
            txtDiscount.IsEnabled = enabled;

            btnSearchCustomer.IsEnabled = enabled;
            btnSearchProduct.IsEnabled  = enabled;
            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var hasInvoice = _currentInvoice != null;
            var isPosted   = hasInvoice && _currentInvoice!.IsPosted;
            var canEdit    = hasInvoice && !isPosted;
            btnPrint.IsEnabled = hasInvoice;
            (FindName("btnEdit") as Button)            ?.SetValue(IsEnabledProperty, canEdit);
            (FindName("btnPreviousInvoice") as Button) ?.SetValue(IsEnabledProperty, hasInvoice);
            (FindName("btnNextInvoice") as Button)     ?.SetValue(IsEnabledProperty, hasInvoice);
        }

        // ========= حوارات البحث =========
        private void btnSearchCustomer_Click(object sender, RoutedEventArgs e) => ShowCustomerSearchDialog();
        private void btnSearchProduct_Click(object sender, RoutedEventArgs e)  => ShowProductSearchDialog();
        private void btnSearchInvoices_Click(object sender, RoutedEventArgs e) => ShowInvoiceSearchDialog();

        private void ShowCustomerSearchDialog()
        {
            try
            {
                var dialog = new CustomerSearchDialog(_customers.ToList()) { Owner = this };
                if (dialog.ShowDialog() == true && dialog.SelectedCustomer != null)
                    cmbCustomer.SelectedItem = dialog.SelectedCustomer;
            }
            catch (Exception ex) { ShowError($"خطأ في فتح بحث العملاء: {ex.Message}"); }
        }

        private void ShowProductSearchDialog()
        {
            try
            {
                var dialog = new ProductSearchDialog(_products.Select(p => ProductSearchItem.FromRaw(p)).ToList()) { Owner = this };
                if (dialog.ShowDialog() == true && dialog.Selected != null)
                    cmbProduct.SelectedItem = _products.FirstOrDefault(p => p.ProductId == dialog.Selected.ProductId);
            }
            catch (Exception ex) { ShowError($"خطأ في فتح بحث المنتجات: {ex.Message}"); }
        }

        private void ShowInvoiceSearchDialog()
        {
            try
            {
                var list = new SalesInvoicesListWindow(_salesInvoiceService, _customerService, _serviceProvider) { Owner = this };
                list.ShowDialog();
            }
            catch (Exception ex) { ShowError($"خطأ في فتح قائمة الفواتير: {ex.Message}"); }
        }

        // ========= المستودع/المندوب =========
        private void cmbWarehouse_SelectionChanged(object sender, SelectionChangedEventArgs e) { /* تحديثات حسب المستودع */ }
        private void cmbWarehouse_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1) { ShowWarehouseSearchDialog(); e.Handled = true; }
            else if (e.Key == Key.Enter) { cmbRepresentative.Focus(); e.Handled = true; }
        }
        private void btnSelectWarehouse_Click(object sender, RoutedEventArgs e) => ShowWarehouseSearchDialog();
        private void btnSearchRepresentative_Click(object sender, RoutedEventArgs e) => ShowRepresentativeSearchDialog();

        private void ShowWarehouseSearchDialog()
        {
            try
            {
                var dialog = new WarehouseSearchDialog(_warehouses.ToList()) { Owner = this };
                if (dialog.ShowDialog() == true && dialog.SelectedWarehouse != null)
                    cmbWarehouse.SelectedItem = dialog.SelectedWarehouse;
            }
            catch (Exception ex) { ShowError($"خطأ في فتح بحث المستودعات: {ex.Message}"); }
        }

        private void cmbRepresentative_SelectionChanged(object sender, SelectionChangedEventArgs e) { /* عمولة/تسعير */ }
        private void cmbRepresentative_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1) { ShowRepresentativeSearchDialog(); e.Handled = true; }
            else if (e.Key == Key.Enter) { txtInvoiceNotes.Focus(); e.Handled = true; }
        }
        private void ShowRepresentativeSearchDialog()
        {
            try
            {
                var dialog = new RepresentativeSearchDialog(_representatives.ToList()) { Owner = this };
                if (dialog.ShowDialog() == true && dialog.SelectedRepresentative != null)
                    cmbRepresentative.SelectedItem = dialog.SelectedRepresentative;
            }
            catch (Exception ex) { ShowError($"خطأ في فتح بحث المندوبين: {ex.Message}"); }
        }

        // ========= تفاصيل صف =========
        private void ctxToggleRowDetails_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (dgItems.SelectedItem != null)
                {
                    if (dgItems.ItemContainerGenerator.ContainerFromItem(dgItems.SelectedItem) is DataGridRow row)
                        row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch (Exception ex) { ShowError($"خطأ في تبديل تفاصيل السطر: {ex.Message}"); }
        }

        #region Missing Event Handlers

        private void cmbCustomer_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Handle customer combobox key navigation
        }

        private void txtInvoiceNotes_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Handle invoice notes key navigation
        }

        private void cmbProduct_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle product combobox key navigation
        }

        private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }

        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsKeyboardFocusWithin)
            {
                textBox.Focus();
                e.Handled = true;
            }
        }

        #endregion
    }
}
