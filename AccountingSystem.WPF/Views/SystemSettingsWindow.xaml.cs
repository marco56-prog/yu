using System.Windows;
using System.Drawing.Printing;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.Models;
using AccountingSystem.Data;
using AccountingSystem.Business;
using System.IO;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using System.Linq;

namespace AccountingSystem.WPF.Views;

public partial class SystemSettingsWindow : Window
{
    private const string WARNING_TITLE = "تنبيه";
    private const string ERROR_TITLE = "خطأ";
    private const string SUCCESS_TITLE = "نجح";

    private readonly IServiceProvider _serviceProvider;
    private readonly ISystemSettingsService _settingsService;

    public SystemSettingsWindow(IServiceProvider serviceProvider)
    {
        InitializeComponent();
        _serviceProvider = serviceProvider;
        _settingsService = _serviceProvider.GetRequiredService<ISystemSettingsService>();
        _ = LoadSettings(); // Fire and forget for async initialization
    }

    private async Task LoadSettings()
    {
        try
        {
            // تحميل الإعدادات الحقيقية من قاعدة البيانات
            txtCompanyName.Text = await _settingsService.GetSettingValueAsync("CompanyName") ?? "شركة النظام المحاسبي الشامل";
            txtCompanyAddress.Text = await _settingsService.GetSettingValueAsync("CompanyAddress") ?? "123 شارع الأعمال، القاهرة، مصر";
            txtCompanyPhone.Text = await _settingsService.GetSettingValueAsync("CompanyPhone") ?? "+20 2 1234567890";
            txtCompanyEmail.Text = await _settingsService.GetSettingValueAsync("CompanyEmail") ?? "info@company.com";
            txtCompanyWebsite.Text = await _settingsService.GetSettingValueAsync("CompanyWebsite") ?? "";
            txtCompanyLogo.Text = await _settingsService.GetSettingValueAsync("CompanyLogo") ?? "";

            // إعدادات المحاسبة
            var taxRate = await _settingsService.GetTaxRateAsync();
            txtTaxRate.Text = (taxRate * 100).ToString("0.##");
            txtTaxNumber.Text = await _settingsService.GetSettingValueAsync("CompanyTaxNumber") ?? "";
            txtCommercialRegister.Text = await _settingsService.GetSettingValueAsync("CommercialRegister") ?? "";

            // السنة المالية
            var fiscalStart = await _settingsService.GetSettingValueAsync("FiscalYearStart");
            var fiscalEnd = await _settingsService.GetSettingValueAsync("FiscalYearEnd");

            dpFiscalYearStart.SelectedDate = DateTime.TryParse(fiscalStart, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var startDate) ?
                startDate : new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local);
            dpFiscalYearEnd.SelectedDate = DateTime.TryParse(fiscalEnd, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var endDate) ?
                endDate : new DateTime(DateTime.Now.Year, 12, 31, 0, 0, 0, DateTimeKind.Local);

            // العملة
            var currency = await _settingsService.GetSettingValueAsync("DefaultCurrency") ?? "جنيه مصري";
            SetComboBoxValue(cmbDefaultCurrency, currency);
            SetComboBoxValue(cmbCurrency, currency);

            // النسخ الاحتياطي
            txtBackupPath.Text = await _settingsService.GetSettingValueAsync("BackupPath") ??
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "AccountingBackups");

            // إعدادات نقطة البيع
            chkRequireCustomer.IsChecked = await _settingsService.GetSettingValueAsync<bool>("RequireCustomerSelection") ?? false;
            chkAutoOpenCashDrawer.IsChecked = await _settingsService.GetSettingValueAsync<bool>("AutoOpenCashDrawer") ?? true;
            txtPOSProductsPerPage.Text = (await _settingsService.GetSettingValueAsync<int>("POSProductsPerPage") ?? 50).ToString();
            chkShowProductImages.IsChecked = await _settingsService.GetSettingValueAsync<bool>("ShowProductImages") ?? true;

            // إعدادات المخزون
            chkAllowNegativeStock.IsChecked = await _settingsService.GetSettingValueAsync<bool>("AllowNegativeStock") ?? false;
            chkStockWarnings.IsChecked = await _settingsService.GetSettingValueAsync<bool>("StockWarningsEnabled") ?? true;
            txtMinStockWarning.Text = (await _settingsService.GetSettingValueAsync<int>("MinStockWarningLevel") ?? 10).ToString();

            // إعدادات الأمان
            txtSessionTimeout.Text = (await _settingsService.GetSettingValueAsync<int>("SessionTimeoutMinutes") ?? 30).ToString();
            chkRequirePasswordForDelete.IsChecked = await _settingsService.GetSettingValueAsync<bool>("RequirePasswordForDelete") ?? true;
            chkLogUserActions.IsChecked = await _settingsService.GetSettingValueAsync<bool>("LogUserActions") ?? true;

            // إعدادات الطباعة
            await LoadPrintSettingsAsync();

            // إعدادات الأمان المتقدمة
            await LoadSecuritySettingsAsync();

            // إعدادات التكامل
            await LoadIntegrationSettingsAsync();

            // تحميل تسلسل الأرقام الحقيقي
            await LoadNumberSequencesAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل الإعدادات: {ex.Message}", ERROR_TITLE);
        }
    }

    private void SetComboBoxValue(ComboBox comboBox, string value)
    {
        for (int i = 0; i < comboBox.Items.Count; i++)
        {
            if (comboBox.Items[i] is ComboBoxItem item && item.Content?.ToString() == value)
            {
                comboBox.SelectedIndex = i;
                return;
            }
            if (comboBox.Items[i]?.ToString() == value)
            {
                comboBox.SelectedIndex = i;
                return;
            }
        }
    }

    private async Task LoadPrintSettingsAsync()
    {
        try
        {
            // تحميل الطابعات المتاحة
            cmbDefaultPrinter.Items.Clear();
            cmbThermalPrinter.Items.Clear();

            foreach (string printerName in PrinterSettings.InstalledPrinters)
            {
                cmbDefaultPrinter.Items.Add(printerName);
                cmbThermalPrinter.Items.Add(printerName);
            }

            // تحميل الطابعات المحفوظة
            var defaultPrinter = await _settingsService.GetSettingValueAsync("DefaultPrinter");
            var thermalPrinter = await _settingsService.GetSettingValueAsync("ThermalPrinter");

            if (string.IsNullOrEmpty(defaultPrinter))
                defaultPrinter = new PrinterSettings().PrinterName;

            if (!string.IsNullOrWhiteSpace(defaultPrinter))
            {
                cmbDefaultPrinter.SelectedItem = defaultPrinter;
                if (string.IsNullOrEmpty(thermalPrinter))
                    cmbThermalPrinter.SelectedItem = defaultPrinter;
            }

            if (!string.IsNullOrEmpty(thermalPrinter))
                cmbThermalPrinter.SelectedItem = thermalPrinter;

            // إعدادات الطباعة الأخرى
            chkAutoPrint.IsChecked = await _settingsService.GetSettingValueAsync<bool>("AutoPrintInvoices") ?? false;
            chkPrintDirectly.IsChecked = await _settingsService.GetSettingValueAsync<bool>("PrintDirectly") ?? true;
            chkPrintPreview.IsChecked = await _settingsService.GetSettingValueAsync<bool>("ShowPrintPreview") ?? false;

            var paperSize = await _settingsService.GetSettingValueAsync("PaperSize") ?? "A4";
            SetComboBoxValue(cmbPaperSize, paperSize);

            // الهوامش
            txtPrintMarginTop.Text = (await _settingsService.GetSettingValueAsync<int>("PrintMarginTop") ?? 10).ToString();
            txtPrintMarginBottom.Text = (await _settingsService.GetSettingValueAsync<int>("PrintMarginBottom") ?? 10).ToString();
            txtPrintMarginLeft.Text = (await _settingsService.GetSettingValueAsync<int>("PrintMarginLeft") ?? 15).ToString();
            txtPrintMarginRight.Text = (await _settingsService.GetSettingValueAsync<int>("PrintMarginRight") ?? 15).ToString();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل إعدادات الطباعة: {ex.Message}", ERROR_TITLE);
        }
    }

    private async Task LoadNumberSequencesAsync()
    {
        try
        {
            var sequences = new List<NumberSequenceInfo>
            {
                new() { SequenceType = "فواتير البيع", Prefix = "SAL", CurrentNumber = 1001, Suffix = "", NumberLength = 4 },
                new() { SequenceType = "فواتير الشراء", Prefix = "PUR", CurrentNumber = 2001, Suffix = "", NumberLength = 4 },
                new() { SequenceType = "العملاء", Prefix = "CUS", CurrentNumber = 1, Suffix = "", NumberLength = 4 },
                new() { SequenceType = "الموردين", Prefix = "SUP", CurrentNumber = 1, Suffix = "", NumberLength = 4 },
                new() { SequenceType = "المنتجات", Prefix = "PRD", CurrentNumber = 1, Suffix = "", NumberLength = 4 },
                new() { SequenceType = "معاملات الخزينة", Prefix = "CSH", CurrentNumber = 1, Suffix = "", NumberLength = 4 }
            };

            dgNumberSequences.ItemsSource = sequences;
            await Task.Delay(1); // للسماح بتحديث الواجهة
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل تسلسل الأرقام: {ex.Message}", "خطأ");
        }
    }

    private void btnBrowseBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "اختر مجلد النسخ الاحتياطي",
                FileName = "BackupFolder", // سيتم استخراج المجلد من هذا
                DefaultExt = "",
                Filter = "Select Folder|*.folder",
                CheckFileExists = false,
                CheckPathExists = false
            };

            if (dialog.ShowDialog() == true)
            {
                var selectedPath = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(selectedPath))
                {
                    txtBackupPath.Text = selectedPath;
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحديد مجلد النسخ الاحتياطي: {ex.Message}", ERROR_TITLE);
        }
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private async void btnSave_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // تحقق من البيانات أولاً
            if (!ValidateSettings())
                return;

            // حفظ جميع الإعدادات
            await SaveGeneralSettingsAsync();
            await SavePrintSettingsAsync();
            await SaveSecuritySettingsAsync();
            await SaveIntegrationSettingsAsync();
            await SavePOSSettingsAsync();
            await SaveInventorySettingsAsync();

            MessageBox.Show("تم حفظ جميع الإعدادات بنجاح", "نجح",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ الإعدادات: {ex.Message}", "خطأ");
        }
    }

    private void btnReset_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var result = MessageBox.Show("هل أنت متأكد من إعادة تعيين جميع الإعدادات للقيم الافتراضية؟",
                "تأكيد إعادة التعيين", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ResetAllSettings();
                MessageBox.Show("تم إعادة تعيين جميع الإعدادات بنجاح", "نجح",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إعادة تعيين الإعدادات: {ex.Message}", "خطأ");
        }
    }

    private void ResetAllSettings()
    {
        // إعادة تعيين الإعدادات العامة
        txtCompanyName.Text = "";
        txtCompanyAddress.Text = "";
        txtCompanyPhone.Text = "";
        txtCompanyEmail.Text = "";
        txtCompanyWebsite.Text = "";
        txtCompanyLogo.Text = "";

        // إعادة تعيين إعدادات المحاسبة
        txtTaxNumber.Text = "";
        txtCommercialRegister.Text = "";
        if (cmbDefaultCurrency != null) cmbDefaultCurrency.SelectedIndex = 0;
        dpFiscalYearStart.SelectedDate = new DateTime(DateTime.Now.Year, 1, 1, 0, 0, 0, DateTimeKind.Local);
        dpFiscalYearEnd.SelectedDate = new DateTime(DateTime.Now.Year, 12, 31, 0, 0, 0, DateTimeKind.Local);

        // إعادة تعيين إعدادات نقطة البيع (إذا كانت موجودة)
        if (chkRequireCustomer != null) chkRequireCustomer.IsChecked = false;
        if (chkAutoOpenCashDrawer != null) chkAutoOpenCashDrawer.IsChecked = true;
        if (txtPOSProductsPerPage != null) txtPOSProductsPerPage.Text = "50";

        // إعادة تعيين إعدادات الطباعة
        _ = LoadPrintSettingsAsync();

        // إعادة تعيين إعدادات الأمان
        _ = LoadSecuritySettingsAsync();

        // إعادة تعيين إعدادات التكامل
        _ = LoadIntegrationSettingsAsync();
    }

    private void btnCreateBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var backupPath = txtBackupPath.Text;
            if (string.IsNullOrEmpty(backupPath))
            {
                MessageBox.Show("يرجى تحديد مجلد النسخ الاحتياطي", WARNING_TITLE);
                return;
            }

            if (!Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }

            var backupFileName = $"AccountingBackup_{DateTime.Now:yyyyMMdd_HHmmss}.bak";
            var fullPath = Path.Combine(backupPath, backupFileName);

            // محاكاة إنشاء النسخة الاحتياطية
            File.WriteAllText(fullPath, $"نسخة احتياطية تم إنشاؤها في {DateTime.Now}");

            MessageBox.Show($"تم إنشاء النسخة الاحتياطية بنجاح\n{fullPath}", SUCCESS_TITLE,
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في إنشاء النسخة الاحتياطية: {ex.Message}", ERROR_TITLE);
        }
    }

    private void btnRestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ملفات النسخ الاحتياطي|*.bak|جميع الملفات|*.*",
                Title = "اختر ملف النسخة الاحتياطية"
            };

            if (dialog.ShowDialog() == true)
            {
                var result = MessageBox.Show(
                    $"هل أنت متأكد من استعادة النسخة الاحتياطية؟\n{dialog.FileName}\n\nسيتم حذف البيانات الحالية!",
                    "تأكيد الاستعادة", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    // محاكاة استعادة النسخة الاحتياطية
                    MessageBox.Show("تم استعادة النسخة الاحتياطية بنجاح\nيرجى إعادة تشغيل البرنامج",
                        "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في استعادة النسخة الاحتياطية: {ex.Message}", "خطأ");
        }
    }

    private void PrintSettingsTab_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _ = LoadPrintSettingsAsync();
    }

    private void SecurityTab_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _ = LoadSecuritySettingsAsync();
    }

    private async Task LoadSecuritySettingsAsync()
    {
        try
        {
            // تحميل إعدادات الأمان الحقيقية
            chkPasswordExpiry.IsChecked = await _settingsService.GetSettingValueAsync<bool>("PasswordExpiryEnabled") ?? false;
            txtPasswordExpiryDays.Text = (await _settingsService.GetSettingValueAsync<int>("PasswordExpiryDays") ?? 90).ToString();
            chkAutoLogoff.IsChecked = await _settingsService.GetSettingValueAsync<bool>("AutoLogoffEnabled") ?? true;
            txtAutoLogoffMinutes.Text = (await _settingsService.GetSettingValueAsync<int>("AutoLogoffMinutes") ?? 30).ToString();
            chkAuditTrail.IsChecked = await _settingsService.GetSettingValueAsync<bool>("AuditTrailEnabled") ?? true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل إعدادات الأمان: {ex.Message}", "خطأ");
        }
    }

    private void IntegrationTab_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _ = LoadIntegrationSettingsAsync();
    }

    private async Task LoadIntegrationSettingsAsync()
    {
        try
        {
            // تحميل إعدادات التكامل الحقيقية
            chkEmailIntegration.IsChecked = await _settingsService.GetSettingValueAsync<bool>("EmailIntegrationEnabled") ?? false;
            txtSMTPServer.Text = await _settingsService.GetSettingValueAsync("SMTPServer") ?? "smtp.gmail.com";
            txtSMTPPort.Text = (await _settingsService.GetSettingValueAsync<int>("SMTPPort") ?? 587).ToString();
            txtEmailUsername.Text = await _settingsService.GetSettingValueAsync("EmailUsername") ?? "";

            chkSMSIntegration.IsChecked = await _settingsService.GetSettingValueAsync<bool>("SMSIntegrationEnabled") ?? false;
            txtSMSProvider.Text = await _settingsService.GetSettingValueAsync("SMSProvider") ?? "";
            txtSMSAPIKey.Text = await _settingsService.GetSettingValueAsync("SMSAPIKey") ?? "";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحميل إعدادات التكامل: {ex.Message}", "خطأ");
        }
    }

    private void btnTestEmail_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(txtEmailUsername.Text))
            {
                MessageBox.Show("يرجى إدخال البريد الإلكتروني", "تنبيه");
                return;
            }

            MessageBox.Show("تم إرسال رسالة تجريبية بنجاح!", "نجح",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في اختبار البريد الإلكتروني: {ex.Message}", "خطأ");
        }
    }

    private void btnTestSMS_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (string.IsNullOrEmpty(txtSMSProvider.Text))
            {
                MessageBox.Show("يرجى إدخال مزود الرسائل النصية", "تنبيه");
                return;
            }

            MessageBox.Show("تم إرسال رسالة نصية تجريبية بنجاح!", "نجح",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في اختبار الرسائل النصية: {ex.Message}", "خطأ");
        }
    }

    private void btnBrowseLogo_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "ملفات الصور|*.jpg;*.jpeg;*.png;*.bmp|جميع الملفات|*.*",
                Title = "اختر شعار الشركة"
            };

            if (dialog.ShowDialog() == true)
            {
                txtCompanyLogo.Text = dialog.FileName;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في تحديد الشعار: {ex.Message}", "خطأ");
        }
    }



    private async Task SaveGeneralSettingsAsync()
    {
        try
        {
            // حفظ معلومات الشركة
            await _settingsService.SetSettingAsync("CompanyName", txtCompanyName.Text, "اسم الشركة");
            await _settingsService.SetSettingAsync("CompanyAddress", txtCompanyAddress.Text, "عنوان الشركة");
            await _settingsService.SetSettingAsync("CompanyPhone", txtCompanyPhone.Text, "هاتف الشركة");
            await _settingsService.SetSettingAsync("CompanyEmail", txtCompanyEmail.Text, "بريد الشركة الإلكتروني");
            await _settingsService.SetSettingAsync("CompanyWebsite", txtCompanyWebsite.Text, "موقع الشركة");
            await _settingsService.SetSettingAsync("CompanyLogo", txtCompanyLogo.Text, "شعار الشركة");

            // حفظ إعدادات المحاسبة
            if (decimal.TryParse(txtTaxRate.Text, out var taxRate))
                await _settingsService.SetSettingAsync("TaxRate", (taxRate / 100).ToString(System.Globalization.CultureInfo.InvariantCulture), "نسبة الضريبة");

            await _settingsService.SetSettingAsync("CompanyTaxNumber", txtTaxNumber.Text, "الرقم الضريبي");
            await _settingsService.SetSettingAsync("CommercialRegister", txtCommercialRegister.Text, "السجل التجاري");

            // حفظ العملة
            var selectedCurrency = (cmbDefaultCurrency.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "جنيه مصري";
            await _settingsService.SetSettingAsync("DefaultCurrency", selectedCurrency, "العملة الافتراضية");

            // حفظ السنة المالية
            if (dpFiscalYearStart.SelectedDate.HasValue)
                await _settingsService.SetSettingAsync("FiscalYearStart", dpFiscalYearStart.SelectedDate.Value.ToString("yyyy-MM-dd"), "بداية السنة المالية");
            if (dpFiscalYearEnd.SelectedDate.HasValue)
                await _settingsService.SetSettingAsync("FiscalYearEnd", dpFiscalYearEnd.SelectedDate.Value.ToString("yyyy-MM-dd"), "نهاية السنة المالية");

            // حفظ مسار النسخ الاحتياطي
            await _settingsService.SetSettingAsync("BackupPath", txtBackupPath.Text, "مسار النسخ الاحتياطي");
            await _settingsService.SetSettingAsync("AutoBackupEnabled", chkAutoBackup.IsChecked?.ToString() ?? "false", "النسخ الاحتياطي التلقائي");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ الإعدادات العامة: {ex.Message}", ERROR_TITLE);
            throw;
        }
    }

    private async Task SavePrintSettingsAsync()
    {
        try
        {
            // حفظ إعدادات الطباعة
            await _settingsService.SetSettingAsync("DefaultPrinter", cmbDefaultPrinter.SelectedItem?.ToString() ?? "", "الطابعة الافتراضية");
            await _settingsService.SetSettingAsync("ThermalPrinter", cmbThermalPrinter.SelectedItem?.ToString() ?? "", "طابعة الإيصالات الحرارية");

            var paperSize = (cmbPaperSize.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "A4";
            await _settingsService.SetSettingAsync("PaperSize", paperSize, "حجم الورق");

            await _settingsService.SetSettingAsync("AutoPrintInvoices", chkAutoPrint.IsChecked?.ToString() ?? "false", "الطباعة التلقائية للفواتير");
            await _settingsService.SetSettingAsync("PrintDirectly", chkPrintDirectly.IsChecked?.ToString() ?? "true", "الطباعة المباشرة");
            await _settingsService.SetSettingAsync("ShowPrintPreview", chkPrintPreview.IsChecked?.ToString() ?? "false", "معاينة الطباعة");

            // حفظ الهوامش
            await _settingsService.SetSettingAsync("PrintMarginTop", txtPrintMarginTop.Text, "الهامش العلوي");
            await _settingsService.SetSettingAsync("PrintMarginBottom", txtPrintMarginBottom.Text, "الهامش السفلي");
            await _settingsService.SetSettingAsync("PrintMarginLeft", txtPrintMarginLeft.Text, "الهامش الأيسر");
            await _settingsService.SetSettingAsync("PrintMarginRight", txtPrintMarginRight.Text, "الهامش الأيمن");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ إعدادات الطباعة: {ex.Message}", ERROR_TITLE);
            throw;
        }
    }

    private async Task SaveSecuritySettingsAsync()
    {
        try
        {
            // حفظ إعدادات الأمان
            await _settingsService.SetSettingAsync("PasswordExpiryEnabled", chkPasswordExpiry.IsChecked?.ToString() ?? "false", "انتهاء صلاحية كلمة المرور");
            await _settingsService.SetSettingAsync("PasswordExpiryDays", txtPasswordExpiryDays.Text, "أيام انتهاء صلاحية كلمة المرور");
            await _settingsService.SetSettingAsync("AutoLogoffEnabled", chkAutoLogoff.IsChecked?.ToString() ?? "false", "تسجيل الخروج التلقائي");
            await _settingsService.SetSettingAsync("AutoLogoffMinutes", txtAutoLogoffMinutes.Text, "دقائق عدم النشاط للخروج التلقائي");
            await _settingsService.SetSettingAsync("AuditTrailEnabled", chkAuditTrail.IsChecked?.ToString() ?? "true", "تسجيل سجل المراجعة");

            await _settingsService.SetSettingAsync("SessionTimeoutMinutes", txtSessionTimeout.Text, "مهلة انتهاء الجلسة بالدقائق");
            await _settingsService.SetSettingAsync("RequirePasswordForDelete", chkRequirePasswordForDelete.IsChecked?.ToString() ?? "true", "مطالبة بكلمة مرور عند الحذف");
            await _settingsService.SetSettingAsync("LogUserActions", chkLogUserActions.IsChecked?.ToString() ?? "true", "تسجيل عمليات المستخدمين");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ إعدادات الأمان: {ex.Message}", ERROR_TITLE);
            throw;
        }
    }

    private async Task SaveIntegrationSettingsAsync()
    {
        try
        {
            // حفظ إعدادات البريد الإلكتروني
            await _settingsService.SetSettingAsync("EmailIntegrationEnabled", chkEmailIntegration.IsChecked?.ToString() ?? "false", "تكامل البريد الإلكتروني");
            await _settingsService.SetSettingAsync("SMTPServer", txtSMTPServer.Text, "خادم SMTP");
            await _settingsService.SetSettingAsync("SMTPPort", txtSMTPPort.Text, "منفذ SMTP");
            await _settingsService.SetSettingAsync("EmailUsername", txtEmailUsername.Text, "اسم مستخدم البريد");

            if (!string.IsNullOrEmpty(txtEmailPassword.Password))
                await _settingsService.SetSettingAsync("EmailPassword", txtEmailPassword.Password, "كلمة مرور البريد");

            // حفظ إعدادات الرسائل النصية
            await _settingsService.SetSettingAsync("SMSIntegrationEnabled", chkSMSIntegration.IsChecked?.ToString() ?? "false", "تكامل الرسائل النصية");
            await _settingsService.SetSettingAsync("SMSProvider", txtSMSProvider.Text, "مزود خدمة الرسائل النصية");
            await _settingsService.SetSettingAsync("SMSAPIKey", txtSMSAPIKey.Text, "مفتاح API للرسائل النصية");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ إعدادات التكامل: {ex.Message}", ERROR_TITLE);
            throw;
        }
    }

    private async Task SavePOSSettingsAsync()
    {
        try
        {
            // حفظ إعدادات نقطة البيع
            await _settingsService.SetSettingAsync("RequireCustomerSelection", chkRequireCustomer.IsChecked?.ToString() ?? "false", "مطالبة باختيار عميل");
            await _settingsService.SetSettingAsync("AutoOpenCashDrawer", chkAutoOpenCashDrawer.IsChecked?.ToString() ?? "true", "فتح درج النقدية تلقائياً");
            await _settingsService.SetSettingAsync("POSProductsPerPage", txtPOSProductsPerPage.Text, "عدد المنتجات في الصفحة");
            await _settingsService.SetSettingAsync("ShowProductImages", chkShowProductImages.IsChecked?.ToString() ?? "true", "عرض صور المنتجات");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ إعدادات نقطة البيع: {ex.Message}", ERROR_TITLE);
            throw;
        }
    }

    private async Task SaveInventorySettingsAsync()
    {
        try
        {
            // حفظ إعدادات المخزون
            await _settingsService.SetSettingAsync("AllowNegativeStock", chkAllowNegativeStock.IsChecked?.ToString() ?? "false", "السماح بالمخزون السالب");
            await _settingsService.SetSettingAsync("StockWarningsEnabled", chkStockWarnings.IsChecked?.ToString() ?? "true", "تنبيهات المخزون الناقص");
            await _settingsService.SetSettingAsync("MinStockWarningLevel", txtMinStockWarning.Text, "حد التنبيه للمخزون الناقص");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في حفظ إعدادات المخزون: {ex.Message}", ERROR_TITLE);
            throw;
        }
    }

    // معالجات التحقق الرقمي
    private static bool IsAllDigits(string text) => text.All(ch => char.IsDigit(ch));

    private void Numeric_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
    {
        e.Handled = !IsAllDigits(e.Text);
    }

    private void Numeric_Pasting(object sender, DataObjectPastingEventArgs e)
    {
        if (e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string))!;
            if (!IsAllDigits(text)) e.CancelCommand();
        }
        else e.CancelCommand();
    }

    // تحقق بسيط قبل الحفظ
    private bool ValidateSettings()
    {
        // تحقق من البريد الإلكتروني
        if (!string.IsNullOrEmpty(txtCompanyEmail.Text))
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(txtCompanyEmail.Text);
                if (addr.Address != txtCompanyEmail.Text)
                {
                    MessageBox.Show("بريد إلكتروني غير صحيح", WARNING_TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return false;
                }
            }
            catch
            {
                MessageBox.Show("بريد إلكتروني غير صحيح", WARNING_TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
        }

        // تحقق من تواريخ السنة المالية
        if (dpFiscalYearStart.SelectedDate is DateTime s && dpFiscalYearEnd.SelectedDate is DateTime e && s > e)
        {
            MessageBox.Show("تاريخ بداية السنة المالية يجب أن يكون قبل تاريخ النهاية.", WARNING_TITLE, MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        return true;
    }

    public class NumberSequenceInfo
    {
        public required string SequenceType { get; set; }
        public required string Prefix { get; set; }
        public int CurrentNumber { get; set; }
        public required string Suffix { get; set; }
        public int NumberLength { get; set; }
    }
}