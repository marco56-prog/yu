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
    public partial class PurchaseReturnWindow : Window
    {
        private readonly IPurchaseInvoiceService _purchaseInvoiceService;
        private readonly ISupplierService _supplierService;
        private readonly IProductService _productService;
        private readonly IPriceHistoryService _priceHistoryService;
        private readonly AccountingDbContext _context;
        private readonly IServiceProvider _serviceProvider;

        private readonly ObservableCollection<PurchaseReturnDetail> _returnDetails;
        private readonly ObservableCollection<Supplier> _suppliers;
        private readonly ObservableCollection<PurchaseInvoice> _originalInvoices;
        private ICollectionView _suppliersView;
        private ICollectionView _invoicesView;

        private readonly CultureInfo _culture;
        private decimal _taxRatePercent = 15m;

        private PurchaseReturn? _currentReturn;
        private PurchaseInvoice? _selectedOriginalInvoice;

        private const string TitleWarning = "تحذير";
        private const string TitleInfo = "تنبيه";

        private bool _isClosingConfirmed = false;

        public PurchaseReturnWindow(IServiceProvider serviceProvider)
        {
            try
            {
                InitializeComponent();

                _serviceProvider        = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
                _purchaseInvoiceService = serviceProvider.GetRequiredService<IPurchaseInvoiceService>();
                _supplierService        = serviceProvider.GetRequiredService<ISupplierService>();
                _productService         = serviceProvider.GetRequiredService<IProductService>();
                _priceHistoryService    = serviceProvider.GetRequiredService<IPriceHistoryService>();
                _context                = serviceProvider.GetRequiredService<AccountingDbContext>();

                _context.ChangeTracker.Clear();

                _returnDetails    = new ObservableCollection<PurchaseReturnDetail>();
                _suppliers        = new ObservableCollection<Supplier>();
                _originalInvoices = new ObservableCollection<PurchaseInvoice>();

                _suppliersView = CollectionViewSource.GetDefaultView(_suppliers);
                _invoicesView  = CollectionViewSource.GetDefaultView(_originalInvoices);

                _culture = new CultureInfo("ar-EG");
                _culture.NumberFormat.CurrencySymbol = "ج.م";

                dgItems.ItemsSource           = _returnDetails;
                cmbSupplier.ItemsSource       = _suppliersView;
                cmbOriginalInvoice.ItemsSource = _invoicesView;

                EnableEditMode(false);
                Closing += PurchaseReturnWindow_Closing;

                EventManager.RegisterClassHandler(typeof(TextBox),
                    UIElement.GotKeyboardFocusEvent,
                    new KeyboardFocusChangedEventHandler(TextBox_GotKeyboardFocus));
                EventManager.RegisterClassHandler(typeof(TextBox),
                    UIElement.PreviewMouseLeftButtonDownEvent,
                    new MouseButtonEventHandler(TextBox_PreviewMouseLeftButtonDown), true);

                Loaded += async (_, __) => await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل تهيئة نافذة مرتجع الشراء:\n{ex}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        // نفس المنطق من SalesReturnWindow مع تغيير للموردين بدلاً من العملاء
        private async Task LoadDataAsync()
        {
            try
            {
                _context.ChangeTracker.Clear();

                await LoadTaxRateAsync();
                _context.ChangeTracker.Clear();

                var suppliers = await _supplierService.GetAllSuppliersAsync();
                _suppliers.Clear();
                foreach (var s in suppliers) _suppliers.Add(s);

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
            catch { }
        }

        private static string NormalizeDigits(string? s) => s?
            .Replace('٠','0').Replace('١','1').Replace('٢','2').Replace('٣','3').Replace('٤','4')
            .Replace('٥','5').Replace('٦','6').Replace('٧','7').Replace('٨','8').Replace('٩','9')
            .Replace('۰','0').Replace('۱','1').Replace('۲','2').Replace('۳','3').Replace('۴','4')
            .Replace('۵','5').Replace('۶','6').Replace('۷','7').Replace('۸','8').Replace('۹','9')
            .Replace('٫','.') ?? "0";

        private bool TryParseDecimal(string? text, out decimal value)
        {
            text = NormalizeDigits(text ?? "0");
            return decimal.TryParse(text, NumberStyles.Any, _culture, out value) ||
                   decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value) ||
                   decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
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

        // أحداث المورد (بدلاً من العميل)
        private void cmbSupplier_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => SupplierSelectionChanged(sender, e);

        private async void SupplierSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (cmbSupplier?.SelectedItem is Supplier supplier)
            {
                lblPreviousBalance.Content = supplier.Balance.ToString("C", _culture);
                await LoadSupplierInvoicesAsync(supplier.SupplierId);
            }
            else
            {
                _originalInvoices.Clear();
                cmbOriginalInvoice.SelectedIndex = -1;
            }
        }

        private async Task LoadSupplierInvoicesAsync(int supplierId)
        {
            try
            {
                var invoices = await _context.PurchaseInvoices
                    .Include(i => i.Items).ThenInclude(d => d.Product)
                    .Include(i => i.Items).ThenInclude(d => d.Unit)
                    .Where(i => i.SupplierId == supplierId && i.Status == InvoiceStatus.Confirmed)
                    .OrderByDescending(i => i.InvoiceDate)
                    .Take(50)
                    .ToListAsync();

                _originalInvoices.Clear();
                foreach (var inv in invoices)
                {
                    _originalInvoices.Add(inv);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل فواتير المورد: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void cmbOriginalInvoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbOriginalInvoice.SelectedItem is PurchaseInvoice invoice)
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

        private void btnAddFromOriginal_Click(object sender, RoutedEventArgs e)
        {
            if (dgOriginalItems.SelectedItem is not PurchaseInvoiceItem originalItem)
            {
                MessageBox.Show("يرجى اختيار بند من الفاتورة الأصلية", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
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

                var previouslyReturned = _returnDetails.Where(r => r.OriginalInvoiceDetailId == originalItem.PurchaseInvoiceItemId)
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

                var returnDetail = new PurchaseReturnDetail
                {
                    ProductId = originalItem.ProductId,
                    Product = originalItem.Product,
                    UnitId = originalItem.UnitId,
                    Unit = originalItem.Unit,
                    Quantity = returnQty,
                    UnitPrice = originalItem.UnitCost,
                    TotalPrice = returnQty * originalItem.UnitCost,
                    ReturnReason = reason,
                    OriginalInvoiceDetailId = originalItem.PurchaseInvoiceItemId
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

            if (sender is Button btn && btn.Tag is PurchaseReturnDetail detailFromTag)
            {
                _returnDetails.Remove(detailFromTag);
                CalculateReturnTotals();
                return;
            }

            if (dgItems.SelectedItem is PurchaseReturnDetail detail)
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

        private async Task<PurchaseReturn?> SaveReturnAsync()
        {
            try
            {
                _context.ChangeTracker.Clear();

                if (cmbSupplier.SelectedItem is not Supplier supplier)
                {
                    MessageBox.Show("يرجى اختيار المورد", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
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

                var detailsForSave = _returnDetails.Select(d => new PurchaseReturnDetail
                {
                    ProductId = d.ProductId,
                    UnitId = d.UnitId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    TotalPrice = d.TotalPrice,
                    ReturnReason = d.ReturnReason,
                    OriginalInvoiceDetailId = d.OriginalInvoiceDetailId
                }).ToList();

                var purchaseReturn = new PurchaseReturn
                {
                    ReturnDate = dpReturnDate.SelectedDate ?? DateTime.Now,
                    SupplierId = supplier.SupplierId,
                    OriginalPurchaseInvoiceId = _selectedOriginalInvoice.PurchaseInvoiceId,
                    SubTotal = subTotal,
                    TaxAmount = taxAmount,
                    NetTotal = netTotal,
                    Notes = txtNotes.Text,
                    Status = InvoiceStatus.Draft,
                    CreatedBy = 1,
                    PurchaseReturnDetails = detailsForSave
                };

                SetBusy(true);
                
                // مؤقتاً حفظ مباشر في قاعدة البيانات
                _context.PurchaseReturns.Add(purchaseReturn);
                await _context.SaveChangesAsync();
                
                _currentReturn = purchaseReturn;
                lblReturnNumber.Text = $"PRET-{purchaseReturn.PurchaseReturnId}";

                EnableEditMode(false);
                UpdateButtonStates();

                MessageBox.Show("تم حفظ مرتجع الشراء بنجاح!", "نجح الحفظ", MessageBoxButton.OK, MessageBoxImage.Information);
                return purchaseReturn;
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
            btnAddFromOriginal.IsEnabled = !isBusy; // PurchaseReturn does not have posting state
        }

        private async void btnSave_Click(object sender, RoutedEventArgs e) => await SaveReturnAsync();
        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintCurrentReturn();
        private void btnNew_Click(object sender, RoutedEventArgs e) => NewClick(sender, e);
        private void btnCancel_Click(object sender, RoutedEventArgs e) => Close();

        private void PrintCurrentReturn()
        {
            MessageBox.Show("ميزة طباعة مرتجعات الشراء ستتوفر قريباً", "معلومة", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void NewClick(object? sender, RoutedEventArgs e)
        {
            if (HasUnsavedChanges())
            {
                var result = MessageBox.Show("هناك بيانات غير محفوظة. هل تريد حفظ المرتجع؟",
                    "حفظ المرتجع", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    Dispatcher.Invoke(async () =>
                    {
                        var saved = await SaveReturnAsync();
                        if (saved != null) PrepareNewReturn();
                    });
                    return;
                }
                else if (result == MessageBoxResult.Cancel) return;
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
            cmbSupplier.SelectedIndex = -1;
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

        private async void PurchaseReturnWindow_Closing(object? sender, CancelEventArgs e)
        {
            if (_isClosingConfirmed) return;

            if (!HasUnsavedChanges()) return;

            var result = MessageBox.Show("هناك بيانات غير محفوظة. هل تريد حفظها قبل الخروج?",
                "حفظ البيانات", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

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
                bool hasSupplier = cmbSupplier?.SelectedItem is Supplier;
                bool hasNotes = !string.IsNullOrWhiteSpace(txtNotes?.Text);
                return anyDetail || hasSupplier || hasNotes;
            }

            return false;
        }

        private void EnableEditMode(bool enabled)
        {
            cmbSupplier.IsEnabled = enabled;
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
    }
}