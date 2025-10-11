using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingSystem.Business;
using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.WPF.Models;
using AccountingSystem.WPF.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// نافذة إدخال صنف احترافية مع جميع الميزات الذكية المطلوبة
    /// WholeQty/PartQty، WholePrice/PartPrice، Factor تحويل، آخر 3 أسعار، تحقق مخزون ذكي
    /// </summary>
    public partial class ModernItemEntryDialog : Window, INotifyPropertyChanged
    {
        #region Private Fields & Services

        private readonly IServiceProvider _serviceProvider;
        private readonly IProductService _productService;
        private readonly IPriceHistoryService _priceHistoryService;
        private readonly ICustomerService _customerService;
        private readonly AccountingDbContext _context;

        private readonly ObservableCollection<Product> _products;
        private readonly ObservableCollection<Unit> _productUnits;
        private readonly ObservableCollection<ProductUnit> _currentProductUnits;
        private readonly List<PriceHistoryItem> _lastPrices;

        private readonly CultureInfo _culture;
        private bool _isCalculating;
        private bool _isLoadingPrices;
        private decimal _conversionFactor = 1m;
        private Customer? _currentCustomer;
        private Product? _selectedProduct;
        private Unit? _selectedUnit;

        #endregion

        #region Properties

        public SalesInvoiceItem? ResultItem { get; private set; }
        public bool SaveAndContinue { get; private set; }

        private decimal _wholeQty = 1m;
        public decimal WholeQty
        {
            get => _wholeQty;
            set
            {
                if (SetProperty(ref _wholeQty, value))
                {
                    if (!_isCalculating) CalculatePartQuantity();
                    CalculateLineTotal();
                    ValidateStock();
                }
            }
        }

        private decimal _partQty;
        public decimal PartQty
        {
            get => _partQty;
            set
            {
                if (SetProperty(ref _partQty, value))
                {
                    if (!_isCalculating) CalculateWholeQuantity();
                    CalculateLineTotal();
                    ValidateStock();
                }
            }
        }

        private decimal _wholePrice;
        public decimal WholePrice
        {
            get => _wholePrice;
            set
            {
                if (SetProperty(ref _wholePrice, value))
                {
                    if (!_isCalculating) CalculatePartPrice();
                    CalculateLineTotal();
                }
            }
        }

        private decimal _partPrice;
        public decimal PartPrice
        {
            get => _partPrice;
            set
            {
                if (SetProperty(ref _partPrice, value))
                {
                    if (!_isCalculating) CalculateWholePrice();
                    CalculateLineTotal();
                }
            }
        }

        private decimal _discountAmount;
        public decimal DiscountAmount
        {
            get => _discountAmount;
            set
            {
                if (SetProperty(ref _discountAmount, value))
                {
                    CalculateLineTotal();
                    ValidateDiscount();
                }
            }
        }

        private bool _discountIsPercentage;
        public bool DiscountIsPercentage
        {
            get => _discountIsPercentage;
            set
            {
                if (SetProperty(ref _discountIsPercentage, value))
                {
                    CalculateLineTotal();
                    ValidateDiscount();
                }
            }
        }

        private decimal _lineTotal;
        public decimal LineTotal
        {
            get => _lineTotal;
            set => SetProperty(ref _lineTotal, value);
        }

        private string _statusMessage = "جاهز للإدخال";
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value ?? string.Empty);
        }

        private string _priceSource = "المنتج";
        public string PriceSource
        {
            get => _priceSource;
            set => SetProperty(ref _priceSource, value ?? string.Empty);
        }

        #endregion

        #region Constructor

        public ModernItemEntryDialog(IServiceProvider serviceProvider, Customer? customer = null)
        {
            InitializeComponent();
            DataContext = this;

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _productService = serviceProvider.GetRequiredService<IProductService>();
            _priceHistoryService = serviceProvider.GetRequiredService<IPriceHistoryService>();
            _customerService = serviceProvider.GetRequiredService<ICustomerService>();
            _context = serviceProvider.GetRequiredService<AccountingDbContext>();

            _products = new ObservableCollection<Product>();
            _productUnits = new ObservableCollection<Unit>();
            _currentProductUnits = new ObservableCollection<ProductUnit>();
            _lastPrices = new List<PriceHistoryItem>();

            _culture = new CultureInfo("ar-EG");
            _culture.NumberFormat.CurrencySymbol = "ج.م";
            _currentCustomer = customer;

            cmbProduct.ItemsSource = _products;
            cmbUnit.ItemsSource = _productUnits;

            // Setup events for SelectAll behavior
            SetupSelectAllBehavior();
        }

        #endregion

        #region Window Events

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            cmbProduct.Focus();
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Smart Enter Navigation
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                HandleSmartEnter();
                e.Handled = true;
            }
            // F1 Search
            else if (e.Key == Key.F1)
            {
                ShowProductSearch();
                e.Handled = true;
            }
            // Escape Cancel
            else if (e.Key == Key.Escape)
            {
                DialogResult = false;
                e.Handled = true;
            }
        }

        #endregion

        #region Data Loading

        private async Task LoadDataAsync()
        {
            try
            {
                SetBusy(true, "جارٍ تحميل البيانات...");

                var products = await _productService.GetAllProductsAsync();
                _products.Clear();
                foreach (var product in products)
                    _products.Add(product);

                StatusMessage = "تم تحميل البيانات بنجاح";
                ComprehensiveLogger.LogUIOperation("تم تحميل بيانات نافذة إدخال الأصناف", "ModernItemEntryDialog");
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في تحميل البيانات: {ex.Message}";
                ComprehensiveLogger.LogError("فشل تحميل بيانات نافذة إدخال الأصناف", ex, "ModernItemEntryDialog");
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task LoadProductUnitsAsync(Product product)
        {
            try
            {
                _productUnits.Clear();
                _currentProductUnits.Clear();

                // Add main unit
                if (product.MainUnit != null)
                    _productUnits.Add(product.MainUnit);

                // Load additional units
                var productUnits = await _context.ProductUnits
                    .AsNoTracking()
                    .Include(pu => pu.Unit)
                    .Where(pu => pu.ProductId == product.ProductId && pu.IsActive)
                    .ToListAsync();

                foreach (var pu in productUnits)
                {
                    _currentProductUnits.Add(pu);
                    if (pu.Unit != null && !_productUnits.Any(u => u.UnitId == pu.UnitId))
                        _productUnits.Add(pu.Unit);
                }

                // Set default selection
                if (_productUnits.Any())
                {
                    cmbUnit.SelectedItem = _productUnits.First();
                    await UpdateConversionFactor();
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في تحميل وحدات المنتج: {ex.Message}";
                ComprehensiveLogger.LogError($"فشل تحميل وحدات المنتج {product?.ProductId}", ex, "ModernItemEntryDialog");
            }
        }

        private async Task LoadLastPricesAsync(Product product)
        {
            if (_isLoadingPrices) return;

            try
            {
                _isLoadingPrices = true;
                brdLastPrices.Visibility = Visibility.Collapsed;
                _lastPrices.Clear();

                // Get last 3 prices for this customer and product
                var prices = await _context.SalesInvoiceItems
                    .AsNoTracking()
                    .Include(item => item.SalesInvoice)
                    .Where(item => item.ProductId == product.ProductId)
                    .Where(item => _currentCustomer == null || item.SalesInvoice.CustomerId == _currentCustomer.CustomerId)
                    .OrderByDescending(item => item.SalesInvoice.InvoiceDate)
                    .Take(3)
                    .Select(item => new PriceHistoryItem
                    {
                        Price = item.UnitPrice,
                        Date = item.SalesInvoice.InvoiceDate.ToString("yyyy-MM-dd", _culture)
                    })
                    .ToListAsync();

                if (prices.Any())
                {
                    _lastPrices.AddRange(prices);
                    icLastPrices.ItemsSource = _lastPrices;
                    brdLastPrices.Visibility = Visibility.Visible;

                    // Use the most recent price
                    var latestPrice = prices.First();
                    WholePrice = latestPrice.Price;
                    PriceSource = "آخر سعر للعميل";
                    brdAutoPriceIndicator.Visibility = Visibility.Visible;
                }
                else
                {
                    // Use product's default price
                    WholePrice = product.SalePrice;
                    PriceSource = "سعر المنتج";
                    brdAutoPriceIndicator.Visibility = Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في تحميل تاريخ الأسعار: {ex.Message}";
                ComprehensiveLogger.LogError($"فشل تحميل تاريخ أسعار المنتج {product?.ProductId}", ex, "ModernItemEntryDialog");
            }
            finally
            {
                _isLoadingPrices = false;
            }
        }

        #endregion

        #region Product Selection

        private async void cmbProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProduct.SelectedItem is Product product)
            {
                await OnProductSelectedAsync(product);
            }
        }

        private async Task OnProductSelectedAsync(Product product)
        {
            try
            {
                _selectedProduct = product;

                // Update UI with product info
                lblProductName.Text = product.ProductName ?? "غير محدد";
                lblProductCode.Text = product.ProductCode ?? "غير محدد";
                lblCurrentStock.Text = product.CurrentStock.ToString("F2", _culture);
                lblStockUnit.Text = product.MainUnit?.UnitName ?? "وحدة";

                // Load units and prices
                await LoadProductUnitsAsync(product);
                await LoadLastPricesAsync(product);

                StatusMessage = $"تم اختيار المنتج: {product.ProductName}";
                ValidateStock();
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في معالجة المنتج: {ex.Message}";
                ComprehensiveLogger.LogError($"فشل معالجة اختيار المنتج {product?.ProductId}", ex, "ModernItemEntryDialog");
            }
        }

        private void cmbProduct_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Enable filtering as user types
            if (sender is ComboBox combo && !combo.IsDropDownOpen)
                combo.IsDropDownOpen = true;
        }

        private void cmbProduct_DropDownOpened(object sender, EventArgs e)
        {
            // Clear any filters when dropdown opens
        }

        private void cmbProduct_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                ShowProductSearch();
                e.Handled = true;
            }
        }

        #endregion

        #region Unit Selection & Conversion

        private async void cmbUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbUnit.SelectedItem is Unit unit)
            {
                _selectedUnit = unit;
                await UpdateConversionFactor();
                CalculateQuantitiesForNewUnit();
                ValidateStock();
            }
        }

        private async Task UpdateConversionFactor()
        {
            try
            {
                if (_selectedProduct == null || _selectedUnit == null)
                {
                    _conversionFactor = 1m;
                    lblConversionFactor.Text = "1.00";
                    return;
                }

                if (_selectedUnit.UnitId == _selectedProduct.MainUnitId)
                {
                    _conversionFactor = 1m;
                }
                else
                {
                    var productUnit = _currentProductUnits.FirstOrDefault(pu => pu.UnitId == _selectedUnit.UnitId);
                    _conversionFactor = productUnit?.ConversionFactor ?? 1m;
                }

                lblConversionFactor.Text = _conversionFactor.ToString("F2", _culture);

                // Update available stock for selected unit
                if (_selectedProduct != null)
                {
                    var availableInSelectedUnit = _selectedProduct.CurrentStock / _conversionFactor;
                    lblAvailableStock.Text = availableInSelectedUnit.ToString("F2", _culture);
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في حساب معامل التحويل: {ex.Message}";
                ComprehensiveLogger.LogError("فشل حساب معامل التحويل", ex, "ModernItemEntryDialog");
            }
        }

        private void CalculateQuantitiesForNewUnit()
        {
            if (_isCalculating) return;

            try
            {
                _isCalculating = true;

                // Keep the total quantity in base unit constant
                var totalInBaseUnit = (WholeQty + PartQty) * _conversionFactor;

                // Recalculate for new unit
                var totalInNewUnit = totalInBaseUnit / _conversionFactor;
                WholeQty = Math.Floor(totalInNewUnit);
                PartQty = totalInNewUnit - WholeQty;
            }
            finally
            {
                _isCalculating = false;
            }
        }

        #endregion

        #region Quantity Calculations

        private void txtWholeQty_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtWholeQty.Text, out var value))
                WholeQty = value;
        }

        private void txtPartQty_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtPartQty.Text, out var value))
                PartQty = value;
        }

        private void CalculatePartQuantity()
        {
            if (_isCalculating) return;

            try
            {
                _isCalculating = true;
                txtPartQty.Text = "0";
                OnPropertyChanged(nameof(PartQty));
            }
            finally
            {
                _isCalculating = false;
            }
        }

        private void CalculateWholeQuantity()
        {
            if (_isCalculating) return;

            try
            {
                _isCalculating = true;
                // For now, keep it simple - part quantity doesn't affect whole quantity
                // This can be enhanced based on specific business rules
            }
            finally
            {
                _isCalculating = false;
            }
        }

        #endregion

        #region Price Calculations

        private void txtWholePrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtWholePrice.Text, out var value))
                WholePrice = value;
        }

        private void txtPartPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtPartPrice.Text, out var value))
                PartPrice = value;
        }

        private void CalculatePartPrice()
        {
            if (_isCalculating) return;

            try
            {
                _isCalculating = true;
                PartPrice = WholePrice * _conversionFactor;
                txtPartPrice.Text = PartPrice.ToString("F2", _culture);
            }
            finally
            {
                _isCalculating = false;
            }
        }

        private void CalculateWholePrice()
        {
            if (_isCalculating) return;

            try
            {
                _isCalculating = true;
                WholePrice = _conversionFactor > 0 ? PartPrice / _conversionFactor : PartPrice;
                txtWholePrice.Text = WholePrice.ToString("F2", _culture);
            }
            finally
            {
                _isCalculating = false;
            }
        }

        #endregion

        #region Discount & Line Total

        private void txtDiscount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (decimal.TryParse(txtDiscount.Text, out var value))
                DiscountAmount = value;
        }

        private void cmbDiscountType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DiscountIsPercentage = cmbDiscountType.SelectedIndex == 1;
        }

        private void CalculateLineTotal()
        {
            try
            {
                var totalQuantity = WholeQty + PartQty;
                var grossAmount = totalQuantity * WholePrice;

                var discountValue = DiscountIsPercentage
                    ? grossAmount * (DiscountAmount / 100m)
                    : DiscountAmount;

                LineTotal = Math.Max(0, grossAmount - discountValue);
                lblLineTotal.Text = LineTotal.ToString("C", _culture);
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في حساب الإجمالي: {ex.Message}";
                ComprehensiveLogger.LogError("فشل حساب إجمالي السطر", ex, "ModernItemEntryDialog");
            }
        }

        #endregion

        #region Validation

        private void ValidateStock()
        {
            try
            {
                if (_selectedProduct == null || _selectedUnit == null)
                {
                    brdStockWarning.Visibility = Visibility.Collapsed;
                    brdValidationStatus.Background = (System.Windows.Media.Brush)FindResource("InfoBrush");
                    lblValidationIcon.Text = "?";
                    return;
                }

                var totalQuantity = WholeQty + PartQty;
                var requiredInBaseUnit = totalQuantity * _conversionFactor;
                var availableInBaseUnit = _selectedProduct.CurrentStock;

                if (requiredInBaseUnit > availableInBaseUnit)
                {
                    var shortage = requiredInBaseUnit - availableInBaseUnit;
                    var shortageInSelectedUnit = shortage / _conversionFactor;

                    lblStockWarning.Text = $"نقص في المخزون: {shortageInSelectedUnit:F2} {_selectedUnit.UnitName}";
                    brdStockWarning.Visibility = Visibility.Visible;
                    brdValidationStatus.Background = (System.Windows.Media.Brush)FindResource("DangerBrush");
                    lblValidationIcon.Text = "⚠";
                    StatusMessage = "تحذير: الكمية المطلوبة تتجاوز المخزون المتاح";
                }
                else
                {
                    brdStockWarning.Visibility = Visibility.Collapsed;
                    brdValidationStatus.Background = (System.Windows.Media.Brush)FindResource("SuccessBrush");
                    lblValidationIcon.Text = "✓";
                    StatusMessage = "الكمية متاحة في المخزون";
                }
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل التحقق من المخزون", ex, "ModernItemEntryDialog");
            }
        }

        private void ValidateDiscount()
        {
            try
            {
                var totalQuantity = WholeQty + PartQty;
                var grossAmount = totalQuantity * WholePrice;

                if (DiscountIsPercentage && DiscountAmount > 100)
                {
                    StatusMessage = "نسبة الخصم لا يمكن أن تتجاوز 100%";
                    return;
                }

                var discountValue = DiscountIsPercentage
                    ? grossAmount * (DiscountAmount / 100m)
                    : DiscountAmount;

                if (discountValue > grossAmount)
                {
                    StatusMessage = "قيمة الخصم تتجاوز إجمالي السطر";
                    return;
                }

                StatusMessage = "الخصم صالح";
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل التحقق من الخصم", ex, "ModernItemEntryDialog");
            }
        }

        #endregion

        #region Smart Navigation & Actions

        private void HandleSmartEnter()
        {
            var focusedElement = Keyboard.FocusedElement as FrameworkElement;

            if (focusedElement == null) return;

            // Special handling for discount field - goes to save
            if (ReferenceEquals(focusedElement, txtDiscount))
            {
                btnSaveAndAdd.Focus();
                return;
            }

            // Normal tab navigation
            var request = new TraversalRequest(FocusNavigationDirection.Next);
            focusedElement.MoveFocus(request);

            // Select all text in text boxes
            if (Keyboard.FocusedElement is TextBox nextTextBox)
                nextTextBox.SelectAll();
        }

        private void ShowProductSearch()
        {
            try
            {
                var productItems = _products.Select(p => ProductSearchItem.FromRaw(p)).ToList();

                var dialog = new ProductSearchDialog(productItems) { Owner = this };
                if (dialog.ShowDialog() == true && dialog.Selected != null)
                {
                    var selectedProduct = _products.FirstOrDefault(p => p.ProductId == dialog.Selected.ProductId);
                    if (selectedProduct != null)
                    {
                        cmbProduct.SelectedItem = selectedProduct;
                        cmbUnit.Focus();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بحث المنتجات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region Button Events

        private async void btnSaveAndAdd_Click(object sender, RoutedEventArgs e)
        {
            if (await SaveCurrentItem())
            {
                SaveAndContinue = true;
                DialogResult = true;
            }
        }

        private async void btnSaveAndClose_Click(object sender, RoutedEventArgs e)
        {
            if (await SaveCurrentItem())
            {
                SaveAndContinue = false;
                DialogResult = true;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to previous item in sequence
            // This would be implemented based on the parent window's item list
            StatusMessage = "التنقل للصنف السابق";
        }

        private void btnNext_Click(object sender, RoutedEventArgs e)
        {
            // Navigate to next item in sequence
            // This would be implemented based on the parent window's item list
            StatusMessage = "التنقل للصنف التالي";
        }

        private void btnSearchProduct_Click(object sender, RoutedEventArgs e)
        {
            ShowProductSearch();
        }

        #endregion

        #region Save Logic

        private async Task<bool> SaveCurrentItem()
        {
            try
            {
                if (!await ValidateForSave())
                    return false;

                SetBusy(true, "جارٍ حفظ الصنف...");

                var totalQuantity = WholeQty + PartQty;
                var discountValue = DiscountIsPercentage
                    ? totalQuantity * WholePrice * (DiscountAmount / 100m)
                    : DiscountAmount;

                ResultItem = new SalesInvoiceItem
                {
                    ProductId = _selectedProduct!.ProductId,
                    Product = _selectedProduct,
                    UnitId = _selectedUnit!.UnitId,
                    Unit = _selectedUnit,
                    Quantity = totalQuantity,
                    UnitPrice = WholePrice,
                    DiscountAmount = discountValue,
                    TotalPrice = totalQuantity * WholePrice,
                    NetAmount = LineTotal
                };

                ComprehensiveLogger.LogBusinessOperation(
                    "تم إنشاء صنف فاتورة بيع",
                    $"المنتج: {_selectedProduct.ProductName}, الكمية: {totalQuantity}, السعر: {WholePrice:C}");

                StatusMessage = "تم حفظ الصنف بنجاح";
                return true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في حفظ الصنف: {ex.Message}";
                MessageBox.Show($"خطأ في حفظ الصنف: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ComprehensiveLogger.LogError("فشل حفظ صنف الفاتورة", ex, "ModernItemEntryDialog");
                return false;
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task<bool> ValidateForSave()
        {
            if (_selectedProduct == null)
            {
                MessageBox.Show("يرجى اختيار المنتج", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbProduct.Focus();
                return false;
            }

            if (_selectedUnit == null)
            {
                MessageBox.Show("يرجى اختيار الوحدة", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                cmbUnit.Focus();
                return false;
            }

            var totalQuantity = WholeQty + PartQty;
            if (totalQuantity <= 0)
            {
                MessageBox.Show("يرجى إدخال كمية صحيحة", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtWholeQty.Focus();
                txtWholeQty.SelectAll();
                return false;
            }

            if (WholePrice < 0)
            {
                MessageBox.Show("السعر لا يمكن أن يكون سالباً", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtWholePrice.Focus();
                txtWholePrice.SelectAll();
                return false;
            }

            // Check stock availability
            var requiredInBaseUnit = totalQuantity * _conversionFactor;
            if (requiredInBaseUnit > _selectedProduct.CurrentStock)
            {
                var result = MessageBox.Show(
                    "الكمية المطلوبة تتجاوز المخزون المتاح. هل تريد المتابعة؟",
                    "تأكيد المخزون",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                    return false;
            }

            return true;
        }

        #endregion

        #region Helper Methods

        private void SetupSelectAllBehavior()
        {
            // Setup SelectAll behavior for all text boxes
            EventManager.RegisterClassHandler(typeof(TextBox),
                UIElement.GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(TextBox_GotKeyboardFocus));
            EventManager.RegisterClassHandler(typeof(TextBox),
                UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(TextBox_PreviewMouseLeftButtonDown), true);
        }

        private void TextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (sender is TextBox textBox)
                textBox.SelectAll();
        }

        private void TextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is TextBox textBox && !textBox.IsKeyboardFocusWithin)
            {
                e.Handled = true;
                textBox.Focus();
                textBox.SelectAll();
            }
        }

        private void SetBusy(bool isBusy, string? message = null)
        {
            BusyOverlay.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            if (message != null)
                lblLoadingMessage.Text = message;
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
                return false;

            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    #region Helper Classes

    public class PriceHistoryItem
    {
        public decimal Price { get; set; }
        public string Date { get; set; } = string.Empty;
    }

    #endregion
}