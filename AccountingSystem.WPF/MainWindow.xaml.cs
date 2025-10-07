using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AccountingSystem.WPF.ViewModels;
using AccountingSystem.WPF.Views;
using AccountingSystem.WPF.Views.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private DispatcherTimer _dataRefreshTimer;
        private bool _isSidebarOpen = false;
        private readonly IServiceProvider _serviceProvider;

        public MainWindow(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            InitializeComponent();
            SetupTimer();
            SetupDataRefreshTimer();
            UpdateDateTime();
            LoadDashboardData();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadDashboardData();
        }

        private void SetupTimer()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void SetupDataRefreshTimer()
        {
            _dataRefreshTimer = new DispatcherTimer();
            _dataRefreshTimer.Interval = TimeSpan.FromSeconds(30);
            _dataRefreshTimer.Tick += DataRefreshTimer_Tick;
            _dataRefreshTimer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            UpdateDateTime();
        }

        private void DataRefreshTimer_Tick(object sender, EventArgs e)
        {
            LoadStatistics();
            LoadNotifications();
        }

        private void UpdateDateTime()
        {
            var now = DateTime.Now;
            lblCurrentTime.Text = now.ToString("HH:mm:ss");
            lblCurrentDate.Text = now.ToString("dddd dd MMMM", new System.Globalization.CultureInfo("ar-SA"));
        }

        private void LoadDashboardData()
        {
            try
            {
                LoadStatistics();
                LoadRecentActivities();
                LoadNotifications();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات لوحة التحكم: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadStatistics()
        {
            try
            {
                Random random = new Random();

                double totalSales = 125500 + random.Next(-5000, 10000);
                int totalInvoices = 1247 + random.Next(-50, 100);
                int activeCustomers = 856 + random.Next(-20, 50);
                int alertsCount = random.Next(15, 35);

                lblTotalSales.Text = $"{totalSales:N0} ج.م";
                lblTotalInvoices.Text = totalInvoices.ToString("N0");
                lblActiveCustomers.Text = activeCustomers.ToString();
                lblAlerts.Text = alertsCount.ToString();

                if (lblSidebarSales != null)
                    lblSidebarSales.Text = $"{12500 + random.Next(-1000, 2000):N0} ج.م";
                if (lblSidebarInvoices != null)
                    lblSidebarInvoices.Text = (25 + random.Next(-5, 15)).ToString();
                if (lblSidebarCustomers != null)
                    lblSidebarCustomers.Text = (8 + random.Next(-2, 8)).ToString();

                if (lblStatusUser != null)
                    lblStatusUser.Text = "المدير العام - أحمد محمد";
                if (lblMemoryUsage != null)
                {
                    int memoryUsage = 65 + random.Next(0, 20);
                    lblMemoryUsage.Text = $"{memoryUsage}%";
                }

                if (lblConnectionStatus != null)
                {
                    lblConnectionStatus.Text = "متصل";
                    lblConnectionStatus.Foreground = new SolidColorBrush(Colors.Green);
                }

                if (lblNetworkStatus != null)
                {
                    lblNetworkStatus.Text = "متصل";
                    lblNetworkStatus.Foreground = new SolidColorBrush(Colors.Green);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الإحصائيات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadRecentActivities()
        {
            try
            {
                lvRecentActivities.Items.Clear();

                var activities = new[]
                {
                    new { Type = "فاتورة مبيعات", Description = "فاتورة رقم #2024001 - عميل: شركة النور", Date = DateTime.Now.AddHours(-2).ToString("HH:mm"), Amount = "1,250 ج.م", Status = "مكتملة" },
                    new { Type = "دفعة نقدية", Description = "تحصيل من العميل: أحمد علي", Date = DateTime.Now.AddHours(-3).ToString("HH:mm"), Amount = "800 ج.م", Status = "مكتملة" },
                    new { Type = "فاتورة شراء", Description = "فاتورة رقم #P2024045 - مورد: الشركة المتحدة", Date = DateTime.Now.AddHours(-4).ToString("HH:mm"), Amount = "3,500 ج.م", Status = "قيد المراجعة" },
                    new { Type = "إضافة منتج", Description = "منتج جديد: كاميرا رقمية Canon", Date = DateTime.Now.AddHours(-5).ToString("HH:mm"), Amount = "-", Status = "مضاف" },
                    new { Type = "تحديث مخزون", Description = "تحديث كمية المنتج: لابتوب HP", Date = DateTime.Now.AddHours(-6).ToString("HH:mm"), Amount = "15 قطعة", Status = "محدث" },
                    new { Type = "عميل جديد", Description = "إضافة عميل: شركة التقنية الحديثة", Date = DateTime.Now.AddHours(-8).ToString("HH:mm"), Amount = "-", Status = "مضاف" },
                    new { Type = "سند صرف", Description = "سند صرف رقم #EX2024012 - مصروفات إدارية", Date = DateTime.Now.AddHours(-10).ToString("HH:mm"), Amount = "500 ج.م", Status = "مكتملة" }
                };

                foreach (var activity in activities)
                {
                    lvRecentActivities.Items.Add(activity);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل النشاطات: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LoadNotifications()
        {
            try
            {
                Random random = new Random();
                int notificationCount = random.Next(3, 12);

                lblNotificationCount.Text = notificationCount.ToString();

                if (notificationCount > 8)
                {
                    lblNotificationCount.Foreground = new SolidColorBrush(Colors.Red);
                }
                else if (notificationCount > 5)
                {
                    lblNotificationCount.Foreground = new SolidColorBrush(Colors.Orange);
                }
                else
                {
                    lblNotificationCount.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));
                }

                lblStatus.Text = notificationCount > 0 ? $"يوجد {notificationCount} إشعار جديد" : "النظام جاهز";
            }
            catch (Exception)
            {
                lblNotificationCount.Text = "!";
                lblNotificationCount.Foreground = new SolidColorBrush(Colors.Red);
            }
        }

        // معالجات الأحداث للقائمة الجانبية
        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            if (_isSidebarOpen)
                HideSidebar();
            else
                ShowSidebar();
        }

        private void ShowSidebar()
        {
            _isSidebarOpen = true;
            OverlayBorder.Visibility = Visibility.Visible;
            var showStoryboard = (Storyboard)Resources["ShowSidebar"];
            showStoryboard.Begin();
        }

        private void HideSidebar()
        {
            _isSidebarOpen = false;
            var hideStoryboard = (Storyboard)Resources["HideSidebar"];
            hideStoryboard.Completed += (s, ev) => OverlayBorder.Visibility = Visibility.Collapsed;
            hideStoryboard.Begin();
        }

        private void btnCloseSidebar_Click(object sender, RoutedEventArgs e)
        {
            HideSidebar();
        }

        private void OverlayBorder_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            HideSidebar();
        }

        // معالجات الأحداث للأزرار العلوية والنوافذ
        private void btnNotifications_Click(object sender, RoutedEventArgs e) => OpenWindow<NotificationCenterWindow>();
        private void btnQuickSettings_Click(object sender, RoutedEventArgs e) => OpenWindow<SettingsWindow>();
        private void btnAllScreens_Click(object sender, RoutedEventArgs e) => ShowAllScreensWindow();

        private void ShowAllScreensWindow()
        {
            MessageBox.Show("This will eventually show a searchable list of all windows.", "All Screens", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // قائمة ملف
        private void mnuNewCustomer_Click(object sender, RoutedEventArgs e) => _serviceProvider.GetRequiredService<CustomerDialog>().ShowDialog();
        private void mnuNewSupplier_Click(object sender, RoutedEventArgs e) => _serviceProvider.GetRequiredService<SupplierSearchDialog>().ShowDialog();
        private void mnuNewProduct_Click(object sender, RoutedEventArgs e) => _serviceProvider.GetRequiredService<ProductDialog>().ShowDialog();
        private void mnuBackup_Click(object sender, RoutedEventArgs e) => OpenWindow<BackupRestoreWindow>();
        private void mnuPrint_Click(object sender, RoutedEventArgs e) => ShowMessage("الطباعة العامة غير مدعومة بعد، يرجى الطباعة من الفاتورة مباشرة.");
        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("هل تريد إغلاق البرنامج؟", "تأكيد الخروج", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        // قائمة البيانات الأساسية
        private void mnuCustomers_Click(object sender, RoutedEventArgs e) => OpenWindow<CustomersWindow>();
        private void mnuSuppliers_Click(object sender, RoutedEventArgs e) => OpenWindow<SuppliersWindow>();
        private void mnuProducts_Click(object sender, RoutedEventArgs e) => OpenWindow<ProductsWindow>();
        private void mnuCategories_Click(object sender, RoutedEventArgs e) => OpenWindow<CategoriesWindow>();
        private void mnuUnits_Click(object sender, RoutedEventArgs e) => OpenWindow<UnitsWindow>();
        private void mnuWarehouses_Click(object sender, RoutedEventArgs e) => ShowMessage("إدارة المخازن غير متاح بعد");

        // قائمة المبيعات
        private void mnuPOS_Click(object sender, RoutedEventArgs e) => OpenWindow<POSWindow>();
        private void mnuNewSalesInvoice_Click(object sender, RoutedEventArgs e) => OpenWindow<SalesInvoiceWindow>();
        private void mnuSalesInvoices_Click(object sender, RoutedEventArgs e) => OpenWindow<SalesInvoicesListWindow>();
        private void mnuDraftInvoices_Click(object sender, RoutedEventArgs e) => OpenWindow<DraftInvoicesWindow>();
        private void mnuSalesReturns_Click(object sender, RoutedEventArgs e) => OpenWindow<SalesReturnWindow>();
        private void mnuReturnsManagement_Click(object sender, RoutedEventArgs e) => OpenWindow<ReturnsManagementWindow>();
        private void mnuQuotations_Click(object sender, RoutedEventArgs e) => OpenWindow<QuotationsWindow>();
        private void mnuDiscountManagement_Click(object sender, RoutedEventArgs e) => OpenWindow<DiscountManagementWindow>();
        private void mnuLoyaltyProgram_Click(object sender, RoutedEventArgs e) => OpenWindow<LoyaltyProgramWindow>();

        // قائمة المشتريات
        private void mnuNewPurchaseInvoice_Click(object sender, RoutedEventArgs e) => OpenWindow<PurchaseInvoiceWindow>();
        private void mnuPurchaseInvoices_Click(object sender, RoutedEventArgs e) => OpenWindow<PurchaseInvoicesListWindow>();
        private void mnuPurchaseOrders_Click(object sender, RoutedEventArgs e) => OpenWindow<PurchaseOrdersWindow>();
        private void mnuPurchaseReturns_Click(object sender, RoutedEventArgs e) => OpenWindow<PurchaseReturnWindow>();

        // قائمة المخزون
        private void mnuStockReport_Click(object sender, RoutedEventArgs e) => OpenWindow<InventoryReportsWindow>();
        private void mnuStockMovements_Click(object sender, RoutedEventArgs e) => OpenWindow<StockMovementsWindow>();
        private void mnuBarcodeReader_Click(object sender, RoutedEventArgs e) => OpenWindow<BarcodeReaderWindow>();
        private void mnuInventoryCount_Click(object sender, RoutedEventArgs e) => OpenWindow<StockAdjustmentWindow>();
        private void mnuStockAlerts_Click(object sender, RoutedEventArgs e) => ShowMessage("تنبيهات المخزون غير متاحة بعد");

        // قائمة الخزينة والمالية
        private void mnuCashBoxes_Click(object sender, RoutedEventArgs e) => OpenWindow<CashBoxWindow>();
        private void mnuCashTransactions_Click(object sender, RoutedEventArgs e) => OpenWindow<CashTransactionsWindow>();
        private void mnuCashReport_Click(object sender, RoutedEventArgs e) => OpenWindow<CashBoxReportsWindow>();
        private void mnuReceiptVouchers_Click(object sender, RoutedEventArgs e) => OpenWindow<ReceiptVouchersWindow>();
        private void mnuPaymentVouchers_Click(object sender, RoutedEventArgs e) => OpenWindow<PaymentVouchersWindow>();
        private void mnuCashTransfer_Click(object sender, RoutedEventArgs e) => _serviceProvider.GetRequiredService<CashTransferDialog>().ShowDialog();
        private void mnuBankAccounts_Click(object sender, RoutedEventArgs e) => OpenWindow<BankAccountsWindow>();
        private void mnuChecks_Click(object sender, RoutedEventArgs e) => ShowMessage("إدارة الشيكات غير متاحة بعد");
        private void mnuExpenses_Click(object sender, RoutedEventArgs e) => ShowMessage("إدارة المصروفات غير متاحة بعد");

        // قائمة التقارير
        private void mnuReports_Click(object sender, RoutedEventArgs e) => OpenWindow<ReportsWindow>();
        private void mnuFinancialAnalysis_Click(object sender, RoutedEventArgs e) => OpenWindow<FinancialAnalysisWindow>();
        private void mnuSalesReports_Click(object sender, RoutedEventArgs e) => OpenWindow<SalesReportsWindow>();
        private void mnuInventoryReports_Click(object sender, RoutedEventArgs e) => OpenWindow<InventoryReportsWindow>();
        private void mnuCustomerReports_Click(object sender, RoutedEventArgs e) => OpenWindow<CustomerReportsWindow>();
        private void mnuSupplierReports_Click(object sender, RoutedEventArgs e) => OpenWindow<SupplierReportsWindow>();
        private void mnuCharts_Click(object sender, RoutedEventArgs e) => OpenWindow<ChartsWindow>();
        private void mnuAdvancedReports_Click(object sender, RoutedEventArgs e) => OpenWindow<AdvancedReportsWindow>();

        // قائمة أدوات النظام
        private void mnuPrintTemplates_Click(object sender, RoutedEventArgs e) => OpenWindow<PrintTemplateDesignerWindow>();
        private void mnuThermalPrinting_Click(object sender, RoutedEventArgs e) => OpenWindow<ThermalReceiptPrintWindow>();
        private void mnuBackupRestore_Click(object sender, RoutedEventArgs e) => OpenWindow<BackupRestoreWindow>();
        private void mnuImportExport_Click(object sender, RoutedEventArgs e) => OpenWindow<ImportExportWindow>();
        private void mnuDatabaseMaintenance_Click(object sender, RoutedEventArgs e) => OpenWindow<DiagnosticWindow>();

        // قائمة الإعدادات
        private void mnuSettings_Click(object sender, RoutedEventArgs e) => OpenWindow<SettingsWindow>();
        private void mnuSystemSettings_Click(object sender, RoutedEventArgs e) => OpenWindow<SystemSettingsWindow>();
        private void mnuUserManagement_Click(object sender, RoutedEventArgs e) => OpenWindow<AdvancedUserManagementWindow>();
        private void mnuPermissions_Click(object sender, RoutedEventArgs e) => OpenWindow<UsersPermissionsWindow>();
        private void mnuCustomizeInterface_Click(object sender, RoutedEventArgs e) => OpenPageAsWindow(_serviceProvider.GetRequiredService<AppearanceSettingsPage>(), "تخصيص الواجهة");
        private void mnuSystemMonitoring_Click(object sender, RoutedEventArgs e) => OpenWindow<SystemMonitoringWindow>();

        // قائمة المساعدة
        private void mnuUserManual_Click(object sender, RoutedEventArgs e) => ShowMessage("سيتم فتح دليل المستخدم");
        private void mnuVideoTutorials_Click(object sender, RoutedEventArgs e) => ShowMessage("سيتم فتح قائمة الفيديوهات التعليمية");
        private void mnuCheckUpdates_Click(object sender, RoutedEventArgs e) => ShowMessage("جاري فحص التحديثات...");
        private void mnuTechnicalSupport_Click(object sender, RoutedEventArgs e) => ShowMessage("للتواصل مع الدعم التقني...");
        private void mnuAbout_Click(object sender, RoutedEventArgs e)
        {
            var aboutMessage = @"💼 النظام المحاسبي الشامل
الإصدار: 2024.1.0

🏢 تطوير: فريق التطوير المحترف
📧 البريد الإلكتروني: support@accounting.com
📞 الهاتف: +20-xxx-xxx-xxxx

✨ المميزات:
• نظام محاسبي متكامل
• إدارة المبيعات والمشتريات
• إدارة المخزون والعملاء
• تقارير تفصيلية ومتقدمة
• واجهة عربية احترافية

© 2024 جميع الحقوق محفوظة";

            MessageBox.Show(aboutMessage, "حول النظام المحاسبي الشامل", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // البحث في القائمة الجانبية
        private void txtSidebarSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox?.Text == "بحث سريع...")
            {
                textBox.Text = "";
                textBox.Foreground = new SolidColorBrush(Colors.White);
            }
        }

        private void txtSidebarSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (string.IsNullOrWhiteSpace(textBox?.Text))
            {
                textBox.Text = "بحث سريع...";
                textBox.Foreground = new SolidColorBrush(Colors.LightGray);
            }
        }

        private void btnSidebarSearch_Click(object sender, RoutedEventArgs e)
        {
            var searchText = txtSidebarSearch.Text;
            if (!string.IsNullOrWhiteSpace(searchText) && searchText != "بحث سريع...")
            {
                ShowMessage($"البحث عن: {searchText}");
            }
        }

        private void ShowMessage(string message)
        {
            try
            {
                if (lblStatus != null)
                {
                    lblStatus.Text = $"تم النقر على: {message}";
                    
                    var fadeAnimation = new DoubleAnimation
                    {
                        From = 0.3,
                        To = 1,
                        Duration = TimeSpan.FromMilliseconds(400)
                    };
                    lblStatus.BeginAnimation(OpacityProperty, fadeAnimation);
                }
                
                UpdateDateTime();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShowMessage: {ex.Message}");
            }
        }

        private void OpenWindow<T>() where T : Window
        {
            try
            {
                var window = _serviceProvider.GetRequiredService<T>();
                window.Owner = this;
                window.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"تعذر فتح النافذة: {ex.Message}\n\n{ex.InnerException?.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            HideSidebar();
        }

        private void OpenPageAsWindow(Page page, string title)
        {
            var window = new Window
            {
                Title = title,
                Content = page,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Width = 800,
                Height = 600
            };
            window.Show();
            HideSidebar();
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            _dataRefreshTimer?.Stop();
            base.OnClosed(e);
        }
    }
}