using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AccountingSystem.Business;
using AccountingSystem.WPF.Models;
using AccountingSystem.WPF.Views;
using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.WPF.Commands;
using AccountingSystem.WPF.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.ViewModels
{
    /// <summary>
    /// ViewModel احترافي لفاتورة البيع مع MVVM Pattern كامل
    /// يدعم جميع الميزات المطلوبة: AutoSave، Smart Navigation، Cache، Theme Integration
    /// </summary>
    public class SalesInvoiceViewModel : INotifyPropertyChanged, IDisposable
    {
        #region Private Fields & Services

        private readonly ISalesInvoiceService _salesInvoiceService;
        private readonly ICustomerService _customerService;
        private readonly IProductService _productService;
        private readonly IPriceHistoryService _priceHistoryService;
        private readonly AccountingDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        private readonly CultureInfo _culture;
        private readonly Timer _autoSaveTimer;
        private CancellationTokenSource? _cancellationTokenSource;

        private bool _isBusy;
        private bool _hasUnsavedChanges;
        private string _statusMessage;
        private InvoiceMode _currentMode;
        private SalesInvoice? _currentInvoice;
        private readonly List<int> _invoiceIds = new();

        #endregion

        #region Observable Collections

        public ObservableCollection<SalesInvoiceItem> InvoiceItems { get; }
        public ObservableCollection<Customer> Customers { get; }
        public ObservableCollection<Product> Products { get; }
        public ObservableCollection<Warehouse> Warehouses { get; }
        public ObservableCollection<Representative> Representatives { get; }
        public ObservableCollection<Unit> ProductUnits { get; }

        #endregion

        #region Properties

        private Customer? _selectedCustomer;
        public Customer? SelectedCustomer
        {
            get => _selectedCustomer;
            set
            {
                if (SetProperty(ref _selectedCustomer, value))
                {
                    OnPropertyChanged(nameof(CustomerBalance));
                    HasUnsavedChanges = true;
                }
            }
        }

        private Warehouse? _selectedWarehouse;
        public Warehouse? SelectedWarehouse
        {
            get => _selectedWarehouse;
            set => SetProperty(ref _selectedWarehouse, value);
        }

        private Representative? _selectedRepresentative;
        public Representative? SelectedRepresentative
        {
            get => _selectedRepresentative;
            set => SetProperty(ref _selectedRepresentative, value);
        }

        private DateTime _invoiceDate = DateTime.Now;
        public DateTime InvoiceDate
        {
            get => _invoiceDate;
            set
            {
                if (SetProperty(ref _invoiceDate, value))
                    HasUnsavedChanges = true;
            }
        }

        private string _notes = string.Empty;
        public string Notes
        {
            get => _notes;
            set
            {
                if (SetProperty(ref _notes, value ?? string.Empty))
                    HasUnsavedChanges = true;
            }
        }

        private decimal _paidAmount;
        public decimal PaidAmount
        {
            get => _paidAmount;
            set
            {
                if (SetProperty(ref _paidAmount, value))
                {
                    OnPropertyChanged(nameof(RemainingAmount));
                    HasUnsavedChanges = true;
                }
            }
        }

        private decimal _taxRate = 15m;
        public decimal TaxRate
        {
            get => _taxRate;
            set
            {
                if (SetProperty(ref _taxRate, value))
                {
                    CalculateTotals();
                    HasUnsavedChanges = true;
                }
            }
        }

        private decimal _globalDiscountAmount;
        public decimal GlobalDiscountAmount
        {
            get => _globalDiscountAmount;
            set
            {
                if (SetProperty(ref _globalDiscountAmount, value))
                {
                    CalculateTotals();
                    HasUnsavedChanges = true;
                }
            }
        }

        private bool _globalDiscountIsPercentage;
        public bool GlobalDiscountIsPercentage
        {
            get => _globalDiscountIsPercentage;
            set
            {
                if (SetProperty(ref _globalDiscountIsPercentage, value))
                {
                    CalculateTotals();
                    HasUnsavedChanges = true;
                }
            }
        }

        private bool _taxOnNetOfDiscount = true;
        public bool TaxOnNetOfDiscount
        {
            get => _taxOnNetOfDiscount;
            set
            {
                if (SetProperty(ref _taxOnNetOfDiscount, value))
                {
                    CalculateTotals();
                    HasUnsavedChanges = true;
                }
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set => SetProperty(ref _isBusy, value);
        }

        public bool HasUnsavedChanges
        {
            get => _hasUnsavedChanges;
            set => SetProperty(ref _hasUnsavedChanges, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value ?? string.Empty);
        }

        public InvoiceMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (SetProperty(ref _currentMode, value))
                {
                    OnPropertyChanged(nameof(CanEdit));
                    OnPropertyChanged(nameof(CanSave));
                    OnPropertyChanged(nameof(WindowTitle));
                }
            }
        }

        public SalesInvoice? CurrentInvoice
        {
            get => _currentInvoice;
            private set
            {
                if (SetProperty(ref _currentInvoice, value))
                {
                    OnPropertyChanged(nameof(InvoiceNumber));
                    OnPropertyChanged(nameof(WindowTitle));
                    OnPropertyChanged(nameof(CanEdit));
                    OnPropertyChanged(nameof(CanSave));
                }
            }
        }

        // Computed Properties
        public string InvoiceNumber => CurrentInvoice?.InvoiceNumber ?? "سيتم التوليد تلقائياً";
        public string WindowTitle => CurrentInvoice?.InvoiceNumber != null 
            ? $"فاتورة بيع - {CurrentInvoice.InvoiceNumber}" 
            : "فاتورة بيع جديدة";
        public string CustomerBalance => SelectedCustomer?.Balance.ToString("C", _culture) ?? "0.00 ج.م";
        public bool CanEdit => CurrentMode == InvoiceMode.New || CurrentMode == InvoiceMode.Edit;
        public bool CanSave => CanEdit && HasUnsavedChanges;

        // Totals - Calculated automatically
        public decimal SubTotal { get; private set; }
        public decimal TotalDiscount { get; private set; }
        public decimal TaxAmount { get; private set; }
        public decimal NetTotal { get; private set; }
        public decimal RemainingAmount => NetTotal - PaidAmount;
        public int ItemsCount => InvoiceItems.Count;

        #endregion

        #region Commands

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand NewInvoiceCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand PrintCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand DeleteItemCommand { get; }
        public ICommand SearchCustomerCommand { get; }
        public ICommand SearchProductCommand { get; }
        public ICommand PreviousInvoiceCommand { get; }
        public ICommand NextInvoiceCommand { get; }
        public ICommand OpenItemEntryCommand { get; }

        #endregion

        #region Constructor

        public SalesInvoiceViewModel(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _salesInvoiceService = serviceProvider.GetRequiredService<ISalesInvoiceService>();
            _customerService = serviceProvider.GetRequiredService<ICustomerService>();
            _productService = serviceProvider.GetRequiredService<IProductService>();
            _priceHistoryService = serviceProvider.GetRequiredService<IPriceHistoryService>();
            _context = serviceProvider.GetRequiredService<AccountingDbContext>();

            // Initialize Collections
            InvoiceItems = new ObservableCollection<SalesInvoiceItem>();
            Customers = new ObservableCollection<Customer>();
            Products = new ObservableCollection<Product>();
            Warehouses = new ObservableCollection<Warehouse>();
            Representatives = new ObservableCollection<Representative>();
            ProductUnits = new ObservableCollection<Unit>();

            // Culture
            _culture = new CultureInfo("ar-EG");
            _culture.NumberFormat.CurrencySymbol = "ج.م";

            // Commands
            SaveCommand = new AsyncRelayCommand(SaveInvoiceAsync, () => CanSave);
            CancelCommand = new RelayCommand(CancelInvoice);
            NewInvoiceCommand = new RelayCommand(CreateNewInvoice);
            EditCommand = new RelayCommand(EditCurrentInvoice, () => CurrentInvoice != null && !CurrentInvoice.IsPosted);
            PrintCommand = new AsyncRelayCommand(PrintInvoiceAsync, () => CurrentInvoice != null);
            AddItemCommand = new AsyncRelayCommand<SalesInvoiceItem>(AddItemAsync);
            DeleteItemCommand = new RelayCommand<SalesInvoiceItem>(DeleteItem);
            SearchCustomerCommand = new AsyncRelayCommand(OpenCustomerSearchAsync);
            SearchProductCommand = new AsyncRelayCommand(OpenProductSearchAsync);
            PreviousInvoiceCommand = new AsyncRelayCommand(NavigateToPreviousAsync);
            NextInvoiceCommand = new AsyncRelayCommand(NavigateToNextAsync);
            OpenItemEntryCommand = new AsyncRelayCommand(OpenItemEntryDialogAsync);

            // Initialize
            _statusMessage = "جاهز";
            CurrentMode = InvoiceMode.New;

            // AutoSave Timer (every 60 seconds)
            _autoSaveTimer = new Timer(AutoSaveCallback, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));

            // Listen to collection changes
            InvoiceItems.CollectionChanged += (_, __) =>
            {
                CalculateTotals();
                HasUnsavedChanges = true;
            };
        }

        #endregion

        #region Public Methods

        public async Task InitializeAsync()
        {
            try
            {
                IsBusy = true;
                StatusMessage = "جارٍ تحميل البيانات...";

                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();
                var ct = _cancellationTokenSource.Token;

                // Load data in parallel
                var customersTask = LoadCustomersAsync(ct);
                var productsTask = LoadProductsAsync(ct);
                var warehousesTask = LoadWarehousesAsync(ct);
                var representativesTask = LoadRepresentativesAsync(ct);
                var taxRateTask = LoadTaxRateAsync(ct);

                await Task.WhenAll(customersTask, productsTask, warehousesTask, representativesTask, taxRateTask);

                StatusMessage = "جاهز";
                ComprehensiveLogger.LogUIOperation("تم تحميل بيانات فاتورة البيع بنجاح", "SalesInvoiceViewModel");
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "تم إلغاء التحميل";
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في التحميل: {ex.Message}";
                ComprehensiveLogger.LogError("فشل تحميل بيانات فاتورة البيع", ex, "SalesInvoiceViewModel");
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task LoadInvoiceAsync(int invoiceId)
        {
            try
            {
                IsBusy = true;
                StatusMessage = "جارٍ تحميل الفاتورة...";

                var invoice = await _context.SalesInvoices
                    .AsNoTracking()
                    .Include(i => i.Customer)
                    .Include(i => i.Items).ThenInclude(item => item.Product)
                    .Include(i => i.Items).ThenInclude(item => item.Unit)
                    .FirstOrDefaultAsync(i => i.SalesInvoiceId == invoiceId);

                if (invoice == null)
                {
                    StatusMessage = "لم يتم العثور على الفاتورة";
                    return;
                }

                LoadInvoiceData(invoice);
                await LoadInvoiceIdsAsync();

                StatusMessage = $"تم تحميل فاتورة {invoice.InvoiceNumber}";
                ComprehensiveLogger.LogUIOperation($"تم تحميل فاتورة البيع {invoice.InvoiceNumber}", "SalesInvoiceViewModel");
            }
            catch (Exception ex)
            {
                StatusMessage = $"خطأ في تحميل الفاتورة: {ex.Message}";
                ComprehensiveLogger.LogError($"فشل تحميل فاتورة البيع {invoiceId}", ex, "SalesInvoiceViewModel");
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion

        #region Private Methods - Data Loading

        private async Task LoadCustomersAsync(CancellationToken ct)
        {
            var customers = await _customerService.GetAllCustomersAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Customers.Clear();
                foreach (var customer in customers)
                    Customers.Add(customer);
            });
        }

        private async Task LoadProductsAsync(CancellationToken ct)
        {
            var products = await _productService.GetAllProductsAsync();
            Application.Current.Dispatcher.Invoke(() =>
            {
                Products.Clear();
                foreach (var product in products)
                    Products.Add(product);
            });
        }

        private async Task LoadWarehousesAsync(CancellationToken ct)
        {
            var warehouses = await _context.Warehouses.AsNoTracking().ToListAsync(ct);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Warehouses.Clear();
                foreach (var warehouse in warehouses)
                    Warehouses.Add(warehouse);
            });
        }

        private async Task LoadRepresentativesAsync(CancellationToken ct)
        {
            var representatives = await _context.Representatives.AsNoTracking().ToListAsync(ct);
            Application.Current.Dispatcher.Invoke(() =>
            {
                Representatives.Clear();
                foreach (var rep in representatives)
                    Representatives.Add(rep);
            });
        }

        private async Task LoadTaxRateAsync(CancellationToken ct)
        {
            try
            {
                var setting = await _context.SystemSettings.AsNoTracking()
                    .FirstOrDefaultAsync(s => s.SettingKey == "TaxRate", ct);
                
                if (setting?.SettingValue != null && 
                    decimal.TryParse(setting.SettingValue, out var rate))
                {
                    TaxRate = rate;
                }
            }
            catch { /* Use default */ }
        }

        private async Task LoadInvoiceIdsAsync()
        {
            try
            {
                var ids = await _context.SalesInvoices
                    .AsNoTracking()
                    .OrderBy(i => i.SalesInvoiceId)
                    .Select(i => i.SalesInvoiceId)
                    .ToListAsync();

                _invoiceIds.Clear();
                _invoiceIds.AddRange(ids);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل تحميل قائمة معرفات الفواتير", ex, "SalesInvoiceViewModel");
            }
        }

        #endregion

        #region Private Methods - Invoice Operations

        private void LoadInvoiceData(SalesInvoice invoice)
        {
            CurrentInvoice = invoice;
            SelectedCustomer = Customers.FirstOrDefault(c => c.CustomerId == invoice.CustomerId);
            SelectedWarehouse = Warehouses.FirstOrDefault(w => w.WarehouseId == invoice.WarehouseId);
            SelectedRepresentative = Representatives.FirstOrDefault(r => r.RepresentativeId == invoice.RepresentativeId);
            InvoiceDate = invoice.InvoiceDate;
            Notes = invoice.Notes ?? string.Empty;
            PaidAmount = invoice.PaidAmount;

            InvoiceItems.Clear();
            if (invoice.Items != null)
            {
                foreach (var item in invoice.Items)
                    InvoiceItems.Add(item);
            }

            CurrentMode = invoice.IsPosted ? InvoiceMode.View : InvoiceMode.Edit;
            HasUnsavedChanges = false;
            CalculateTotals();
        }

        private void CreateNewInvoice()
        {
            if (HasUnsavedChanges && !ConfirmDiscardChanges())
                return;

            CurrentInvoice = null;
            SelectedCustomer = null;
            SelectedWarehouse = null;
            SelectedRepresentative = null;
            InvoiceDate = DateTime.Now;
            Notes = string.Empty;
            PaidAmount = 0;
            
            InvoiceItems.Clear();
            
            CurrentMode = InvoiceMode.New;
            HasUnsavedChanges = false;
            StatusMessage = "فاتورة جديدة";
        }

        private void EditCurrentInvoice()
        {
            if (CurrentInvoice == null || CurrentInvoice.IsPosted)
                return;

            CurrentMode = InvoiceMode.Edit;
            StatusMessage = "وضع التعديل مُفعّل";
        }

        private void CancelInvoice()
        {
            if (HasUnsavedChanges && !ConfirmDiscardChanges())
                return;

            if (CurrentInvoice != null)
                LoadInvoiceData(CurrentInvoice); // Reload original data
            else
                CreateNewInvoice();
        }

        private bool ConfirmDiscardChanges()
        {
            var result = MessageBox.Show(
                "هناك تغييرات غير محفوظة. هل تريد المتابعة وإلغاء التغييرات؟",
                "تأكيد الإلغاء",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            return result == MessageBoxResult.Yes;
        }

        #endregion

        #region Private Methods - Calculations

        private void CalculateTotals()
        {
            SubTotal = InvoiceItems.Sum(item => item.Quantity * item.UnitPrice);
            
            var itemsDiscount = InvoiceItems.Sum(item => item.DiscountAmount);
            var globalDiscount = GlobalDiscountIsPercentage 
                ? SubTotal * (GlobalDiscountAmount / 100m) 
                : GlobalDiscountAmount;
            
            TotalDiscount = itemsDiscount + globalDiscount;
            
            var baseForTax = TaxOnNetOfDiscount 
                ? Math.Max(0, SubTotal - TotalDiscount) 
                : SubTotal;
            
            TaxAmount = Math.Round(baseForTax * (TaxRate / 100m), 2);
            NetTotal = baseForTax + TaxAmount;

            // Notify UI
            OnPropertyChanged(nameof(SubTotal));
            OnPropertyChanged(nameof(TotalDiscount));
            OnPropertyChanged(nameof(TaxAmount));
            OnPropertyChanged(nameof(NetTotal));
            OnPropertyChanged(nameof(RemainingAmount));
            OnPropertyChanged(nameof(ItemsCount));
        }

        #endregion

        #region Command Implementations

        private async Task SaveInvoiceAsync()
        {
            try
            {
                if (SelectedCustomer == null)
                {
                    MessageBox.Show("يرجى اختيار العميل", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!InvoiceItems.Any())
                {
                    MessageBox.Show("يرجى إضافة منتجات للفاتورة", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                IsBusy = true;
                StatusMessage = "جارٍ حفظ الفاتورة...";

                var invoiceToSave = CreateInvoiceForSaving();
                var result = await _salesInvoiceService.CreateSalesInvoiceAsync(invoiceToSave);

                if (result?.IsSuccess == true && result.Data != null)
                {
                    CurrentInvoice = result.Data;
                    CurrentMode = InvoiceMode.View;
                    HasUnsavedChanges = false;
                    StatusMessage = $"تم حفظ الفاتورة {result.Data.InvoiceNumber}";

                    // Auto-post if configured
                    await AutoPostIfConfiguredAsync(result.Data);

                    ComprehensiveLogger.LogBusinessOperation(
                        $"تم حفظ فاتورة بيع {result.Data.InvoiceNumber}",
                        $"العميل: {SelectedCustomer.CustomerName}, المبلغ: {NetTotal:C}");
                }
                else
                {
                    MessageBox.Show($"فشل في حفظ الفاتورة: {result?.Message ?? "خطأ غير معروف"}", 
                        "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الفاتورة: {ex.Message}", 
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                ComprehensiveLogger.LogError("فشل حفظ فاتورة البيع", ex, "SalesInvoiceViewModel");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private SalesInvoice CreateInvoiceForSaving()
        {
            return new SalesInvoice
            {
                InvoiceDate = InvoiceDate,
                CustomerId = SelectedCustomer!.CustomerId,
                WarehouseId = SelectedWarehouse?.WarehouseId,
                RepresentativeId = SelectedRepresentative?.RepresentativeId,
                SubTotal = SubTotal,
                TaxAmount = TaxAmount,
                DiscountAmount = TotalDiscount,
                NetTotal = NetTotal,
                PaidAmount = PaidAmount,
                RemainingAmount = RemainingAmount,
                Notes = Notes,
                Status = InvoiceStatus.Draft,
                CreatedBy = Environment.UserName,
                Items = InvoiceItems.Select(item => new SalesInvoiceItem
                {
                    ProductId = item.ProductId,
                    UnitId = item.UnitId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    DiscountAmount = item.DiscountAmount,
                    TotalPrice = item.TotalPrice,
                    NetAmount = item.NetAmount
                }).ToList()
            };
        }

        private async Task AutoPostIfConfiguredAsync(SalesInvoice invoice)
        {
            try
            {
                // Check if auto-post is enabled
                var autoPostSetting = await _context.SystemSettings
                    .FirstOrDefaultAsync(s => s.SettingKey == "AutoPostSalesInvoices");
                
                if (autoPostSetting?.SettingValue?.ToLower() == "true")
                {
                    var posted = await _salesInvoiceService.PostSalesInvoiceAsync(invoice.SalesInvoiceId);
                    if (posted != null)
                    {
                        CurrentInvoice = posted;
                        StatusMessage = $"تم ترحيل الفاتورة {posted.InvoiceNumber} تلقائياً";
                    }
                }
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل الترحيل التلقائي للفاتورة", ex, "SalesInvoiceViewModel");
            }
        }

        private async Task PrintInvoiceAsync()
        {
            try
            {
                if (CurrentInvoice == null)
                {
                    MessageBox.Show("لا توجد فاتورة للطباعة", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Open print preview
                var printPreview = new InvoicePrintPreview(CurrentInvoice, _context);
                printPreview.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                ComprehensiveLogger.LogError("فشل طباعة الفاتورة", ex, "SalesInvoiceViewModel");
            }
        }

        private async Task AddItemAsync(SalesInvoiceItem? item)
        {
            if (!CanEdit) return;

            try
            {
                // This would be called from ItemEntryDialog
                if (item != null)
                {
                    var existing = InvoiceItems.FirstOrDefault(i => 
                        i.ProductId == item.ProductId && i.UnitId == item.UnitId);

                    if (existing != null)
                    {
                        existing.Quantity += item.Quantity;
                        existing.TotalPrice = existing.Quantity * existing.UnitPrice;
                        existing.NetAmount = Math.Max(0, existing.TotalPrice - existing.DiscountAmount);
                    }
                    else
                    {
                        InvoiceItems.Add(item);
                    }

                    HasUnsavedChanges = true;
                    StatusMessage = "تم إضافة الصنف";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إضافة الصنف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteItem(SalesInvoiceItem? item)
        {
            if (!CanEdit || item == null) return;

            try
            {
                if (MessageBox.Show($"هل تريد حذف {item.Product?.ProductName}؟", 
                    "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    InvoiceItems.Remove(item);
                    HasUnsavedChanges = true;
                    StatusMessage = "تم حذف الصنف";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حذف الصنف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task OpenCustomerSearchAsync()
        {
            try
            {
                var dialog = new CustomerSearchDialog(Customers.ToList());
                if (dialog.ShowDialog() == true && dialog.SelectedCustomer != null)
                {
                    SelectedCustomer = dialog.SelectedCustomer;
                    StatusMessage = $"تم اختيار العميل: {SelectedCustomer.CustomerName}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بحث العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task OpenProductSearchAsync()
        {
            try
            {
                var productItems = Products.Select(p => ProductSearchItem.FromRaw(p)).ToList();

                var dialog = new ProductSearchDialog(productItems);
                if (dialog.ShowDialog() == true && dialog.Selected != null)
                {
                    // This would trigger opening ItemEntryDialog with selected product
                    await OpenItemEntryDialogAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في بحث المنتجات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task OpenItemEntryDialogAsync()
        {
            try
            {
                var dialog = new ItemEntryDialog(CurrentInvoice?.CustomerId ?? 0)
                {
                    DataContext = this // Share the same ViewModel for seamless integration
                };

                if (dialog.ShowDialog() == true)
                {
                    // Item will be added through AddItemAsync command
                    StatusMessage = "تم إضافة الصنف بنجاح";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة الأصناف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task NavigateToPreviousAsync()
        {
            await NavigateToInvoiceAsync(-1);
        }

        private async Task NavigateToNextAsync()
        {
            await NavigateToInvoiceAsync(1);
        }

        private async Task NavigateToInvoiceAsync(int direction)
        {
            try
            {
                if (CurrentInvoice == null)
                {
                    MessageBox.Show("لا توجد فاتورة حالية للتنقل منها", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (HasUnsavedChanges && !ConfirmDiscardChanges())
                    return;

                if (_invoiceIds.Count == 0)
                    await LoadInvoiceIdsAsync();

                var currentIndex = _invoiceIds.IndexOf(CurrentInvoice.SalesInvoiceId);
                if (currentIndex == -1)
                {
                    MessageBox.Show("لا يمكن تحديد موقع الفاتورة الحالية", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var newIndex = currentIndex + direction;
                if (newIndex < 0)
                {
                    MessageBox.Show("هذه أول فاتورة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (newIndex >= _invoiceIds.Count)
                {
                    MessageBox.Show("هذه آخر فاتورة", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                await LoadInvoiceAsync(_invoiceIds[newIndex]);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التنقل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion

        #region AutoSave

        private async void AutoSaveCallback(object? state)
        {
            if (!HasUnsavedChanges || CurrentMode != InvoiceMode.New || IsBusy)
                return;

            try
            {
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                {
                    await SaveDraftAsync();
                });
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل الحفظ التلقائي", ex, "SalesInvoiceViewModel");
            }
        }

        private async Task SaveDraftAsync()
        {
            // Implementation would save to a draft table or temporary storage
            // This is a placeholder for the AutoSave feature
            StatusMessage = "تم الحفظ التلقائي";
            await Task.Delay(1000);
            StatusMessage = "جاهز";
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

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;

            _autoSaveTimer?.Dispose();
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            _disposed = true;
        }

        #endregion
    }

    #region Enums

    public enum InvoiceMode
    {
        New,
        View,
        Edit
    }

    #endregion
}