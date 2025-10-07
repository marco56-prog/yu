using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using AccountingSystem.WPF.ViewModels;

namespace AccountingSystem.WPF
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;
        private DispatcherTimer _dataRefreshTimer;
        private bool _isSidebarOpen = false;
        private readonly MainWindowViewModel _viewModel;

        public MainWindow(MainWindowViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            DataContext = _viewModel;
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
            catch (Exception ex)
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
            hideStoryboard.Completed += (s, e) => OverlayBorder.Visibility = Visibility.Collapsed;
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

        // معالجات أحداث الأزرار العلوية
        private void btnNotifications_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("نافذة الإشعارات");
        }

        private void btnQuickSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("الإعدادات السريعة");
        }

        private void btnAllScreens_Click(object sender, RoutedEventArgs e)
        {
            ShowAllScreensWindow();
        }

        private void ShowAllScreensWindow()
        {
            try
            {
                var allScreensMessage = @"🖥️ الشاشات المتوفرة في النظام:

📊 العمليات الأساسية:
• نقطة البيع (POS) - F1
• فاتورة مبيعات جديدة - F2
• فاتورة مشتريات جديدة - F3
• إدارة العملاء - F5
• إدارة الموردين - F6
• إدارة المنتجات - F7
• تقرير المخزون - F8
• التقارير العامة - F9

👥 إدارة البيانات:
• إدارة الفئات
• إدارة وحدات القياس
• إدارة المخازن
• إدارة الخزائن
• إدارة الحسابات البنكية

💰 العمليات المالية:
• حركات الخزينة
• سندات القبض والصرف
• إدارة الشيكات
• إدارة المصروفات

📈 التقارير المتقدمة:
• تقارير المبيعات التفصيلية
• تقارير المشتريات
• تقارير العملاء والموردين
• التحليل المالي والرسوم البيانية

⚙️ الإعدادات والأدوات:
• إعدادات النظام
• إدارة المستخدمين والصلاحيات
• النسخ الاحتياطي والاستعادة
• صيانة قاعدة البيانات

يمكنك الوصول لجميع الشاشات من القائمة الجانبية (☰) أو شريط القوائم العلوي";

                MessageBox.Show(allScreensMessage, "جميع الشاشات المتوفرة", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                ShowMessage($"خطأ في عرض الشاشات: {ex.Message}");
            }
        }

        // العمليات السريعة
        private void mnuNewSalesInvoice_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("فاتورة مبيعات جديدة");
            HideSidebar();
        }

        private void mnuPOS_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("نقطة البيع");
            HideSidebar();
        }

        private void mnuProducts_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إدارة المنتجات");
            HideSidebar();
        }

        private void mnuCustomers_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إدارة العملاء");
            HideSidebar();
        }

        private void mnuNewPurchaseInvoice_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("فاتورة شراء جديدة");
            HideSidebar();
        }

        private void mnuCashBoxes_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إدارة الخزينة");
            HideSidebar();
        }

        private void mnuReports_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("التقارير والإحصائيات");
            HideSidebar();
        }

        private void mnuSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إعدادات النظام");
            HideSidebar();
        }

        // القائمة الجانبية
        private void mnuDashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("الصفحة الرئيسية");
            HideSidebar();
        }

        private void mnuSalesInvoices_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("جميع فواتير المبيعات");
            HideSidebar();
        }

        private void mnuPurchaseInvoices_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("جميع فواتير الشراء");
            HideSidebar();
        }

        private void mnuSuppliers_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إدارة الموردين");
            HideSidebar();
        }

        private void mnuCategories_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إدارة الفئات");
            HideSidebar();
        }

        private void mnuUnits_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إدارة وحدات القياس");
            HideSidebar();
        }

        private void mnuBarcodeReader_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("قارئ الباركود");
            HideSidebar();
        }

        private void mnuWarehouses_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إدارة المخازن");
            HideSidebar();
        }

        private void mnuQuotations_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("عروض الأسعار");
            HideSidebar();
        }

        private void mnuDraftInvoices_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("الفواتير المؤجلة");
            HideSidebar();
        }

        private void mnuPurchaseOrders_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("أوامر الشراء");
            HideSidebar();
        }

        private void mnuStockReport_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("تقرير المخزون الحالي");
            HideSidebar();
        }

        private void mnuStockMovements_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("حركات المخزن");
            HideSidebar();
        }

        private void mnuInventoryCount_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("الجرد الدوري");
            HideSidebar();
        }

        private void mnuStockAlerts_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("تنبيهات المخزون");
            HideSidebar();
        }

        private void mnuCashTransactions_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("حركات الخزينة");
            HideSidebar();
        }

        private void mnuCashReport_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("تقرير الخزينة");
            HideSidebar();
        }

        private void mnuReceiptVouchers_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("سندات القبض");
            HideSidebar();
        }

        private void mnuPaymentVouchers_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("سندات الصرف");
            HideSidebar();
        }

        private void mnuBankAccounts_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("الحسابات البنكية");
            HideSidebar();
        }

        private void mnuChecks_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إدارة الشيكات");
            HideSidebar();
        }

        private void mnuExpenses_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إدارة المصروفات");
            HideSidebar();
        }

        private void mnuAdvancedReports_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("التقارير المتقدمة");
            HideSidebar();
        }

        private void mnuCharts_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("الرسوم البيانية");
            HideSidebar();
        }

        private void mnuFinancialAnalysis_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("التحليل المالي المتقدم");
            HideSidebar();
        }

        private void mnuSalesReports_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("تقارير المبيعات التفصيلية");
            HideSidebar();
        }

        private void mnuInventoryReports_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("تقارير المخزون المتقدمة");
            HideSidebar();
        }

        private void mnuCustomerReports_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("تقارير العملاء التفصيلية");
            HideSidebar();
        }

        private void mnuSupplierReports_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("تقارير الموردين التفصيلية");
            HideSidebar();
        }

        private void mnuSystemSettings_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إعدادات النظام المتقدمة");
            HideSidebar();
        }

        private void mnuUserManagement_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إدارة المستخدمين");
            HideSidebar();
        }

        private void mnuBackupRestore_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("النسخ والاستعادة");
            HideSidebar();
        }

        private void mnuDatabaseMaintenance_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("صيانة قاعدة البيانات");
            HideSidebar();
        }

        private void mnuSystemMonitoring_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("مراقبة النظام");
            HideSidebar();
        }

        private void mnuPermissions_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إدارة الصلاحيات");
            HideSidebar();
        }

        private void mnuCustomizeInterface_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("تخصيص واجهة المستخدم");
            HideSidebar();
        }

        private void mnuTechnicalSupport_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("الدعم التقني");
            HideSidebar();
        }

        private void mnuUserManual_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("دليل المستخدم");
            HideSidebar();
        }

        // شريط القوائم العلوي
        private void mnuNewCustomer_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إضافة عميل جديد");
        }

        private void mnuNewSupplier_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إضافة مورد جديد");
        }

        private void mnuNewProduct_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إضافة منتج جديد");
        }

        private void mnuBackup_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("نسخ احتياطي");
        }

        private void mnuPrint_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("طباعة");
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("هل تريد إغلاق البرنامج؟", "تأكيد الخروج", 
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void mnuSalesReturns_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("مرتجعات المبيعات");
            HideSidebar();
        }

        private void mnuPurchaseReturns_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("مرتجعات الشراء");
            HideSidebar();
        }

        private void mnuLoyaltyProgram_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("برنامج الولاء");
        }

        private void mnuDiscountManagement_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("إدارة الخصومات");
        }

        private void mnuPrintTemplates_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("قوالب الطباعة");
        }

        private void mnuThermalPrinting_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("الطباعة الحرارية");
        }

        private void mnuVideoTutorials_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("فيديوهات تعليمية");
        }

        private void mnuCheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            ShowMessage("فحص التحديثات");
        }

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
                Console.WriteLine($"Button clicked: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ShowMessage: {ex.Message}");
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _timer?.Stop();
            _dataRefreshTimer?.Stop();
            base.OnClosed(e);
        }
    }
}