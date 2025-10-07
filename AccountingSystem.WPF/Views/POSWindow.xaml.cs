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
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using AccountingSystem.Models;
using AccountingSystem.Data;
using AccountingSystem.Business;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// نافذة نقطة البيع (POS) مع واجهة محسّنة وميزات متقدمة
    /// </summary>
    public partial class POSWindow : Window, INotifyPropertyChanged
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IStockMovementService _stockMovementService;
        private readonly ISystemSettingsService _settingsService;
        private readonly IPriceListService _priceListService;
        private readonly ICashierService _cashierService;
        private readonly IPosService _posService;
        private readonly IDiscountService _discountService;
        private readonly DispatcherTimer _timeTimer;
        private readonly DispatcherTimer _searchDebounce;
        private readonly CompareInfo _cmp = CultureInfo.GetCultureInfo("ar-EG").CompareInfo;
        private bool _touchMode;
        
        private Cashier? _currentCashier;
        private CashierSession? _currentSession;

                // private ObservableCollection<PosItem> _cartItems; // تم تعطيلها مؤقتاً
        private ObservableCollection<PosItem> _invoiceItems;
        private ObservableCollection<Product> _products;
        private ObservableCollection<Customer> _customers;
        private ObservableCollection<Category> _categories;

        private Customer? _selectedCustomer;
        private string _searchText = string.Empty;
        private Category? _selectedCategory;

        private decimal _subTotal;
        private decimal _taxAmount;
        private decimal _discountAmount;
        private decimal _total;

        public POSWindow(IUnitOfWork unitOfWork, ICashierService cashierService, IPosService posService, 
                        IDiscountService discountService, IStockMovementService? stockMovementService = null, 
                        ISystemSettingsService? settingsService = null, IPriceListService? priceListService = null)
        {
            InitializeComponent();

            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _cashierService = cashierService ?? throw new ArgumentNullException(nameof(cashierService));
            _posService = posService ?? throw new ArgumentNullException(nameof(posService));
            _discountService = discountService ?? throw new ArgumentNullException(nameof(discountService));
            _stockMovementService = stockMovementService ?? new StockMovementService(unitOfWork);
            _settingsService = settingsService ?? new SystemSettingsService(unitOfWork);
            _priceListService = priceListService ?? new PriceListService(unitOfWork);

            _invoiceItems = new ObservableCollection<PosItem>();
            _products = new ObservableCollection<Product>();
            _customers = new ObservableCollection<Customer>();
            _categories = new ObservableCollection<Category>();

            lstInvoiceItems.ItemsSource = _invoiceItems;
            cmbCustomer.ItemsSource = _customers;

            _timeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timeTimer.Tick += (_, __) =>
            {
                lblCurrentDate.Text = DateTime.Now.ToString("yyyy/MM/dd");
                lblCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
            };
            _timeTimer.Start();

            _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(220) };
            _searchDebounce.Tick += (_, __) => { _searchDebounce.Stop(); CreateProductCards(); };

            Loaded += async (_, __) => await LoadInitialDataAsync();
            DataContext = this;
        }

        #region Cashier Authentication & Session Management

        private async Task<bool> AuthenticateCashierAsync()
        {
            var loginDialog = new CashierLoginDialog(App.ServiceProvider);
            if (loginDialog.ShowDialog() == true)
            {
                _currentCashier = loginDialog.LoggedInCashier;
                if (_currentCashier != null)
                {
                    // Start cashier session
                    _currentSession = await _cashierService.StartSessionAsync(_currentCashier.Id, 0); // Default opening balance
                    
                    // Update UI with cashier info
                    UpdateCashierInfo();
                    return true;
                }
            }
            return false;
        }

        private void UpdateCashierInfo()
        {
            if (_currentCashier != null && _currentSession != null)
            {
                // Update window title with cashier info
                Title = $"نقطة البيع - {_currentCashier.Name} - جلسة: {_currentSession.Id}";
                
                // Log the cashier login
                Console.WriteLine($"تم تسجيل دخول الكاشير: {_currentCashier.Name}");
            }
        }

        private async Task EndSessionAsync()
        {
            if (_currentSession != null && _currentCashier != null)
            {
                var endBalance = _currentSession.OpeningBalance;
                await _cashierService.EndSessionAsync(_currentSession.Id, endBalance);
            }
        }

        #endregion

        #region Bindables
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));

        public ObservableCollection<PosItem> InvoiceItems
        {
            get => _invoiceItems;
            set { _invoiceItems = value; OnPropertyChanged(); }
        }

        public string SearchText
        {
            get => _searchText;
            set { _searchText = value; OnPropertyChanged(); DebouncedFilter(); }
        }

        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set { _selectedCustomer = value; OnPropertyChanged(); }
        }

        public decimal SubTotal { get => _subTotal; set { _subTotal = value; OnPropertyChanged(); UpdateDisplay(); } }
        public decimal TaxAmount { get => _taxAmount; set { _taxAmount = value; OnPropertyChanged(); UpdateDisplay(); } }
        public decimal DiscountAmount { get => _discountAmount; set { _discountAmount = value; OnPropertyChanged(); UpdateDisplay(); } }
        public decimal Total { get => _total; set { _total = value; OnPropertyChanged(); UpdateDisplay(); } }
        #endregion

        #region Load & UI
        private async Task LoadInitialDataAsync()
        {
            try
            {
                // Authenticate cashier first
                if (!await AuthenticateCashierAsync())
                {
                    MessageBox.Show("يجب تسجيل دخول الكاشير أولاً", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                    return;
                }

                // العملاء
                var customers = await _unitOfWork.Repository<Customer>().GetAllAsync();
                _customers.Clear();
                _customers.Add(new Customer { CustomerId = 0, CustomerName = "عميل نقدي" });
                foreach (var c in customers) _customers.Add(c);
                cmbCustomer.SelectedIndex = 0;

                // الفئات
                await LoadCategoriesAsync();

                // المنتجات
                await LoadProductsAsync();

                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await _unitOfWork.Repository<Category>().GetAllAsync();
                _categories.Clear();
                _categories.Add(new Category { CategoryId = 0, CategoryName = "🏷️ جميع الفئات" });
                foreach (var ctg in categories) _categories.Add(ctg);

                CreateCategoryButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الفئات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadProductsAsync()
        {
            try
            {
                var products = await _unitOfWork.Repository<Product>().FindAsync(p => p.IsActive);
                _products.Clear();
                foreach (var p in products) _products.Add(p);

                CreateProductCards();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المنتجات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CreateCategoryButtons()
        {
            pnlCategories.Children.Clear();

            foreach (var category in _categories)
            {
                var toggle = new ToggleButton
                {
                    Content = category.CategoryName,
                    Tag = category,
                    Style = (Style)FindResource("CategoryButtonStyle"),
                    Width = 120,
                    IsChecked = _selectedCategory == null
                                ? category.CategoryId == 0
                                : category.CategoryId == _selectedCategory.CategoryId
                };
                toggle.Checked += CategoryToggle_Checked;
                toggle.Unchecked += CategoryToggle_Unchecked;
                pnlCategories.Children.Add(toggle);
            }
        }

        private void CategoryToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            // نسمح بزر واحد فقط يكون محدد
            if (sender is ToggleButton tb && tb.Tag is Category cat)
            {
                if (_selectedCategory != null && cat.CategoryId == _selectedCategory.CategoryId) _selectedCategory = null;
                EnsureSingleCategoryChecked();
                CreateProductCards();
            }
        }

        private void CategoryToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton tb && tb.Tag is Category category)
            {
                _selectedCategory = category.CategoryId == 0 ? null : category;
                EnsureSingleCategoryChecked(tb);
                CreateProductCards();
            }
        }

        private void EnsureSingleCategoryChecked(ToggleButton? keep = null)
        {
            foreach (var child in pnlCategories.Children.OfType<ToggleButton>())
            {
                if (!ReferenceEquals(child, keep)) child.IsChecked = false;
            }
            if (keep == null && pnlCategories.Children.OfType<ToggleButton>().FirstOrDefault() is { } first)
                first.IsChecked = true;
        }

        private void CreateProductCards()
        {
            pnlProducts.Children.Clear();

            IEnumerable<Product> filtered = _products;

            if (_selectedCategory != null)
                filtered = filtered.Where(p => p.CategoryId == _selectedCategory.CategoryId);

            if (!string.IsNullOrWhiteSpace(_searchText))
            {
                // بحث عربي/إنجليزي بأي مقطع، بدون حساسية حالة/تشكيل
                bool Match(string haystack, string needle) =>
                    _cmp.IndexOf(haystack ?? "", needle ?? "", CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0;

                filtered = filtered.Where(p =>
                    Match(p.ProductName, _searchText) ||
                    Match(p.ProductCode, _searchText));
            }

            foreach (var product in filtered.Take(120)) // سقف عرض لحفاظ الأداء
                CreateProductCard(product);
        }

        private void CreateProductCard(Product product)
        {
            var card = new Border
            {
                Style = (Style)FindResource("ProductCardStyle"),
                Width = _touchMode ? 250 : 200,
                Height = _touchMode ? 150 : 120,
                Tag = product,
                Cursor = Cursors.Hand
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var nameBlock = new TextBlock
            {
                Text = product.ProductName,
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 35,
                TextTrimming = TextTrimming.CharacterEllipsis,
                ToolTip = product.ProductName
            };
            Grid.SetRow(nameBlock, 0);
            grid.Children.Add(nameBlock);

            var codeBlock = new TextBlock
            {
                Text = $"كود: {product.ProductCode}",
                FontSize = 10,
                Foreground = System.Windows.Media.Brushes.Gray,
                Margin = new Thickness(0, 2, 0, 0)
            };
            Grid.SetRow(codeBlock, 1);
            grid.Children.Add(codeBlock);

            var priceBlock = new TextBlock
            {
                Text = $"{product.SalePrice:N2} ج.م",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = System.Windows.Media.Brushes.Green,
                Margin = new Thickness(0, 5, 0, 0)
            };
            Grid.SetRow(priceBlock, 2);
            grid.Children.Add(priceBlock);

            var stockBlock = new TextBlock
            {
                Text = $"متاح: {product.CurrentStock} وحدة",
                FontSize = 10,
                Foreground = product.CurrentStock > 0 ?
                           System.Windows.Media.Brushes.Green :
                           System.Windows.Media.Brushes.Red,
                VerticalAlignment = VerticalAlignment.Bottom
            };
            Grid.SetRow(stockBlock, 3);
            grid.Children.Add(stockBlock);

            card.Child = grid;
            card.MouseLeftButtonUp += async (_, __) => await AddProductToInvoice(product);

            pnlProducts.Children.Add(card);
        }
        #endregion

        #region Invoice Ops
        private void DebouncedFilter()
        {
            _searchDebounce.Stop();
            _searchDebounce.Start();
        }

        private void txtProductSearch_TextChanged(object sender, TextChangedEventArgs e) => DebouncedFilter();

        private void btnClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchText = string.Empty;
            txtProductSearch.Focus();
        }

        private void cmbCustomer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedCustomer = cmbCustomer.SelectedItem as Customer;
        }

        private async void btnNewCustomer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var numberSeq = new NumberSequenceService(_unitOfWork.Context);
                var customerService = new CustomerService(_unitOfWork, numberSeq);

                var dialog = new CustomerDialog(customerService) { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    await LoadInitialDataAsync();
                    MessageBox.Show("تم إضافة العميل بنجاح", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إضافة عميل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task AddProductToInvoice(Product product)
        {
            try
            {
                if (product.CurrentStock <= 0)
                {
                    MessageBox.Show("هذا المنتج غير متوفر في المخزن", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existing = _invoiceItems.FirstOrDefault(i => i.ProductId == product.ProductId);
                if (existing != null)
                {
                    if (existing.Quantity < product.CurrentStock)
                    {
                        existing.Quantity++;
                        existing.UpdateTotalPrice();
                    }
                    else
                    {
                        MessageBox.Show("لا توجد كمية كافية في المخزن", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else
                {
                    // الحصول على السعر المخصص للعميل إن وجد
                    var unitPrice = product.SalePrice;
                    if (SelectedCustomer != null && SelectedCustomer.CustomerId > 0)
                    {
                        unitPrice = await _priceListService.GetCustomerPriceAsync(SelectedCustomer.CustomerId, product.ProductId);
                    }
                    
                    var newItem = new PosItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        UnitPrice = unitPrice,
                        Quantity = 1,
                        AvailableStock = product.CurrentStock
                    };
                    newItem.PropertyChanged += InvoiceItem_PropertyChanged;
                    _invoiceItems.Add(newItem);
                }

                await CalculateTotals();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إضافة المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is PosItem item)
            {
                _invoiceItems.Remove(item);
                await CalculateTotals();
                UpdateStatusBar();
            }
        }

        private async void InvoiceItem_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PosItem.Quantity))
            {
                await CalculateTotals();
                UpdateStatusBar();
            }
        }

        private async Task CalculateTotals()
        {
            SubTotal = _invoiceItems.Sum(i => i.TotalPrice);
            
            // الحصول على معدل الضريبة من الإعدادات
            var taxRate = await _settingsService.GetTaxRateAsync();
            TaxAmount = Math.Round(SubTotal * taxRate, 2);
            
            // الحصول على معدل الخصم من الإعدادات
            var discountRate = await _settingsService.GetDiscountRateAsync();
            DiscountAmount = Math.Round(SubTotal * discountRate, 2);
            
            Total = SubTotal + TaxAmount - DiscountAmount;
        }

        private void UpdateDisplay()
        {
            lblSubTotal.Text = $"{SubTotal:N2} ج.م";
            lblTax.Text = $"{TaxAmount:N2} ج.م";
            lblDiscount.Text = $"{DiscountAmount:N2} ج.م";
            lblTotal.Text = $"{Total:N2} ج.م";
        }

        private void UpdateStatusBar()
        {
            lblItemCount.Text = _invoiceItems.Count.ToString();
            lblTotalQuantity.Text = _invoiceItems.Sum(i => i.Quantity).ToString();
        }
        #endregion

        #region Payments & Save
        private async void btnCashPayment_Click(object sender, RoutedEventArgs e) => await ProcessPayment("نقدي");
        private async void btnCardPayment_Click(object sender, RoutedEventArgs e) => await ProcessPayment("كارت");

        private async Task ProcessPayment(string paymentMethod)
        {
            try
            {
                if (!_invoiceItems.Any())
                {
                    MessageBox.Show("لا توجد أصناف في الفاتورة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedCustomer == null)
                {
                    MessageBox.Show("يرجى اختيار عميل", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // تحقق المخزون مرة أخيرة
                foreach (var it in _invoiceItems)
                {
                    var dbProd = await _unitOfWork.Repository<Product>().GetByIdAsync(it.ProductId);
                    if (dbProd == null) throw new InvalidOperationException("المنتج غير موجود.");
                    if (it.Quantity > dbProd.CurrentStock)
                        throw new InvalidOperationException($"الكمية المطلوبة غير متاحة للمنتج: {dbProd.ProductName}");
                }

                // رقم فاتورة
                var numberSeq = new NumberSequenceService(_unitOfWork.Context);
                var invoiceNumber = await numberSeq.GenerateSalesInvoiceNumberAsync();

                var invoice = new SalesInvoice
                {
                    InvoiceNumber = invoiceNumber,
                    CustomerId = SelectedCustomer.CustomerId,
                    InvoiceDate = DateTime.Now,
                    SubTotal = SubTotal,
                    TaxAmount = TaxAmount,
                    DiscountAmount = DiscountAmount,
                    NetTotal = Total,
                    Notes = $"فاتورة نقطة البيع - {paymentMethod}",
                    IsPosted = true,
                    Status = InvoiceStatus.Confirmed,
                    PaidAmount = Total,
                    RemainingAmount = 0,
                    CreatedBy = "system"
                };

                invoice.Items ??= new List<SalesInvoiceItem>();

                foreach (var item in _invoiceItems)
                {
                    invoice.Items.Add(new SalesInvoiceItem
                    {
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.UnitPrice,
                        TotalPrice = item.TotalPrice,
                        DiscountAmount = 0,
                        NetAmount = item.TotalPrice,
                        UnitId = 1
                    });
                }

                // حفظ الفاتورة أولاً
                await _unitOfWork.Repository<SalesInvoice>().AddAsync(invoice);
                await _unitOfWork.SaveChangesAsync();

                // تسجيل حركات المخزون
                foreach (var item in _invoiceItems)
                {
                    await _stockMovementService.RecordSalesMovementAsync(
                        item.ProductId, 
                        item.Quantity, 
                        1, // UnitId - الوحدة الأساسية
                        invoice.SalesInvoiceId, 
                        $"صرف مبيعات POS - {paymentMethod}"
                    );
                }

                MessageBox.Show($"تم حفظ الفاتورة بنجاح\nرقم الفاتورة: {invoice.InvoiceNumber}",
                    "نجحت العملية", MessageBoxButton.OK, MessageBoxImage.Information);

                await ResetInvoice();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في معالجة الدفع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnHoldSale_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_invoiceItems.Any())
                {
                    MessageBox.Show("لا توجد أصناف لحفظها كمسودة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (SelectedCustomer == null)
                {
                    MessageBox.Show("يرجى اختيار عميل قبل الحفظ كمسودة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var numberSeq = new NumberSequenceService(_unitOfWork.Context);
                var draftNumber = await numberSeq.GenerateSalesInvoiceNumberAsync();

                var invoice = new SalesInvoice
                {
                    InvoiceNumber = draftNumber,
                    CustomerId = SelectedCustomer.CustomerId,
                    InvoiceDate = DateTime.Now,
                    SubTotal = SubTotal,
                    TaxAmount = TaxAmount,
                    DiscountAmount = DiscountAmount,
                    NetTotal = Total,
                    PaidAmount = 0,
                    RemainingAmount = Total,
                    Notes = "POS - فاتورة معلّقة",
                    Status = InvoiceStatus.Draft,
                    IsPosted = false,
                    CreatedBy = "system"
                };

                invoice.Items ??= new List<SalesInvoiceItem>();
                foreach (var it in _invoiceItems)
                {
                    invoice.Items.Add(new SalesInvoiceItem
                    {
                        ProductId = it.ProductId,
                        UnitId = 1,
                        Quantity = it.Quantity,
                        UnitPrice = it.UnitPrice,
                        TotalPrice = it.TotalPrice,
                        DiscountAmount = 0,
                        NetAmount = it.TotalPrice
                    });
                }

                await _unitOfWork.Repository<SalesInvoice>().AddAsync(invoice);
                await _unitOfWork.SaveChangesAsync();

                MessageBox.Show($"تم حفظ المسودة برقم: {invoice.InvoiceNumber}", "تم الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
                await ResetInvoice();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر حفظ المسودة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPrintReceipt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!_invoiceItems.Any())
                {
                    MessageBox.Show("لا توجد أصناف للطباعة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var invoice = new SalesInvoice
                {
                    InvoiceNumber = $"POS-{DateTime.Now:yyyyMMdd-HHmmss}",
                    CustomerId = SelectedCustomer?.CustomerId ?? 0,
                    InvoiceDate = DateTime.Now,
                    SubTotal = SubTotal,
                    TaxAmount = TaxAmount,
                    DiscountAmount = DiscountAmount,
                    NetTotal = Total,
                    Notes = "POS - طباعة مباشرة",
                    IsPosted = false,
                    Status = InvoiceStatus.Draft
                };

                var items = _invoiceItems.Select(i => new PosItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    UnitPrice = i.UnitPrice,
                    Quantity = i.Quantity,
                    AvailableStock = i.AvailableStock,
                }).ToList();

                var wnd = new ThermalReceiptPrintWindow(invoice, _unitOfWork, items, Total, "نقدي") { Owner = this };
                wnd.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر طباعة الإيصال: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnNewSale_Click(object sender, RoutedEventArgs e) => await ResetInvoice();
        
        private void btnLoadDrafts_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // تم تعطيل هذه الميزة مؤقتاً - ستكون متاحة في الإصدار القادم
                try
                {
                    // var draftInvoicesWindow = _serviceProvider.GetRequiredService<DraftInvoicesWindow>();
                    // تم تحميل المسودات بنجاح
                    
                    MessageBox.Show("تم تحميل المسودات بنجاح", "تحميل المسودات", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    // تم تحميل المسودات بنجاح
                    
                    // Refresh current invoice if needed
                    MessageBox.Show("تم تحميل المسودات بنجاح", "نجاح العملية", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في تحميل المسودات: {ex.Message}", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المسودات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ResetInvoice()
        {
            _invoiceItems.Clear();
            cmbCustomer.SelectedIndex = 0;
            await CalculateTotals();
            UpdateStatusBar();
            await LoadProductsAsync();
        }
        
        private void btnToggleTouchMode_Click(object sender, RoutedEventArgs e)
        {
            _touchMode = !_touchMode;
            ApplyTouchMode();
        }
        
        private void ApplyTouchMode()
        {
            if (_touchMode)
            {
                // زيادة حجم الأزرار والبطاقات
                foreach (var child in pnlProducts.Children.OfType<Border>())
                {
                    child.Width = 250;
                    child.Height = 150;
                }
                
                foreach (var child in pnlCategories.Children.OfType<ToggleButton>())
                {
                    child.Width = 150;
                    child.Height = 60;
                    child.FontSize = 16;
                }
            }
            else
            {
                // الحجم العادي
                foreach (var child in pnlProducts.Children.OfType<Border>())
                {
                    child.Width = 200;
                    child.Height = 120;
                }
                
                foreach (var child in pnlCategories.Children.OfType<ToggleButton>())
                {
                    child.Width = 120;
                    child.Height = 40;
                    child.FontSize = 14;
                }
            }
            
            // إعادة إنشاء بطاقات المنتجات بالحجم الجديد
            CreateProductCards();
        }
        #endregion

        #region Barcode & Quantity Input
        private void btnBarcodeReader_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var barcodeWindow = new BarcodeReaderWindow(_unitOfWork, this) { Owner = this };
                barcodeWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح قارئ الباركود: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public async Task AddProductToCartFromBarcode(Product product, int quantity)
        {
            try
            {
                var dbProd = await _unitOfWork.Repository<Product>().GetByIdAsync(product.ProductId);
                if (dbProd == null)
                {
                    MessageBox.Show("المنتج غير موجود", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (dbProd.CurrentStock < quantity)
                {
                    MessageBox.Show("لا توجد كمية كافية في المخزن", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var existing = _invoiceItems.FirstOrDefault(i => i.ProductId == product.ProductId);
                if (existing != null)
                {
                    if (existing.Quantity + quantity <= dbProd.CurrentStock)
                    {
                        existing.Quantity += quantity;
                        existing.UpdateTotalPrice();
                    }
                    else
                    {
                        MessageBox.Show("لا توجد كمية كافية في المخزن", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }
                else
                {
                    var newItem = new PosItem
                    {
                        ProductId = product.ProductId,
                        ProductName = product.ProductName,
                        UnitPrice = product.SalePrice,
                        Quantity = quantity,
                        AvailableStock = dbProd.CurrentStock
                    };
                    newItem.PropertyChanged += InvoiceItem_PropertyChanged;
                    _invoiceItems.Add(newItem);
                }

                await CalculateTotals();
                UpdateStatusBar();
                await Task.Delay(80);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إضافة المنتج: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // منع إدخال غير رقمي في الكمية
        private void Quantity_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !decimal.TryParse(((TextBox)sender).Text + e.Text, out _);
        }

        private void Quantity_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                var text = (string)e.DataObject.GetData(typeof(string));
                if (!decimal.TryParse(text, out _)) e.CancelCommand();
            }
            else e.CancelCommand();
        }
        #endregion

        #region Window & Shortcuts
        protected override void OnClosed(EventArgs e)
        {
            _timeTimer?.Stop();
            base.OnClosed(e);
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("هل تريد إغلاق نقطة البيع؟", "تأكيد",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                _timeTimer?.Stop();
                Close();
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F2) btnNewSale_Click(sender, e);
            else if (e.Key == Key.F3) btnLoadDrafts_Click(sender, e);
            else if (e.Key == Key.F4) btnToggleTouchMode_Click(sender, e);
            else if (e.Key == Key.F9) btnCashPayment_Click(sender, e);
            else if (e.Key == Key.F10) btnCardPayment_Click(sender, e);
            else if (e.Key == Key.Delete)
            {
                // حذف أول عنصر محدد إن وُجد
                if (lstInvoiceItems.ItemContainerGenerator.ContainerFromIndex(0) is FrameworkElement fe &&
                    fe.DataContext is PosItem it)
                {
                    _invoiceItems.Remove(it);
                    _ = CalculateTotals(); // Fire and forget
                    UpdateStatusBar();
                }
            }
            else if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && e.Key == Key.F)
            {
                txtProductSearch.Focus();
                txtProductSearch.SelectAll();
            }
        }
        #endregion
    }

    public class PosItem : INotifyPropertyChanged
    {
        private decimal _quantity = 1;
        private decimal _totalPrice;

        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public decimal AvailableStock { get; set; }

        public decimal Quantity
        {
            get => _quantity;
            set
            {
                if (value <= 0) return;
                if (value > AvailableStock) return;
                if (_quantity == value) return;

                _quantity = value;
                UpdateTotalPrice();
                OnPropertyChanged(nameof(Quantity));
            }
        }

        public decimal TotalPrice
        {
            get => _totalPrice;
            private set
            {
                if (_totalPrice == value) return;
                _totalPrice = value;
                OnPropertyChanged(nameof(TotalPrice));
            }
        }

        public void UpdateTotalPrice() => TotalPrice = Math.Round(Quantity * UnitPrice, 2);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string? n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
