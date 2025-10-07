using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AccountingSystem.Business;
using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.WPF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// منبثق إدخال/تعديل صنف الفاتورة وفق المواصفات المفصلة
    /// يدعم: اختيار المنتج والوحدة، الكميات الكلية/الجزئية، الأسعار مع التحويل التلقائي، 
    /// استرجاع آخر سعر للعميل، التحقق من الرصيد، التنقل بين السطور
    /// </summary>
    public partial class ItemEntryDialog : Window
    {
        #region Fields

        // serviceProvider محفوظ للاستخدام المستقبلي في إضافة ميزات أخرى
        private readonly IPriceHistoryService _priceHistoryService;
        private readonly IProductService _productService;
        private readonly AccountingDbContext _context;
        private readonly CultureInfo _culture;

        private readonly ObservableCollection<Product> _products;
        private readonly ObservableCollection<ProductUnit> _productUnits;
        
        private readonly int _customerId;
        private decimal _currentUnitFactor = 1m;
        private Product? _selectedProduct;
        private ProductUnit? _selectedProductUnit;

        // للتعديل: بيانات السطر الحالي
        private readonly SalesInvoiceItem? _currentItem;
        private readonly bool _isEditMode;
        private readonly List<SalesInvoiceItem>? _allItems; // كل سطور الفاتورة للتنقل
        private int _currentItemIndex = -1;

        // تحكم في عمليات async
        private CancellationTokenSource? _cts;

        // تلميحات للمستخدم
        private bool _userOpenedProduct = false;
        private bool _isUpdatingPrices = false;

        #endregion

        #region Constructor

        public ItemEntryDialog(IServiceProvider serviceProvider, int customerId)
        {
            InitializeComponent();

            var sp = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _priceHistoryService = sp.GetRequiredService<IPriceHistoryService>();
            _productService = sp.GetRequiredService<IProductService>();
            _context = sp.GetRequiredService<AccountingDbContext>();

            _customerId = customerId;

            _products = new ObservableCollection<Product>();
            _productUnits = new ObservableCollection<ProductUnit>();

            _culture = new CultureInfo("ar-EG");
            _culture.NumberFormat.CurrencySymbol = "ج.م";

            // ربط البيانات
            cmbProduct.ItemsSource = _products;
            cmbUnit.ItemsSource = _productUnits;

            // SelectAll لكل TextBox
            EventManager.RegisterClassHandler(typeof(TextBox),
                UIElement.GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(TextBox_GotKeyboardFocus));
            EventManager.RegisterClassHandler(typeof(TextBox),
                UIElement.PreviewMouseLeftButtonDownEvent,
                new MouseButtonEventHandler(TextBox_PreviewMouseLeftButtonDown), true);

            Loaded += async (_, __) => await LoadDataAsync();
        }

        /// <summary>
        /// إنشاء المنبثق لتعديل سطر موجود
        /// </summary>
        public ItemEntryDialog(IServiceProvider serviceProvider, int customerId, SalesInvoiceItem item, List<SalesInvoiceItem> allItems)
            : this(serviceProvider, customerId)
        {
            _currentItem = item ?? throw new ArgumentNullException(nameof(item));
            _allItems = allItems ?? throw new ArgumentNullException(nameof(allItems));
            _isEditMode = true;
            _currentItemIndex = _allItems.IndexOf(item);

            lblTitle.Text = "تعديل صنف";
            btnPrevious.Visibility = _currentItemIndex > 0 ? Visibility.Visible : Visibility.Hidden;
            btnNext.Visibility = _currentItemIndex < _allItems.Count - 1 ? Visibility.Visible : Visibility.Hidden;
        }

        /// <summary>
        /// إنشاء منبثق بسيط بمعامل واحد (للتوافق مع الكود القديم)
        /// </summary>
        public ItemEntryDialog(int customerId) : this(App.ServiceProvider!, customerId)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        /// السطر الناتج من الإدخال/التعديل
        /// </summary>
        public SalesInvoiceItem? ResultItem { get; private set; }

        /// <summary>
        /// هل المستخدم اختار "إضافة آخر"
        /// </summary>
        public bool AddAnotherRequested { get; private set; }

        /// <summary>
        /// خاصية للتوافق مع الكود القديم
        /// </summary>
        public bool SaveAndContinue => AddAnotherRequested;

        #endregion

        #region Data Loading

        private async Task LoadDataAsync()
        {
            try
            {
                SetBusy(true);
                _cts?.Cancel();
                _cts = new CancellationTokenSource();

                // تحميل المنتجات
                var products = await _productService.GetAllProductsAsync();
                _products.Clear();
                foreach (var product in products)
                    _products.Add(product);

                // إذا كان في وضع التعديل، حمّل بيانات السطر
                if (_isEditMode && _currentItem != null)
                    await LoadItemForEditAsync(_currentItem);
                else
                {
                    // وضع إضافة جديدة: ركز على المنتج
                    txtWholeQuantity.Text = "";
                    cmbProduct.Focus();
                }

                UpdateStatusBar("جاهز للإدخال");
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل البيانات: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task LoadItemForEditAsync(SalesInvoiceItem item)
        {
            try
            {
                // اختيار المنتج
                var product = _products.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (product != null)
                {
                    cmbProduct.SelectedItem = product;
                    await LoadProductUnitsAsync(product.ProductId);

                    // اختيار الوحدة
                    var unit = _productUnits.FirstOrDefault(u => u.UnitId == item.UnitId);
                    if (unit != null)
                    {
                        cmbUnit.SelectedItem = unit;
                        _currentUnitFactor = unit.ConversionFactor;
                    }

                    // تحميل الكميات والأسعار
                    txtWholeQuantity.Text = item.Quantity.ToString("F2");
                    txtWholePrice.Text = item.UnitPrice.ToString("F2");
                    txtDiscount.Text = item.DiscountAmount.ToString("F2");

                    // تحديث الحسابات
                    UpdateQuantitySync();
                    UpdatePriceSync();
                    CalculateLineTotal();
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل بيانات السطر: {ex.Message}");
            }
        }

        #endregion

        #region Product Selection

        private async void cmbProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbProduct.SelectedItem is Product product)
            {
                _selectedProduct = product;
                await LoadProductUnitsAsync(product.ProductId);
                await UpdateProductInfoAsync();
            }
        }

        private void cmbProduct_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F1 || e.Key == Key.F2)
            {
                ShowProductSearchDialog();
                e.Handled = true;
            }
        }

        private void cmbProduct_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (_userOpenedProduct)
                cmbProduct.IsDropDownOpen = true;
        }

        private void cmbProduct_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            _userOpenedProduct = true;
        }

        private void cmbProduct_DropDownOpened(object sender, EventArgs e)
        {
            _userOpenedProduct = true;
        }

        private async Task LoadProductUnitsAsync(int productId)
        {
            try
            {
                _productUnits.Clear();

                // تحميل الوحدات من ProductUnits أو نظام الوحدات
                var units = await _context.ProductUnits
                    .Where(pu => pu.ProductId == productId)
                    .Include(pu => pu.Unit)
                    .AsNoTracking()
                    .ToListAsync();

                foreach (var unit in units)
                    _productUnits.Add(unit);

                // اختيار الوحدة الأولى افتراضياً (لا يوجد IsMainUnit في ProductUnit)
                if (_productUnits.Count > 0)
                {
                    cmbUnit.SelectedItem = _productUnits[0];
                }

                UpdateStatusBar($"تم تحميل {_productUnits.Count} وحدة للمنتج");
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحميل وحدات المنتج: {ex.Message}");
            }
        }

        private async Task UpdateProductInfoAsync()
        {
            try
            {
                if (_selectedProduct == null) return;

                // تحديث الرصيد المتاح
                lblAvailableStock.Text = _selectedProduct.CurrentStock.ToString("F2");
                lblStockStatus.Foreground = _selectedProduct.CurrentStock > 0 
                    ? System.Windows.Media.Brushes.Green 
                    : System.Windows.Media.Brushes.Red;
                lblStockStatus.ToolTip = $"متاح: {_selectedProduct.CurrentStock:F2}";

                // تحديث معلومات إضافية
                await UpdatePriceInfoAsync();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في تحديث معلومات المنتج: {ex.Message}");
            }
        }

        private async Task UpdatePriceInfoAsync()
        {
            try
            {
                if (_selectedProduct == null || _selectedProductUnit == null || _isUpdatingPrices) return;

                _isUpdatingPrices = true;

                // جلب آخر سعر للعميل
                var lastPrice = await _priceHistoryService.GetLastCustomerPriceAsync(
                    _customerId, _selectedProduct.ProductId, _selectedProductUnit.UnitId);

                if (lastPrice.HasValue)
                {
                    txtWholePrice.Text = lastPrice.Value.ToString("F2");
                    lblWholePriceInfo.Text = "آخر سعر للعميل";
                    lblWholePriceInfo.Foreground = System.Windows.Media.Brushes.Green;
                }
                else
                {
                    txtWholePrice.Text = _selectedProduct.SalePrice.ToString("F2");
                    lblWholePriceInfo.Text = "السعر العام";
                    lblWholePriceInfo.Foreground = System.Windows.Media.Brushes.Blue;
                }

                UpdatePriceSync();
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في استرجاع السعر: {ex.Message}");
            }
            finally
            {
                _isUpdatingPrices = false;
            }
        }

        #endregion

        #region Unit Selection

        private async void cmbUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbUnit.SelectedItem is ProductUnit unit)
            {
                _selectedProductUnit = unit;
                _currentUnitFactor = unit.ConversionFactor;

                // تحديث تسميات الوحدات
                lblWholeUnit.Text = unit.Unit?.UnitName ?? "وحدة";
                lblPartUnit.Text = $"جزء من {unit.Unit?.UnitName}";
                lblFactor.Text = $"المعامل: {_currentUnitFactor}";

                // تحديث السعر حسب الوحدة الجديدة
                await UpdatePriceInfoAsync();

                UpdateQuantitySync();
                UpdatePriceSync();
            }
        }

        private void cmbUnit_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (_userOpenedProduct)
                cmbUnit.IsDropDownOpen = true;
        }

        private void cmbUnit_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                if (cmbUnit.IsDropDownOpen && cmbUnit.SelectedIndex < 0 && cmbUnit.Items.Count > 0)
                    cmbUnit.SelectedIndex = 0;
                cmbUnit.IsDropDownOpen = false;
                txtWholeQuantity.Focus();
                txtWholeQuantity.SelectAll();
            }
        }

        #endregion

        #region Quantity Calculations

        private void txtWholeQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingPrices) return;
            UpdateQuantitySync();
            CalculateLineTotal();
            ValidateStock();
        }

        private void txtPartQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingPrices) return;

            // تحويل من الجزء إلى الكلي
            if (TryParseDecimal(txtPartQuantity.Text, out var partQty) && _currentUnitFactor != 0)
            {
                var wholeQty = partQty / _currentUnitFactor;
                _isUpdatingPrices = true;
                txtWholeQuantity.Text = wholeQty.ToString("F2");
                _isUpdatingPrices = false;
            }

            CalculateLineTotal();
            ValidateStock();
        }

        private void UpdateQuantitySync()
        {
            if (_isUpdatingPrices) return;

            if (TryParseDecimal(txtWholeQuantity.Text, out var wholeQty))
            {
                var partQty = wholeQty * _currentUnitFactor;
                _isUpdatingPrices = true;
                txtPartQuantity.Text = partQty.ToString("F2");
                _isUpdatingPrices = false;
            }
        }

        #endregion

        #region Price Calculations

        private void txtWholePrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingPrices) return;
            UpdatePriceSync();
            CalculateLineTotal();
        }

        private void txtPartPrice_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingPrices) return;

            // تحويل من سعر الجزء إلى سعر الكلي
            if (TryParseDecimal(txtPartPrice.Text, out var partPrice) && _currentUnitFactor != 0)
            {
                var wholePrice = partPrice * _currentUnitFactor;
                _isUpdatingPrices = true;
                txtWholePrice.Text = wholePrice.ToString("F2");
                _isUpdatingPrices = false;
            }

            CalculateLineTotal();
        }

        private void UpdatePriceSync()
        {
            if (_isUpdatingPrices) return;

            if (TryParseDecimal(txtWholePrice.Text, out var wholePrice) && _currentUnitFactor != 0)
            {
                var partPrice = wholePrice / _currentUnitFactor;
                _isUpdatingPrices = true;
                txtPartPrice.Text = partPrice.ToString("F2");
                _isUpdatingPrices = false;
            }
        }

        #endregion

        #region Discount and Line Total

        private void txtDiscount_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateLineTotal();
        }

        private void CalculateLineTotal()
        {
            try
            {
                if (!TryParseDecimal(txtWholeQuantity.Text, out var quantity) ||
                    !TryParseDecimal(txtWholePrice.Text, out var price))
                {
                    lblLineTotal.Text = "0.00 ج.م";
                    lblDiscountAmount.Text = "0.00 ج.م";
                    return;
                }

                var subtotal = quantity * price;
                var discountAmount = 0m;

                if (TryParseDecimal(txtDiscount.Text, out var discount))
                {
                    discountAmount = cmbDiscountType.SelectedIndex == 0
                        ? subtotal * (discount / 100m)  // نسبة مئوية
                        : discount;                     // مبلغ ثابت
                }

                var lineTotal = Math.Max(0, subtotal - discountAmount);

                lblLineTotal.Text = lineTotal.ToString("C", _culture);
                lblDiscountAmount.Text = discountAmount.ToString("C", _culture);
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في حساب الإجمالي: {ex.Message}");
            }
        }

        #endregion

        #region Validation

        private bool ValidateInput()
        {
            lblValidationMessage.Text = "";

            // تحقق من اختيار المنتج
            if (_selectedProduct == null)
            {
                lblValidationMessage.Text = "يجب اختيار منتج";
                cmbProduct.Focus();
                return false;
            }

            // تحقق من اختيار الوحدة
            if (_selectedProductUnit == null)
            {
                lblValidationMessage.Text = "يجب اختيار وحدة";
                cmbUnit.Focus();
                return false;
            }

            // تحقق من الكمية
            if (!TryParseDecimal(txtWholeQuantity.Text, out var quantity) || quantity <= 0)
            {
                lblValidationMessage.Text = "يجب إدخال كمية صحيحة أكبر من صفر";
                txtWholeQuantity.Focus();
                return false;
            }

            // تحقق من السعر
            if (!TryParseDecimal(txtWholePrice.Text, out var price) || price <= 0)
            {
                lblValidationMessage.Text = "يجب إدخال سعر صحيح أكبر من صفر";
                txtWholePrice.Focus();
                return false;
            }

            // تحقق من الرصيد
            var requiredStock = quantity * _currentUnitFactor; // تحويل إلى الوحدة الأساسية
            if (requiredStock > _selectedProduct.CurrentStock)
            {
                lblValidationMessage.Text = $"الكمية المطلوبة ({requiredStock:F2}) تتجاوز المتاح ({_selectedProduct.CurrentStock:F2})";
                txtWholeQuantity.Focus();
                return false;
            }

            return true;
        }

        private void ValidateStock()
        {
            lblStockWarning.Text = "";

            if (_selectedProduct == null || !TryParseDecimal(txtWholeQuantity.Text, out var quantity))
                return;

            var requiredStock = quantity * _currentUnitFactor;
            if (requiredStock > _selectedProduct.CurrentStock)
            {
                lblStockWarning.Text = $"تحذير: الكمية تتجاوز المتاح ({_selectedProduct.CurrentStock:F2})";
                lblStockStatus.Foreground = System.Windows.Media.Brushes.Red;
            }
            else
            {
                lblStockWarning.Text = "";
                lblStockStatus.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        #endregion

        #region Keyboard Navigation

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // منع إغلاق الـ Dialog بـ Escape إذا كان هناك تغييرات غير محفوظة
            if (e.Key == Key.Escape)
            {
                if (HasUnsavedChanges())
                {
                    var result = MessageBox.Show(
                        "هناك تغييرات لم يتم حفظها. هل تريد الخروج بدون حفظ؟",
                        "تأكيد الخروج",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                    {
                        e.Handled = true;
                        return;
                    }
                }
                DialogResult = false;
                return;
            }

            // معالجة Enter التسلسلية
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                HandleEnterNavigation(e);
            }

            // معالجة F1 للبحث
            if (e.Key == Key.F1)
            {
                var focusedElement = Keyboard.FocusedElement;
                if (focusedElement == cmbProduct)
                    ShowProductSearchDialog();
                e.Handled = true;
            }
        }

        private void HandleEnterNavigation(KeyEventArgs e)
        {
            var focusedElement = Keyboard.FocusedElement as FrameworkElement;
            if (focusedElement == null) return;

            // معالجة خاصة للخصم: Enter = موافق
            if (ReferenceEquals(focusedElement, txtDiscount))
            {
                e.Handled = true;
                btnOK_Click(this, new RoutedEventArgs());
                return;
            }

            // معالجة خاصة للأسعار: أول Enter = SelectAll، ثاني Enter = التالي
            if (ReferenceEquals(focusedElement, txtWholePrice) || ReferenceEquals(focusedElement, txtPartPrice))
            {
                var textBox = (TextBox)focusedElement;
                if (textBox.SelectionLength != textBox.Text.Length)
                {
                    textBox.SelectAll();
                    e.Handled = true;
                    return;
                }
            }

            // تنقل عادي للحقول الأخرى
            e.Handled = true;
            MoveFocusToNext();
        }

        private static void MoveFocusToNext()
        {
            var request = new TraversalRequest(FocusNavigationDirection.Next);
            var current = Keyboard.FocusedElement as UIElement;
            current?.MoveFocus(request);
            if (Keyboard.FocusedElement is TextBox nextTb)
                nextTb.SelectAll();
        }

        #endregion

        #region Event Handlers

        private async void btnOK_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                SetBusy(true);

                // إنشاء/تحديث السطر
                CreateResultItem();

                UpdateStatusBar("تم الحفظ بنجاح");
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في حفظ البيانات: {ex.Message}");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void btnAddAnother_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!ValidateInput())
                    return;

                CreateResultItem();
                AddAnotherRequested = true;
                DialogResult = true;
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في حفظ البيانات: {ex.Message}");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (HasUnsavedChanges())
            {
                var result = MessageBox.Show(
                    "هناك تغييرات لم يتم حفظها. هل تريد الخروج بدون حفظ؟",
                    "تأكيد الخروج",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.No)
                    return;
            }

            DialogResult = false;
        }

        private async void btnPrevious_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditMode && _allItems != null && _currentItemIndex > 0)
            {
                _currentItemIndex--;
                await LoadItemForEditAsync(_allItems[_currentItemIndex]);
                UpdateNavigationButtons();
            }
        }

        private async void btnNext_Click(object sender, RoutedEventArgs e)
        {
            if (_isEditMode && _allItems != null && _currentItemIndex < _allItems.Count - 1)
            {
                _currentItemIndex++;
                await LoadItemForEditAsync(_allItems[_currentItemIndex]);
                UpdateNavigationButtons();
            }
        }

        private void btnSearchProduct_Click(object sender, RoutedEventArgs e)
        {
            ShowProductSearchDialog();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_isEditMode)
            {
                cmbProduct.Focus();
                _userOpenedProduct = false;
            }
        }

        #endregion

        #region Helper Methods

        private void CreateResultItem()
        {
            if (_selectedProduct == null || _selectedProductUnit == null)
                throw new InvalidOperationException("المنتج أو الوحدة غير محددة");

            if (!TryParseDecimal(txtWholeQuantity.Text, out var quantity) ||
                !TryParseDecimal(txtWholePrice.Text, out var price))
                throw new InvalidOperationException("بيانات غير صالحة");

            var discountAmount = 0m;
            if (TryParseDecimal(txtDiscount.Text, out var discount))
            {
                discountAmount = cmbDiscountType.SelectedIndex == 0
                    ? (quantity * price) * (discount / 100m)
                    : discount;
            }

            var lineTotal = (quantity * price) - discountAmount;

            ResultItem = new SalesInvoiceItem
            {
                SalesInvoiceItemId = _currentItem?.SalesInvoiceItemId ?? 0,
                ProductId = _selectedProduct.ProductId,
                Product = _selectedProduct,
                UnitId = _selectedProductUnit.UnitId,
                Unit = _selectedProductUnit.Unit,
                Quantity = quantity,
                UnitPrice = price,
                DiscountAmount = discountAmount,
                NetAmount = lineTotal
            };
        }

        private bool HasUnsavedChanges()
        {
            // تحقق من وجود تغييرات غير محفوظة
            if (!_isEditMode)
            {
                return cmbProduct.SelectedItem != null ||
                       !string.IsNullOrWhiteSpace(txtWholeQuantity.Text) ||
                       (TryParseDecimal(txtWholePrice.Text, out var price) && price > 0);
            }

            // في وضع التعديل، قارن مع البيانات الأصلية
            if (_currentItem == null) return false;

            return _currentItem.ProductId != (_selectedProduct?.ProductId ?? 0) ||
                   _currentItem.UnitId != (_selectedProductUnit?.UnitId ?? 0) ||
                   _currentItem.Quantity != (TryParseDecimal(txtWholeQuantity.Text, out var qty) ? qty : 0) ||
                   _currentItem.UnitPrice != (TryParseDecimal(txtWholePrice.Text, out var prc) ? prc : 0);
        }

        private void UpdateNavigationButtons()
        {
            if (_allItems == null) return;

            btnPrevious.Visibility = _currentItemIndex > 0 ? Visibility.Visible : Visibility.Hidden;
            btnNext.Visibility = _currentItemIndex < _allItems.Count - 1 ? Visibility.Visible : Visibility.Hidden;
        }

        private void ShowProductSearchDialog()
        {
            try
            {
                var dialog = new ProductSearchDialog(_products.Select(p => ProductSearchItem.FromRaw(p)).ToList())
                { Owner = this };

                if (dialog.ShowDialog() == true && dialog.Selected != null)
                {
                    var selectedProduct = _products.FirstOrDefault(p => p.ProductId == dialog.Selected.ProductId);
                    if (selectedProduct != null)
                        cmbProduct.SelectedItem = selectedProduct;
                }
            }
            catch (Exception ex)
            {
                ShowError($"خطأ في فتح بحث المنتجات: {ex.Message}");
            }
        }

        private void UpdateStatusBar(string message)
        {
            lblStatusBar.Text = $"{message} - {DateTime.Now:HH:mm:ss}";
            lblStatus.Foreground = System.Windows.Media.Brushes.Green;
        }

        private void SetBusy(bool isBusy)
        {
            BusyOverlay.Visibility = isBusy ? Visibility.Visible : Visibility.Collapsed;
            IsEnabled = !isBusy;
        }

        private static bool TryParseDecimal(string? text, out decimal value)
        {
            text = NormalizeDigits(text ?? "0");
            return decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out value);
        }

        private static string NormalizeDigits(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return "0";
            s = s.Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                 .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9')
                 .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
                 .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9');
            s = s.Replace('٫', '.').Replace("٬", string.Empty).Replace(",", string.Empty);
            return s.Trim();
        }

        private void ShowError(string message)
        {
            MessageBox.Show(message, "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            lblStatus.Foreground = System.Windows.Media.Brushes.Red;
            UpdateStatusBar($"خطأ: {message}");
        }

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

        #endregion

        #region IDisposable

        protected override void OnClosed(EventArgs e)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            base.OnClosed(e);
        }

        #endregion
    }
}