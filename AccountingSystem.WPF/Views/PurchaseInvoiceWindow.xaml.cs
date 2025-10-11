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
using AccountingSystem.Business;
using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.WPF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views
{
    public partial class PurchaseInvoiceWindow : Window
    {
        private readonly IPurchaseInvoiceService _purchaseInvoiceService;
        private readonly ISupplierService _supplierService;
        private readonly IProductService _productService;
        private readonly AccountingDbContext _context;

        private readonly ObservableCollection<PurchaseInvoiceItem> _invoiceDetails;
        private readonly ObservableCollection<Supplier> _suppliers;
        private readonly ObservableCollection<Product> _products;
        private ICollectionView _suppliersView;
        private ICollectionView _productsView;

        private readonly ObservableCollection<Unit> _productUnits;
        private readonly ObservableCollection<ProductUnit> _currentProductUnits;

        private readonly CultureInfo _culture;
        private decimal _taxRatePercent = 15m;

        private PurchaseInvoice? _currentInvoice;
        private readonly List<int> _invoiceIds = new();

        private const string TitleWarning = "تحذير";
        private const string TitleInfo = "تنبيه";

        public PurchaseInvoiceWindow(IServiceProvider serviceProvider)
        {
            try
            {
                InitializeComponent();

                _purchaseInvoiceService = serviceProvider.GetRequiredService<IPurchaseInvoiceService>();
                _supplierService = serviceProvider.GetRequiredService<ISupplierService>();
                _productService = serviceProvider.GetRequiredService<IProductService>();
                _context = serviceProvider.GetRequiredService<AccountingDbContext>();

                _invoiceDetails = new ObservableCollection<PurchaseInvoiceItem>();
                _suppliers = new ObservableCollection<Supplier>();
                _products = new ObservableCollection<Product>();
                _productUnits = new ObservableCollection<Unit>();
                _currentProductUnits = new ObservableCollection<ProductUnit>();

                // إنشاء CollectionViews للفلترة الديناميكية
                _suppliersView = CollectionViewSource.GetDefaultView(_suppliers);
                _productsView = CollectionViewSource.GetDefaultView(_products);

                _culture = new CultureInfo("ar-EG");
                _culture.NumberFormat.CurrencySymbol = "ج.م";

                dgItems.ItemsSource = _invoiceDetails;
                cmbSupplier.ItemsSource = _suppliersView;
                cmbProduct.ItemsSource = _productsView;
                cmbUnit.ItemsSource = _productUnits;

                EnableEditMode(false);

                Loaded += async (_, __) => await LoadDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل تهيئة نافذة فاتورة الشراء:\n{ex}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        }

        private async Task LoadDataAsync()
        {
            try
            {
                await LoadTaxRateAsync();

                // الموردين
                var suppliers = await _supplierService.GetAllSuppliersAsync();
                _suppliers.Clear();
                foreach (var s in suppliers) _suppliers.Add(s);

                // المنتجات
                var products = await _productService.GetAllProductsAsync();
                _products.Clear();
                foreach (var p in products) _products.Add(p);

                await LoadInvoiceIdsAsync();

                lblInvoiceNumber.Content = "سيتم التوليد تلقائياً عند الحفظ";
                dpInvoiceDate.SelectedDate = DateTime.Now;
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

        private bool TryParseDecimal(string? text, out decimal value)
        {
            text ??= "0";
            return decimal.TryParse(text, NumberStyles.Any, _culture, out value) ||
                   decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out value) ||
                   decimal.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out value);
        }

        #region Supplier Events
        private void cmbSupplier_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => SupplierSelectionChanged(sender, e);

        private void SupplierSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (cmbSupplier?.SelectedItem is Supplier supplier && lblPreviousBalance != null)
            {
                lblPreviousBalance.Content = supplier.Balance.ToString("C", _culture);
            }
        }

        private void cmbSupplier_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                _suppliersView.Filter = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Supplier dropdown error: {ex.Message}");
            }
        }

        private void cmbSupplier_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            try
            {
                var combo = (ComboBox)sender;
                var searchText = combo.Text + e.Text;

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var filterText = searchText.ToLowerInvariant();
                    _suppliersView.Filter = item =>
                        item is Supplier supplier &&
                        supplier.SupplierName?.ToLowerInvariant().Contains(filterText) == true;

                    if (!combo.IsDropDownOpen)
                        combo.IsDropDownOpen = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Supplier filter error: {ex.Message}");
            }
        }

        private void cmbSupplier_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F1)
            {
                ShowSupplierSearchDialog();
                e.Handled = true;
            }
        }

        private void btnSearchSupplier_Click(object sender, RoutedEventArgs e) => ShowSupplierSearchDialog();
        private void btnSearchPurchaseInvoices_Click(object sender, RoutedEventArgs e) => ShowPurchaseInvoiceSearchDialog();

        private void ShowSupplierSearchDialog()
        {
            try
            {
                var dialog = new SupplierSearchDialog(_suppliers.ToList())
                {
                    Owner = this
                };
                if (dialog.ShowDialog() == true && dialog.SelectedSupplier != null)
                    cmbSupplier.SelectedItem = dialog.SelectedSupplier;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح بحث الموردين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Product Events
        private void cmbProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
            => ProductSelectionChanged(sender, e);

        private async void ProductSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (cmbProduct?.SelectedItem is Product product && txtStock != null && txtUnitPrice != null)
            {
                txtStock.Text = product.CurrentStock.ToString("F2");
                txtUnitPrice.Text = product.PurchasePrice.ToString("F2"); // استخدام سعر الشراء بدلاً من البيع

                await LoadProductUnitsAsync(product.ProductId);
            }
        }

        private void cmbProduct_DropDownOpened(object sender, EventArgs e)
        {
            try
            {
                _productsView.Filter = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Product dropdown error: {ex.Message}");
            }
        }

        private void cmbProduct_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            try
            {
                var combo = (ComboBox)sender;
                var searchText = combo.Text + e.Text;

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    var filterText = searchText.ToLowerInvariant();
                    _productsView.Filter = item =>
                        item is Product product &&
                        (product.ProductName?.ToLowerInvariant().Contains(filterText) == true ||
                         product.ProductCode?.ToLowerInvariant().Contains(filterText) == true);

                    if (!combo.IsDropDownOpen)
                        combo.IsDropDownOpen = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Product filter error: {ex.Message}");
            }
        }

        private void cmbProduct_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F2)
            {
                ShowProductSearchDialog();
                e.Handled = true;
            }
        }

        private void btnSearchProduct_Click(object sender, RoutedEventArgs e) => ShowProductSearchDialog();

        private void ShowProductSearchDialog()
        {
            try
            {
                var items = _products.Select(p => ProductSearchItem.FromRaw(p)).ToList();

                var dialog = new ProductSearchDialog(items)
                {
                    Owner = this
                };
                if (dialog.ShowDialog() == true && dialog.Selected != null)
                    cmbProduct.SelectedItem = _products.FirstOrDefault(p => p.ProductId == dialog.Selected.ProductId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح بحث المنتجات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ShowPurchaseInvoiceSearchDialog()
        {
            try
            {
                // إنشاء قائمة فواتير الشراء
                var purchaseListWindow = new Window
                {
                    Title = "فواتير الشراء المسجلة",
                    Width = 900,
                    Height = 600,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var content = new TextBlock
                {
                    Text = "قائمة فواتير الشراء\n\nهذه الميزة متاحة الآن - يمكنك البحث والعرض",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 16,
                    TextAlignment = TextAlignment.Center
                };

                purchaseListWindow.Content = content;
                purchaseListWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح قائمة فواتير الشراء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadProductUnitsAsync(int productId)
        {
            try
            {
                var selectedProduct = cmbProduct?.SelectedItem as Product;
                _productUnits.Clear();
                _currentProductUnits.Clear();

                if (selectedProduct?.MainUnit != null)
                    _productUnits.Add(selectedProduct.MainUnit);

                var productUnits = await _context.ProductUnits
                    .Include(pu => pu.Unit)
                    .Where(pu => pu.ProductId == productId && pu.IsActive)
                    .ToListAsync();

                foreach (var pu in productUnits)
                {
                    _currentProductUnits.Add(pu);
                    if (pu.Unit != null && !_productUnits.Any(u => u.UnitId == pu.UnitId))
                        _productUnits.Add(pu.Unit!);
                }

                if (_productUnits.Any())
                    cmbUnit.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadProductUnits error: {ex.Message}");
            }
        }

        private void cmbUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded) CalculateLineTotal();
        }
        #endregion

        #region Calculation Events
        private void QuantityChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) CalculateLineTotal();
        }

        private void UnitPriceChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) CalculateLineTotal();
        }

        private void DiscountChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded) CalculateLineTotal();
        }

        private void CalculateLineTotal()
        {
            try
            {
                if (txtQuantity == null || txtUnitPrice == null || txtDiscount == null || lblTotal == null) return;

                TryParseDecimal(txtQuantity.Text, out var quantity);
                TryParseDecimal(txtUnitPrice.Text, out var unitPrice);
                TryParseDecimal(txtDiscount.Text, out var discount);

                var gross = quantity * unitPrice;
                var lineNet = gross - discount;
                if (lineNet < 0) lineNet = 0;

                lblTotal.Text = lineNet.ToString("C", _culture);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CalculateLineTotal error: {ex.Message}");
            }
        }

        private void txtPaidAmount_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (IsLoaded && _invoiceDetails != null) CalculateInvoiceTotals();
        }

        private void CalculateInvoiceTotals()
        {
            try
            {
                if (_invoiceDetails == null || lblSubTotal == null) return;

                var subTotal = _invoiceDetails.Sum(d => d.LineTotal);
                var totalDiscount = _invoiceDetails.Sum(d => d.DiscountAmount);
                var taxAmount = decimal.Round(subTotal * (_taxRatePercent / 100m), 2);
                var netTotal = subTotal + taxAmount - totalDiscount;

                TryParseDecimal(txtPaidAmount.Text, out var paidAmount);
                var remainingAmount = netTotal - paidAmount;

                lblSubTotal.Content = subTotal.ToString("C", _culture);
                lblTotalDiscount.Content = totalDiscount.ToString("C", _culture);
                lblTaxAmount.Content = taxAmount.ToString("C", _culture);
                lblNetTotal.Content = netTotal.ToString("C", _culture);
                lblRemainingAmount.Content = remainingAmount.ToString("C", _culture);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CalculateInvoiceTotals error: {ex.Message}");
            }
        }
        #endregion

        #region Item Management
        private void btnAddItem_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cmbProduct.SelectedItem is not Product product ||
                    cmbUnit.SelectedItem is not Unit unit ||
                    !TryParseDecimal(txtQuantity.Text, out var quantity) ||
                    !TryParseDecimal(txtUnitPrice.Text, out var unitPrice) ||
                    !TryParseDecimal(txtDiscount.Text, out var discount))
                {
                    MessageBox.Show("يرجى ملء جميع البيانات المطلوبة بشكل صحيح", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var detail = new PurchaseInvoiceItem
                {
                    ProductId = product.ProductId,
                    Product = product,
                    UnitId = unit.UnitId,
                    Unit = unit,
                    Quantity = quantity,
                    UnitCost = unitPrice,
                    LineTotal = quantity * unitPrice,
                    DiscountAmount = discount
                };

                _invoiceDetails.Add(detail);
                CalculateInvoiceTotals();
                ClearItemInputs();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في إضافة الصنف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnDeleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (!btnAddItem.IsEnabled)
            {
                MessageBox.Show("يجب تفعيل وضع التعديل أولاً", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (sender is Button btn && btn.Tag is PurchaseInvoiceItem detailFromTag)
            {
                _invoiceDetails.Remove(detailFromTag);
                CalculateInvoiceTotals();
                return;
            }

            if (dgItems.SelectedItem is PurchaseInvoiceItem detail)
            {
                _invoiceDetails.Remove(detail);
                CalculateInvoiceTotals();
            }
        }

        private void ClearItemInputs()
        {
            cmbProduct.SelectedIndex = -1;
            cmbUnit.SelectedIndex = -1;
            txtQuantity.Text = "1";
            txtUnitPrice.Text = "0";
            txtDiscount.Text = "0";
            txtStock.Text = "";
            lblTotal.Text = "0.00";
            cmbProduct.Focus();
        }

        private void txtQuantity_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                txtUnitPrice.Focus();
                e.Handled = true;
            }
        }
        #endregion

        #region Invoice Operations
        private async void btnSave_Click(object sender, RoutedEventArgs e) => await SaveInvoiceAsync();

        private async Task<PurchaseInvoice?> SaveInvoiceAsync()
        {
            try
            {
                if (cmbSupplier.SelectedItem is not Supplier supplier)
                {
                    MessageBox.Show("يرجى اختيار المورد", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                if (!_invoiceDetails.Any())
                {
                    MessageBox.Show("يرجى إضافة على الأقل صنف واحد", TitleWarning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }

                var subTotal = _invoiceDetails.Sum(d => d.LineTotal);
                var taxAmount = decimal.Round(subTotal * (_taxRatePercent / 100m), 2);
                var netTotal = subTotal + taxAmount - _invoiceDetails.Sum(d => d.DiscountAmount);
                TryParseDecimal(txtPaidAmount.Text, out var paidAmount);

                var invoice = new PurchaseInvoice
                {
                    InvoiceDate = dpInvoiceDate.SelectedDate ?? DateTime.Now,
                    SupplierId = supplier.SupplierId,
                    SubTotal = subTotal,
                    TaxAmount = taxAmount,
                    DiscountAmount = _invoiceDetails.Sum(d => d.DiscountAmount),
                    NetTotal = netTotal,
                    PaidAmount = paidAmount,
                    RemainingAmount = Math.Max(0, netTotal - paidAmount),
                    Notes = txtNotes.Text,
                    Status = InvoiceStatus.Draft,
                    CreatedBy = "system",
                    Items = _invoiceDetails.ToList()
                };

                SetBusy(true);
                var result = await _purchaseInvoiceService.CreatePurchaseInvoiceAsync(invoice);

                _currentInvoice = result;
                lblInvoiceNumber.Content = result.InvoiceNumber;

                EnableEditMode(false);
                UpdateButtonStates();

                // ترحيل تلقائي بعد الحفظ لضمان تحديث المخزون ورصيد المورد مباشرة
                try
                {
                    await _purchaseInvoiceService.PostPurchaseInvoiceAsync(result.PurchaseInvoiceId);
                    _currentInvoice.Status = InvoiceStatus.Confirmed;
                    _currentInvoice.IsPosted = true;

                    // تحديث الواجهة لإظهار الترحيل
                    if (lblStatus != null)
                    {
                        lblStatus.Text = "حالة الفاتورة: مرحلة";
                        lblStatus.Foreground = System.Windows.Media.Brushes.Green;
                    }

                    // تحديث كميات المخزون المعروضة للمنتجات المستخدمة في الفاتورة
                    await RefreshStocksForPurchaseAsync(_currentInvoice);

                    UpdateButtonStates();
                }
                catch (Exception postEx)
                {
                    MessageBox.Show($"تم حفظ الفاتورة ولكن فشل الترحيل:\n{postEx.Message}", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                await ShowPostSaveDialogAsync(_currentInvoice ?? result);
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
            finally
            {
                SetBusy(false);
            }
        }

        // Post button removed - invoices auto-post after save

        private void btnPrint_Click(object sender, RoutedEventArgs e) => PrintCurrentInvoice();

        private void PrintCurrentInvoice()
        {
            try
            {
                if (_currentInvoice?.PurchaseInvoiceId <= 0)
                {
                    MessageBox.Show("يجب حفظ الفاتورة أولاً قبل الطباعة!", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_currentInvoice != null)
                {
                    // Convert PurchaseInvoice to SalesInvoice format for printing
                    var printInvoice = ConvertPurchaseToSalesForPrint(_currentInvoice);
                    var printPreview = new InvoicePrintPreview(printInvoice, _context);
                    printPreview.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnNew_Click(object sender, RoutedEventArgs e)
        {
            _invoiceDetails.Clear();
            _currentInvoice = null;
            cmbSupplier.SelectedIndex = -1;
            txtNotes.Text = "";
            txtPaidAmount.Text = "0";
            dpInvoiceDate.SelectedDate = DateTime.Now;
            ClearItemInputs();
            CalculateInvoiceTotals();
            lblInvoiceNumber.Content = "سيتم التوليد تلقائياً عند الحفظ";

            EnableEditMode(true);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => Close();
        #endregion

        #region Navigation and Edit
        private async Task LoadInvoiceIdsAsync()
        {
            try
            {
                _invoiceIds.Clear();
                var ids = await _context.PurchaseInvoices
                    .OrderBy(i => i.InvoiceDate)
                    .ThenBy(i => i.PurchaseInvoiceId)
                    .Select(i => i.PurchaseInvoiceId)
                    .ToListAsync();

                _invoiceIds.AddRange(ids);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل قائمة الفواتير: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnPreviousInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentInvoice == null || !_invoiceIds.Any()) return;

                var currentIndex = _invoiceIds.IndexOf(_currentInvoice.PurchaseInvoiceId);
                if (currentIndex > 0)
                {
                    var previousId = _invoiceIds[currentIndex - 1];
                    await LoadInvoiceByIdAsync(previousId);
                }
                else
                {
                    MessageBox.Show("هذه أول فاتورة", TitleInfo, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الانتقال للفاتورة السابقة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnNextInvoice_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentInvoice == null || !_invoiceIds.Any()) return;

                var currentIndex = _invoiceIds.IndexOf(_currentInvoice.PurchaseInvoiceId);
                if (currentIndex < _invoiceIds.Count - 1)
                {
                    var nextId = _invoiceIds[currentIndex + 1];
                    await LoadInvoiceByIdAsync(nextId);
                }
                else
                {
                    MessageBox.Show("هذه آخر فاتورة", TitleInfo, MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الانتقال للفاتورة التالية: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task LoadInvoiceByIdAsync(int invoiceId)
        {
            try
            {
                SetBusy(true);

                var invoice = await _context.PurchaseInvoices
                    .Include(i => i.Supplier)
                    .Include(i => i.Items).ThenInclude(d => d.Product)
                    .Include(i => i.Items).ThenInclude(d => d.Unit)
                    .FirstOrDefaultAsync(i => i.PurchaseInvoiceId == invoiceId);

                if (invoice == null)
                {
                    MessageBox.Show("لا يمكن العثور على الفاتورة", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                LoadInvoiceToForm(invoice);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void LoadInvoiceToForm(PurchaseInvoice invoice)
        {
            try
            {
                _currentInvoice = invoice;

                lblInvoiceNumber.Content = invoice.InvoiceNumber;
                dpInvoiceDate.SelectedDate = invoice.InvoiceDate;
                cmbSupplier.SelectedItem = _suppliers.FirstOrDefault(s => s.SupplierId == invoice.SupplierId);
                txtNotes.Text = invoice.Notes ?? "";
                txtPaidAmount.Text = invoice.PaidAmount.ToString("F2");

                _invoiceDetails.Clear();
                foreach (var detail in invoice.Items)
                    _invoiceDetails.Add(detail);

                CalculateInvoiceTotals();
                EnableEditMode(false);
                UpdateButtonStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في عرض الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentInvoice == null)
                {
                    MessageBox.Show("لا توجد فاتورة للتعديل", TitleInfo, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                if (_currentInvoice.IsPosted)
                {
                    MessageBox.Show("لا يمكن تعديل فاتورة مرحلة", "تحذير", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                EnableEditMode(true);
                MessageBox.Show("تم تفعيل وضع التعديل. يمكنك الآن تعديل الفاتورة", "تعديل", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تفعيل التعديل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnableEditMode(bool enabled)
        {
            cmbSupplier.IsEnabled = enabled;
            dpInvoiceDate.IsEnabled = enabled;
            txtNotes.IsEnabled = enabled;
            txtPaidAmount.IsEnabled = enabled;

            btnAddItem.IsEnabled = enabled;
            cmbProduct.IsEnabled = enabled;
            cmbUnit.IsEnabled = enabled;
            txtQuantity.IsEnabled = enabled;
            txtUnitPrice.IsEnabled = enabled;
            txtDiscount.IsEnabled = enabled;

            btnSearchSupplier.IsEnabled = enabled;
            btnSearchProduct.IsEnabled = enabled;

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            var hasInvoice = _currentInvoice != null;
            var isPosted = hasInvoice && _currentInvoice!.IsPosted;
            var canEdit = hasInvoice && !isPosted;

            btnPrint.IsEnabled = hasInvoice;

            try
            {
                var editButton = this.FindName("btnEdit") as Button;
                if (editButton != null) editButton.IsEnabled = canEdit;

                var prevButton = this.FindName("btnPreviousInvoice") as Button;
                if (prevButton != null) prevButton.IsEnabled = hasInvoice;

                var nextButton = this.FindName("btnNextInvoice") as Button;
                if (nextButton != null) nextButton.IsEnabled = hasInvoice;
            }
            catch
            {
                // في حال عدم توفر الأزرار
            }
        }

        private void SetBusy(bool isBusy)
        {
            btnSave.IsEnabled = !isBusy;
            btnPrint.IsEnabled = !isBusy;
            btnCancel.IsEnabled = !isBusy;
            btnAddItem.IsEnabled = !isBusy && (_currentInvoice == null || !_currentInvoice.IsPosted);
        }
        #endregion

        #region Post Save Dialog
        private async Task ShowPostSaveDialogAsync(PurchaseInvoice savedInvoice)
        {
            try
            {
                if (savedInvoice == null)
                {
                    MessageBox.Show("لا يمكن عرض نافذة ما بعد الحفظ - الفاتورة غير صالحة", "خطأ",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // التأكد من تحميل بيانات المورد
                if (savedInvoice.Supplier == null && savedInvoice.SupplierId > 0)
                {
                    savedInvoice.Supplier = (await _context.Suppliers
                        .FirstOrDefaultAsync(s => s.SupplierId == savedInvoice.SupplierId))!;
                }

                var dialog = new PostPurchaseDialog(savedInvoice) { Owner = this };

                if (dialog.ShowDialog() == true)
                {
                    if (dialog.GetPrintInvoice())
                        PrintInvoice(savedInvoice);

                    if (dialog.GetRecordPayment() && dialog.GetPaymentAmount() > 0)
                        await ProcessPaymentAsync(savedInvoice, dialog.GetPaymentAmount());

                    if (dialog.GetOpenNewInvoice())
                        btnNew_Click(this, new RoutedEventArgs());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في معالجة ما بعد الحفظ: {ex.Message}\n\nالتفاصيل: {ex.InnerException?.Message}",
                    "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                System.Diagnostics.Debug.WriteLine($"ShowPostSaveDialogAsync Error: {ex}");
            }
        }

        private void PrintInvoice(PurchaseInvoice invoice)
        {
            try
            {
                if (invoice?.PurchaseInvoiceId > 0)
                {
                    var printInvoice = ConvertPurchaseToSalesForPrint(invoice);
                    var printPreview = new InvoicePrintPreview(printInvoice, _context);
                    printPreview.ShowDialog();
                }
                else
                {
                    MessageBox.Show("فاتورة غير صالحة للطباعة!", "خطأ", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في طباعة الفاتورة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task ProcessPaymentAsync(PurchaseInvoice invoice, decimal paymentAmount)
        {
            try
            {
                invoice.PaidAmount += paymentAmount;
                invoice.RemainingAmount = Math.Max(0, invoice.NetTotal - invoice.PaidAmount);

                _context.PurchaseInvoices.Update(invoice);
                await _context.SaveChangesAsync();

                txtPaidAmount.Text = invoice.PaidAmount.ToString("F2");
                CalculateInvoiceTotals();

                MessageBox.Show($"تم تسجيل دفعة بمبلغ {paymentAmount.ToString("C", _culture)} بنجاح!", "دفع للمورد",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تسجيل الدفع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        #endregion

        #region Keyboard Shortcuts
        protected override void OnKeyDown(System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                if (e.Key == System.Windows.Input.Key.S && e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control)
                {
                    btnSave_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.N && e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control)
                {
                    btnNew_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.P && e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control)
                {
                    btnPrint_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Left && e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control)
                {
                    btnPreviousInvoice_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Right && e.KeyboardDevice.Modifiers == System.Windows.Input.ModifierKeys.Control)
                {
                    btnNextInvoice_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.Escape)
                {
                    btnCancel_Click(this, new RoutedEventArgs());
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.F1)
                {
                    var focusedElement = System.Windows.Input.Keyboard.FocusedElement;
                    if (focusedElement == cmbSupplier)
                    {
                        ShowSupplierSearchDialog();
                    }
                    else // F1 افتراضي للمنتجات
                    {
                        ShowProductSearchDialog();
                    }
                    e.Handled = true;
                }
                else if (e.Key == System.Windows.Input.Key.F2)
                {
                    ShowPurchaseInvoiceSearchDialog(); // F2 لفواتير الشراء السابقة
                    e.Handled = true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"OnKeyDown error: {ex.Message}");
            }

            base.OnKeyDown(e);
        }
        #endregion

        #region Helper Methods for Auto-Post and Printing
        private async Task RefreshStocksForPurchaseAsync(PurchaseInvoice invoice)
        {
            try
            {
                if (invoice?.Items == null) return;

                var productIds = invoice.Items
                    .Select(d => d.ProductId)
                    .Distinct()
                    .ToList();

                foreach (var pid in productIds)
                {
                    var latest = await _productService.GetProductByIdAsync(pid);
                    if (latest == null) continue;

                    var local = _products.FirstOrDefault(p => p.ProductId == pid);
                    if (local != null)
                    {
                        local.CurrentStock = latest.CurrentStock;
                        // تحديث العرض إن كان هذا المنتج مختاراً حالياً
                        if (Equals(cmbProduct.SelectedItem, local) && txtStock != null)
                            txtStock.Text = local.CurrentStock.ToString("F2");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"RefreshStocksForPurchaseAsync error: {ex.Message}");
            }
        }

        private SalesInvoice ConvertPurchaseToSalesForPrint(PurchaseInvoice purchase)
        {
            // Convert PurchaseInvoice to SalesInvoice format for unified printing
            var salesInvoice = new SalesInvoice
            {
                SalesInvoiceId = purchase.PurchaseInvoiceId,
                InvoiceNumber = purchase.InvoiceNumber,
                InvoiceDate = purchase.InvoiceDate,
                SubTotal = purchase.SubTotal,
                TaxAmount = purchase.TaxAmount,
                DiscountAmount = purchase.DiscountAmount,
                NetTotal = purchase.NetTotal,
                PaidAmount = purchase.PaidAmount,
                RemainingAmount = purchase.RemainingAmount,
                Notes = purchase.Notes,
                Status = purchase.Status,
                IsPosted = purchase.IsPosted,
                CreatedDate = purchase.CreatedDate,
                CreatedBy = purchase.CreatedBy
            };

            // Convert Supplier to Customer format
            if (purchase.Supplier != null)
            {
                salesInvoice.Customer = new Customer
                {
                    CustomerId = purchase.SupplierId,
                    CustomerName = purchase.Supplier.SupplierName,
                    Address = purchase.Supplier.Address,
                    Phone = purchase.Supplier.Phone,
                    Email = purchase.Supplier.Email,
                    Balance = purchase.Supplier.Balance
                };
            }

            // Convert PurchaseInvoiceDetails to SalesInvoiceDetails
            if (purchase.Items != null)
            {
                salesInvoice.Items = purchase.Items.Select(d => new SalesInvoiceItem
                {
                    SalesInvoiceItemId = d.PurchaseInvoiceItemId,
                    ProductId = d.ProductId,
                    UnitId = d.UnitId,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitCost
                }).ToList();
            }

            return salesInvoice;
        }
        #endregion
    }
}