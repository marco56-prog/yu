using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Threading;
using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.WPF.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.WPF.Views
{
    public partial class ReturnsManagementWindow : Window, INotifyPropertyChanged
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly DispatcherTimer _timer;
        
        private SalesInvoice _currentInvoice = new();
        private ObservableCollection<ReturnInvoiceItem> _invoiceItems = new();
        private ObservableCollection<ReturnRecord> _returnsHistory = new();

        // خصائص ربط البيانات
        public ObservableCollection<ReturnInvoiceItem> InvoiceItems
        {
            get => _invoiceItems;
            set
            {
                _invoiceItems = value;
                OnPropertyChanged(nameof(InvoiceItems));
            }
        }

        public ObservableCollection<ReturnRecord> ReturnsHistory
        {
            get => _returnsHistory;
            set
            {
                _returnsHistory = value;
                OnPropertyChanged(nameof(ReturnsHistory));
            }
        }

        public ReturnsManagementWindow(IUnitOfWork unitOfWork)
        {
            InitializeComponent();
            
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            
            DataContext = this;
            
            // إعداد المجموعات
            InvoiceItems = new ObservableCollection<ReturnInvoiceItem>();
            ReturnsHistory = new ObservableCollection<ReturnRecord>();
            
            // ربط البيانات
            dgInvoiceItems.ItemsSource = InvoiceItems;
            dgReturnsHistory.ItemsSource = ReturnsHistory;
            
            // إعداد المؤقت
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
            
            Loaded += async (s, e) => await LoadInitialDataAsync();
        }

        private async Task LoadInitialDataAsync()
        {
            try
            {
                lblCurrentDate.Text = DateTime.Now.ToString("yyyy/MM/dd");
                lblCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
                
                // تحميل سجل المرتجعات
                await LoadReturnsHistoryAsync();
                
                // تحديث الإحصائيات
                await UpdateStatisticsAsync();
                
                lblStatusMessage.Text = "تم تحميل البيانات بنجاح";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل البيانات: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatusMessage.Text = "خطأ في تحميل البيانات";
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            lblCurrentTime.Text = DateTime.Now.ToString("HH:mm:ss");
        }

        #region البحث عن الفواتير

        private async void btnSearchInvoice_Click(object sender, RoutedEventArgs e)
        {
            await SearchInvoiceAsync();
        }

        private async Task SearchInvoiceAsync()
        {
            try
            {
                var invoiceNumber = txtInvoiceNumber.Text?.Trim();
                
                if (string.IsNullOrEmpty(invoiceNumber))
                {
                    MessageBox.Show("يرجى إدخال رقم الفاتورة", "تنبيه", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                lblStatusMessage.Text = "جارٍ البحث عن الفاتورة...";
                
                // البحث عن الفاتورة
                var invoices = await _unitOfWork.Repository<SalesInvoice>().GetAllAsync();
                _currentInvoice = invoices.FirstOrDefault(i => i.InvoiceNumber == invoiceNumber) ?? new SalesInvoice();

                if (_currentInvoice == null)
                {
                    lblInvoiceStatus.Text = "لم يتم العثور على الفاتورة";
                    lblInvoiceStatus.Foreground = System.Windows.Media.Brushes.Red;
                    InvoiceInfoPanel.Visibility = Visibility.Collapsed;
                    InvoiceItems.Clear();
                    lblStatusMessage.Text = "لم يتم العثور على الفاتورة";
                    return;
                }

                // التحقق من إمكانية المرتجع
                if (_currentInvoice.InvoiceDate < DateTime.Now.AddDays(-30))
                {
                    MessageBox.Show("لا يمكن إرجاع هذه الفاتورة (انتهت فترة الإرجاع - 30 يوم)", 
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // عرض معلومات الفاتورة
                await DisplayInvoiceInfoAsync();
                
                lblStatusMessage.Text = "تم العثور على الفاتورة بنجاح";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث عن الفاتورة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatusMessage.Text = "خطأ في البحث عن الفاتورة";
            }
        }

        private async Task DisplayInvoiceInfoAsync()
        {
            try
            {
                // عرض معلومات الفاتورة
                lblFoundInvoiceNumber.Text = _currentInvoice.InvoiceNumber;
                lblInvoiceDate.Text = _currentInvoice.InvoiceDate.ToString("yyyy/MM/dd");
                lblCustomerName.Text = _currentInvoice.Customer?.CustomerName ?? "عميل نقدي";
                lblInvoiceTotal.Text = $"{_currentInvoice.NetTotal:N2} ج.م";
                
                lblInvoiceStatus.Text = "موجودة ويمكن إرجاعها";
                lblInvoiceStatus.Foreground = System.Windows.Media.Brushes.Green;
                
                InvoiceInfoPanel.Visibility = Visibility.Visible;

                // تحضير عناصر الفاتورة للإرجاع
                InvoiceItems.Clear();
                
                // محاكاة عناصر الفاتورة - يجب تحميلها من قاعدة البيانات
                await LoadInvoiceItemsAsync(_currentInvoice);

                if (!InvoiceItems.Any())
                {
                    MessageBox.Show("جميع عناصر هذه الفاتورة تم إرجاعها مسبقاً", 
                        "تنبيه", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في عرض معلومات الفاتورة: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    private async Task LoadInvoiceItemsAsync(SalesInvoice invoice)
        {
            try
            {
        _ = invoice; // mark parameter as used
                // محاكاة عناصر الفاتورة - في التطبيق الحقيقي يجب تحميلها من قاعدة البيانات
                await Task.Delay(100);
                
                // إضافة عناصر تجريبية
                for (int i = 1; i <= 3; i++)
                {
                    var previousReturns = await GetPreviousReturnsForItemAsync(i);
                    var quantity = 5m;
                    var availableQuantity = quantity - previousReturns;
                    
                    if (availableQuantity > 0)
                    {
                        InvoiceItems.Add(new ReturnInvoiceItem
                        {
                            InvoiceItemId = i,
                            ProductId = i,
                            ProductName = $"منتج {i}",
                            ProductBarcode = $"PRD{i:000}",
                            Unit = "قطعة",
                            OriginalQuantity = quantity,
                            AvailableQuantity = availableQuantity,
                            ReturnQuantity = 0,
                            UnitPrice = 25.00m * i,
                            ReturnTotal = 0,
                            ReturnReason = "",
                            IsSelected = false
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحميل عناصر الفاتورة: {ex.Message}");
            }
        }

        private async Task<decimal> GetPreviousReturnsForItemAsync(int invoiceItemId)
        {
            try
            {
                // هذا يحتاج إلى تنفيذ جدول المرتجعات في قاعدة البيانات
                // مؤقتاً سنرجع 0
                await Task.Delay(1);
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region معالجة المرتجعات

        private async void btnProcessReturn_Click(object sender, RoutedEventArgs e)
        {
            await ProcessReturnAsync();
        }

        private async Task ProcessReturnAsync()
        {
            try
            {
                if (_currentInvoice == null)
                {
                    MessageBox.Show("يرجى البحث عن فاتورة أولاً", "تنبيه", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var selectedItems = InvoiceItems.Where(i => i.IsSelected && i.ReturnQuantity > 0).ToList();
                
                if (!selectedItems.Any())
                {
                    MessageBox.Show("يرجى اختيار عناصر للإرجاع وتحديد الكمية", "تنبيه", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // التحقق من الكميات
                var invalidItems = selectedItems.Where(i => i.ReturnQuantity > i.AvailableQuantity).ToList();
                if (invalidItems.Any())
                {
                    MessageBox.Show($"كمية الإرجاع أكبر من المتاح للمنتج: {invalidItems.First().ProductName}", 
                        "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // التحقق من أسباب الإرجاع
                var itemsWithoutReason = selectedItems.Where(i => string.IsNullOrEmpty(i.ReturnReason)).ToList();
                if (itemsWithoutReason.Any())
                {
                    MessageBox.Show("يرجى تحديد سبب الإرجاع لجميع العناصر المحددة", "تنبيه", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // تأكيد العملية
                var totalReturnValue = selectedItems.Sum(i => i.ReturnTotal);
                var confirmResult = MessageBox.Show(
                    $"هل تريد تأكيد إرجاع {selectedItems.Count} عنصر بقيمة {totalReturnValue:N2} ج.م؟",
                    "تأكيد المرتجع", MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (confirmResult != MessageBoxResult.Yes)
                    return;

                lblStatusMessage.Text = "جارٍ معالجة المرتجع...";

                // إنشاء سجل المرتجع
                var returnRecord = new ReturnRecord
                {
                    ReturnNumber = await GenerateReturnNumberAsync(),
                    InvoiceId = _currentInvoice.SalesInvoiceId,
                    InvoiceNumber = _currentInvoice.InvoiceNumber,
                    CustomerId = _currentInvoice.CustomerId,
                    CustomerName = _currentInvoice.Customer?.CustomerName ?? "عميل نقدي",
                    ReturnDate = DateTime.Now,
                    ReturnAmount = totalReturnValue,
                    ItemsCount = selectedItems.Count,
                    Status = "مكتمل",
                    ProcessedBy = "admin", // يمكن تحسينها لاحقاً
                    Notes = txtReturnNotes.Text?.Trim() ?? ""
                };

                // إضافة السجل إلى القائمة (يجب حفظه في قاعدة البيانات)
                ReturnsHistory.Insert(0, returnRecord);

                // تحديث المخزون
                await UpdateInventoryForReturnAsync(selectedItems);

                // إنشاء إشعار صندوق النقدية (إذا كان الدفع نقدياً)
                await ProcessCashRefundAsync(returnRecord);

                // إعادة تعيين النموذج
                ResetReturnForm();

                // تحديث الإحصائيات
                await UpdateStatisticsAsync();

                MessageBox.Show($"تم تنفيذ المرتجع بنجاح\nرقم المرتجع: {returnRecord.ReturnNumber}", 
                    "نجحت العملية", MessageBoxButton.OK, MessageBoxImage.Information);

                lblStatusMessage.Text = "تم تنفيذ المرتجع بنجاح";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في معالجة المرتجع: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatusMessage.Text = "خطأ في معالجة المرتجع";
            }
        }

        private async Task<string> GenerateReturnNumberAsync()
        {
            await Task.Delay(1);
            return $"RET{DateTime.Now:yyyyMMddHHmmss}";
        }

        private async Task UpdateInventoryForReturnAsync(List<ReturnInvoiceItem> returnItems)
        {
            try
            {
                foreach (var returnItem in returnItems)
                {
                    var product = await _unitOfWork.Repository<Product>().GetByIdAsync(returnItem.ProductId);
                    if (product != null)
                    {
                        // إعادة الكمية للمخزون
                        product.CurrentStock += returnItem.ReturnQuantity;
                        _unitOfWork.Repository<Product>().Update(product);
                    }
                }

                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"خطأ في تحديث المخزون: {ex.Message}");
            }
        }

        private async Task ProcessCashRefundAsync(ReturnRecord returnRecord)
        {
            try
            {
                // إنشاء إدخال في صندوق النقدية للاسترداد
                var cashEntry = new CashTransaction
                {
                    TransactionNumber = $"RTN-{DateTime.Now:yyyyMMddHHmmss}",
                    CashBoxId = 1, // Default cash box ID
                    TransactionDate = DateTime.Now,
                    TransactionType = TransactionType.Expense,
                    Amount = returnRecord.ReturnAmount,
                    Description = $"استرداد نقدي - مرتجع رقم {returnRecord.ReturnNumber}",
                    ReferenceType = "RETURN",
                    ReferenceId = 1, // Dummy ID for now
                    CreatedBy = 1, // Default user ID
                    CashBox = new CashBox { CashBoxName = "Default", CurrentBalance = 0 }
                };

                await _unitOfWork.Repository<CashTransaction>().AddAsync(cashEntry);
                await _unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // تسجيل الخطأ ولكن لا نوقف العملية
                System.Diagnostics.Debug.WriteLine($"خطأ في معالجة الاسترداد النقدي: {ex.Message}");
            }
        }

        private void ResetReturnForm()
        {
            _currentInvoice = new SalesInvoice();
            txtInvoiceNumber.Text = "";
            txtReturnNotes.Text = "";
            lblReturnTotal.Text = "0.00 ج.م";
            
            InvoiceInfoPanel.Visibility = Visibility.Collapsed;
            InvoiceItems.Clear();
            
            lblInvoiceStatus.Text = "لم يتم البحث";
            lblInvoiceStatus.Foreground = System.Windows.Media.Brushes.Gray;
        }

        private void btnClearReturn_Click(object sender, RoutedEventArgs e)
        {
            ResetReturnForm();
            lblStatusMessage.Text = "تم إعادة تعيين النموذج";
        }

        #endregion

        #region سجل المرتجعات

        private async Task LoadReturnsHistoryAsync()
        {
            try
            {
                ReturnsHistory.Clear();
                
                // مؤقتاً - تحميل بيانات تجريبية
                var sampleReturns = GenerateSampleReturns();
                
                foreach (var returnRecord in sampleReturns)
                {
                    ReturnsHistory.Add(returnRecord);
                }

                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل سجل المرتجعات: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<ReturnRecord> GenerateSampleReturns()
        {
            return new List<ReturnRecord>
            {
                new ReturnRecord
                {
                    ReturnNumber = "RET20241201001",
                    InvoiceNumber = "INV-2024-001",
                    ReturnDate = DateTime.Now.AddDays(-1),
                    CustomerName = "أحمد محمد علي",
                    ItemsCount = 2,
                    ReturnAmount = 150.00m,
                    Status = "مكتمل",
                    ProcessedBy = "admin",
                    Notes = "إرجاع بسبب عيب في المنتج"
                },
                new ReturnRecord
                {
                    ReturnNumber = "RET20241201002",
                    InvoiceNumber = "INV-2024-002",
                    ReturnDate = DateTime.Now.AddDays(-2),
                    CustomerName = "سارة أحمد محمود",
                    ItemsCount = 1,
                    ReturnAmount = 75.50m,
                    Status = "مكتمل",
                    ProcessedBy = "admin",
                    Notes = "طلب استرداد"
                },
                new ReturnRecord
                {
                    ReturnNumber = "RET20241201003",
                    InvoiceNumber = "INV-2024-003",
                    ReturnDate = DateTime.Now.AddDays(-3),
                    CustomerName = "محمد سعد الدين",
                    ItemsCount = 3,
                    ReturnAmount = 220.00m,
                    Status = "ملغى",
                    ProcessedBy = "admin",
                    Notes = "تم إلغاء الطلب"
                }
            };
        }

        private async void btnSearchReturns_Click(object sender, RoutedEventArgs e)
        {
            await SearchReturnsAsync();
        }

        private async Task SearchReturnsAsync()
        {
            try
            {
                lblStatusMessage.Text = "جارٍ البحث في المرتجعات...";
                
                // تطبيق المرشحات
                var filteredReturns = ReturnsHistory.AsEnumerable();

                // فلترة التاريخ
                if (dpFromDate.SelectedDate.HasValue)
                {
                    filteredReturns = filteredReturns.Where(r => r.ReturnDate >= dpFromDate.SelectedDate.Value);
                }

                if (dpToDate.SelectedDate.HasValue)
                {
                    filteredReturns = filteredReturns.Where(r => r.ReturnDate <= dpToDate.SelectedDate.Value.AddDays(1));
                }

                // فلترة النص
                var searchText = txtSearchReturns.Text?.Trim();
                if (!string.IsNullOrEmpty(searchText))
                {
                    filteredReturns = filteredReturns.Where(r => 
                        r.InvoiceNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        r.CustomerName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                        r.ReturnNumber.Contains(searchText, StringComparison.OrdinalIgnoreCase));
                }

                // تطبيق النتائج
                var results = filteredReturns.ToList();
                
                // تحديث الجدول
                dgReturnsHistory.ItemsSource = null;
                dgReturnsHistory.ItemsSource = results;

                lblStatusMessage.Text = $"تم العثور على {results.Count} مرتجع";
                
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في البحث: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatusMessage.Text = "خطأ في البحث";
            }
        }

        private void btnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            dpFromDate.SelectedDate = null;
            dpToDate.SelectedDate = null;
            txtSearchReturns.Text = "";
            
            dgReturnsHistory.ItemsSource = ReturnsHistory;
            lblStatusMessage.Text = "تم مسح المرشحات";
        }

        private void btnViewReturn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ReturnRecord returnRecord)
            {
                var details = $"رقم المرتجع: {returnRecord.ReturnNumber}\n" +
                             $"رقم الفاتورة: {returnRecord.InvoiceNumber}\n" +
                             $"التاريخ: {returnRecord.ReturnDate:yyyy/MM/dd HH:mm}\n" +
                             $"العميل: {returnRecord.CustomerName}\n" +
                             $"عدد الأصناف: {returnRecord.ItemsCount}\n" +
                             $"قيمة المرتجع: {returnRecord.ReturnAmount:N2} ج.م\n" +
                             $"الحالة: {returnRecord.Status}\n" +
                             $"المعالج: {returnRecord.ProcessedBy}";

                if (!string.IsNullOrEmpty(returnRecord.Notes))
                {
                    details += $"\n\nملاحظات:\n{returnRecord.Notes}";
                }

                MessageBox.Show(details, "تفاصيل المرتجع", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void btnPrintReturn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is ReturnRecord returnRecord)
            {
                try
                {
                    var printDialog = new PrintDialog();
                    if (printDialog.ShowDialog() == true)
                    {
                        var doc = new FlowDocument
                        {
                            PageWidth = 600,
                            PagePadding = new Thickness(40),
                            FontFamily = new System.Windows.Media.FontFamily("Arial"),
                            FontSize = 12
                        };

                        void AddPara(string text, bool bold = false)
                        {
                            var p = new System.Windows.Documents.Paragraph();
                            var run = new Run(text);
                            if (bold) run.FontWeight = FontWeights.Bold;
                            p.Inlines.Add(run);
                            doc.Blocks.Add(p);
                        }

                        AddPara($"مرتجع رقم: {returnRecord.ReturnNumber}", true);
                        AddPara($"رقم الفاتورة: {returnRecord.InvoiceNumber}");
                        AddPara($"التاريخ: {returnRecord.ReturnDate:yyyy/MM/dd HH:mm}");
                        AddPara($"العميل: {returnRecord.CustomerName}");
                        AddPara($"عدد الأصناف: {returnRecord.ItemsCount}");
                        AddPara($"قيمة المرتجع: {returnRecord.ReturnAmount:N2} ج.م", true);
                        AddPara($"المعالج: {returnRecord.ProcessedBy}");
                        if (!string.IsNullOrWhiteSpace(returnRecord.Notes))
                        {
                            AddPara("");
                            AddPara("ملاحظات:", true);
                            AddPara(returnRecord.Notes);
                        }

                        var paginator = ((IDocumentPaginatorSource)doc).DocumentPaginator;
                        printDialog.PrintDocument(paginator, $"Return {returnRecord.ReturnNumber}");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"خطأ في طباعة المرتجع: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        #endregion

        #region الإحصائيات

        private async Task UpdateStatisticsAsync()
        {
            try
            {
                var totalReturns = ReturnsHistory.Count;
                var totalValue = ReturnsHistory.Where(r => r.Status == "مكتمل").Sum(r => r.ReturnAmount);
                var todayReturns = ReturnsHistory.Count(r => r.ReturnDate.Date == DateTime.Today);
                var averageValue = totalReturns > 0 ? totalValue / totalReturns : 0;

                lblTotalReturns.Text = totalReturns.ToString();
                lblTotalReturnValue.Text = $"{totalValue:N2} ج.م";
                lblTodayReturns.Text = todayReturns.ToString();
                lblAverageReturnValue.Text = $"{averageValue:N2} ج.م";
                
                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحديث الإحصائيات: {ex.Message}");
            }
        }

        #endregion

        #region تحديث الإجماليات

        private void dgInvoiceItems_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.Column.Header.ToString() == "كمية المرتجع")
            {
                // تأخير التحديث للسماح بانتهاء التحرير
                Dispatcher.BeginInvoke(new Action(UpdateReturnTotals), DispatcherPriority.Background);
            }
        }

        private void dgInvoiceItems_CurrentCellChanged(object sender, EventArgs e)
        {
            UpdateReturnTotals();
        }

        private void UpdateReturnTotals()
        {
            try
            {
                var totalReturn = 0m;
                
                foreach (var item in InvoiceItems)
                {
                    // تحديث إجمالي العنصر
                    item.ReturnTotal = item.ReturnQuantity * item.UnitPrice;
                    
                    if (item.IsSelected)
                    {
                        totalReturn += item.ReturnTotal;
                    }
                }
                
                lblReturnTotal.Text = $"{totalReturn:N2} ج.م";
                
                // إعادة تحميل الجدول لعرض التحديثات
                dgInvoiceItems.Items.Refresh();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحديث الإجماليات: {ex.Message}");
            }
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region إغلاق النافذة

        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            base.OnClosed(e);
        }

        #endregion
    }

    #region فئات البيانات المساعدة

    public class ReturnInvoiceItem : INotifyPropertyChanged
    {
        public int InvoiceItemId { get; set; }
        public int ProductId { get; set; }
        public required string ProductName { get; set; }
        public required string ProductBarcode { get; set; }
        public required string Unit { get; set; }
        public decimal OriginalQuantity { get; set; }
        public decimal AvailableQuantity { get; set; }
        
        private decimal _returnQuantity;
        public decimal ReturnQuantity
        {
            get => _returnQuantity;
            set
            {
                if (_returnQuantity != value)
                {
                    _returnQuantity = value;
                    OnPropertyChanged(nameof(ReturnQuantity));
                    ReturnTotal = _returnQuantity * UnitPrice;
                    OnPropertyChanged(nameof(ReturnTotal));
                }
            }
        }
        
        public decimal UnitPrice { get; set; }
        
        private decimal _returnTotal;
        public decimal ReturnTotal
        {
            get => _returnTotal;
            set
            {
                if (_returnTotal != value)
                {
                    _returnTotal = value;
                    OnPropertyChanged(nameof(ReturnTotal));
                }
            }
        }
        
        public required string ReturnReason { get; set; }
        public bool IsSelected { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ReturnRecord
    {
        public required string ReturnNumber { get; set; }
        public int InvoiceId { get; set; }
        public required string InvoiceNumber { get; set; }
        public int? CustomerId { get; set; }
        public required string CustomerName { get; set; }
        public DateTime ReturnDate { get; set; }
        public int ItemsCount { get; set; }
        public decimal ReturnAmount { get; set; }
        public required string Status { get; set; }
        public required string ProcessedBy { get; set; }
        public required string Notes { get; set; }
    }

    #endregion
}