using System;
using System.Globalization;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using AccountingSystem.Business;
using AccountingSystem.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views;

/// <summary>
/// نافذة إضافة/تعديل الكاشير — نسخة مُحسّنة مع معالجة أرقام عربية، فحص قوي، وتجزئة كلمة المرور.
/// متوافقة مع XAML المُحدَّث (لا تعتمد على Checked/Unchecked للخصومات، لأن التفعيل مربوط مباشرة بالـBinding).
/// </summary>
public partial class AddCashierWindow : Window
{
    private readonly ICashierService _cashierService;
    private readonly ISystemSettingsService _settingsService;
    private readonly ISecurityService _securityService;
    private readonly Cashier? _cashierToEdit;
    private readonly bool _isEditMode;

    public bool IsSuccess { get; private set; }

    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public AddCashierWindow(ICashierService cashierService, ISystemSettingsService settingsService, ISecurityService securityService, Cashier? cashierToEdit = null)
    {
        _cashierService = cashierService ?? throw new ArgumentNullException(nameof(cashierService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _securityService = securityService;
        _cashierToEdit = cashierToEdit;
        _isEditMode = cashierToEdit != null;

        InitializeComponent();
        _ = InitializeFormAsync();
    }

    private async Task InitializeFormAsync()
    {
        await LoadSystemSettingsAsync();
        InitializeForm();
    }

    private async Task LoadSystemSettingsAsync()
    {
        try
        {
            // تحميل إعدادات النظام لتطبيقها على النافذة
            var companyName = await _settingsService.GetCompanyNameAsync();
            this.Title = $"{this.Title} - {companyName}";

            // يمكن إضافة المزيد من الإعدادات هنا
        }
        catch (Exception)
        {
            // في حالة فشل تحميل الإعدادات، نستمر بالقيم الافتراضية
        }
    }

    private void InitializeForm()
    {
        Title = _isEditMode ? "تعديل الكاشير" : "إضافة كاشير جديد";
        btnSave.Content = _isEditMode ? "حفظ التعديلات" : "إضافة الكاشير";

        if (_isEditMode && _cashierToEdit != null)
        {
            LoadCashierData();
        }
        else
        {
            txtCashierCode.Text = GenerateNewCashierCode();
            dpHireDate.SelectedDate = DateTime.Now;
            chkIsActive.IsChecked = true;
        }
    }

    private void LoadCashierData()
    {
        if (_cashierToEdit == null) return;

        txtCashierCode.Text = _cashierToEdit.CashierCode;
        txtName.Text = _cashierToEdit.Name;
        txtPhone.Text = _cashierToEdit.Phone ?? string.Empty;
        txtEmail.Text = _cashierToEdit.Email ?? string.Empty;
        txtAddress.Text = _cashierToEdit.Address ?? string.Empty;
        dpHireDate.SelectedDate = _cashierToEdit.HireDate;
        txtSalary.Text = _cashierToEdit.Salary.ToString("F2", Invariant);
        cmbStatus.Text = _cashierToEdit.Status;
        txtUsername.Text = _cashierToEdit.Username ?? string.Empty;
        chkIsActive.IsChecked = _cashierToEdit.IsActive;

        chkCanApplyDiscounts.IsChecked = _cashierToEdit.CanApplyDiscounts;
        chkCanViewReports.IsChecked = _cashierToEdit.CanViewReports;
        chkCanAccessSettings.IsChecked = _cashierToEdit.CanAccessSettings;

        txtMaxDiscountPercent.Text = _cashierToEdit.MaxDiscountPercent.ToString("F2", Invariant);
        txtMaxDiscountAmount.Text = _cashierToEdit.MaxDiscountAmount.ToString("F2", Invariant);
    }

    private static string GenerateNewCashierCode()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        return $"CASH{timestamp[^6..]}";
    }

    // تحويل أرقام عربية/فارسي إلى إنجليزية + إزالة الفواصل العربية
    private static string NormalizeDigits(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "0";
        return s
            .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
            .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9')
            .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
            .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
            .Replace('٫', '.') // decimal arabic
            .Replace("٬", string.Empty) // group separator arabic
            .Replace(",", string.Empty) // remove western thousand sep
            .Trim();
    }

    private static bool TryParseDecimal(string? text, out decimal value)
    {
        text = NormalizeDigits(text);
        return decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, Invariant, out value);
    }

    private async void btnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!ValidateForm()) return;

        try
        {
            btnSave.IsEnabled = false;
            btnCancel.IsEnabled = false;
            btnSave.Content = "جاري الحفظ...";

            var cashier = _isEditMode ? _cashierToEdit! : new Cashier();

            cashier.CashierCode = txtCashierCode.Text.Trim();
            cashier.Name = txtName.Text.Trim();
            cashier.Phone = string.IsNullOrWhiteSpace(txtPhone.Text) ? null : txtPhone.Text.Trim();
            cashier.Email = string.IsNullOrWhiteSpace(txtEmail.Text) ? null : txtEmail.Text.Trim();
            cashier.Address = string.IsNullOrWhiteSpace(txtAddress.Text) ? null : txtAddress.Text.Trim();
            cashier.HireDate = dpHireDate.SelectedDate ?? DateTime.Now;

            TryParseDecimal(txtSalary.Text, out var salary);
            cashier.Salary = salary;

            cashier.Status = cmbStatus.Text;
            cashier.Username = string.IsNullOrWhiteSpace(txtUsername.Text) ? null : txtUsername.Text.Trim();
            cashier.IsActive = chkIsActive.IsChecked == true;

            cashier.CanApplyDiscounts = chkCanApplyDiscounts.IsChecked == true;
            cashier.CanViewReports = chkCanViewReports?.IsChecked == true;
            cashier.CanAccessSettings = chkCanAccessSettings?.IsChecked == true;

            TryParseDecimal(txtMaxDiscountPercent.Text, out var p);
            TryParseDecimal(txtMaxDiscountAmount.Text, out var a);
            cashier.MaxDiscountPercent = p;
            cashier.MaxDiscountAmount = a;

            // كلمة المرور: عند الإضافة مطلوبة لو اسم المستخدم موجود.
            if (!_isEditMode && !string.IsNullOrWhiteSpace(txtUsername.Text))
            {
                var pwd = txtPassword.Password;
                if (!string.IsNullOrWhiteSpace(pwd))
                    cashier.PasswordHash = HashPassword(pwd); // ملاحظة: استخدم مُولِّد Salt و خوارزمية أقوى في الإنتاج
            }
            else if (_isEditMode && !string.IsNullOrWhiteSpace(txtPassword.Password))
            {
                // دعم تعديل كلمة المرور عند تحرير المستخدم (اختياري)
                cashier.PasswordHash = HashPassword(txtPassword.Password);
            }

            bool success = _isEditMode
                ? await _cashierService.UpdateCashierAsync(cashier)
                : await _cashierService.CreateCashierAsync(cashier);

            if (success)
            {
                IsSuccess = true;
                MessageBox.Show(_isEditMode ? "تم تعديل الكاشير بنجاح!" : "تم إضافة الكاشير بنجاح!",
                                "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
                return;
            }

            ShowValidationError("فشل في حفظ بيانات الكاشير. تأكد من عدم تكرار الكود أو اسم المستخدم.");
        }
        catch (Exception ex)
        {
            ShowValidationError($"حدث خطأ أثناء الحفظ: {ex.Message}");
#if DEBUG
            System.Diagnostics.Debug.WriteLine(ex);
#endif
        }
        finally
        {
            btnSave.IsEnabled = true;
            btnCancel.IsEnabled = true;
            btnSave.Content = _isEditMode ? "حفظ التعديلات" : "إضافة الكاشير";
        }
    }

    private bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(txtCashierCode.Text))
            return ShowValidationError("كود الكاشير مطلوب", txtCashierCode);

        if (string.IsNullOrWhiteSpace(txtName.Text))
            return ShowValidationError("اسم الكاشير مطلوب", txtName);

        if (!TryParseDecimal(txtSalary.Text, out var salary) || salary < 0)
            return ShowValidationError("أدخل راتب صحيح", txtSalary);

        if (!TryParseDecimal(txtMaxDiscountPercent.Text, out var discountPercent) || discountPercent < 0 || discountPercent > 100)
            return ShowValidationError("أدخل نسبة خصم صحيحة (0-100%)", txtMaxDiscountPercent);

        if (!TryParseDecimal(txtMaxDiscountAmount.Text, out var discountAmount) || discountAmount < 0)
            return ShowValidationError("أدخل مبلغ خصم أقصى صحيح", txtMaxDiscountAmount);

        if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !IsValidEmail(txtEmail.Text))
            return ShowValidationError("أدخل بريد إلكتروني صحيح", txtEmail);

        if (!_isEditMode && !string.IsNullOrWhiteSpace(txtUsername.Text))
        {
            if (string.IsNullOrWhiteSpace(txtPassword.Password))
                return ShowValidationError("كلمة المرور مطلوبة عند إدخال اسم المستخدم", txtPassword);

            if (txtPassword.Password != txtConfirmPassword.Password)
                return ShowValidationError("كلمة المرور وتأكيدها غير متطابقتين", txtConfirmPassword);

            if (txtPassword.Password.Length < 6)
                return ShowValidationError("كلمة المرور يجب أن تكون على الأقل 6 أحرف", txtPassword);
        }

        return true;
    }

    private static bool IsValidEmail(string email)
    {
        try { return new MailAddress(email).Address == email; }
        catch { return false; }
    }

    private bool ShowValidationError(string message, Control? control = null)
    {
        MessageBox.Show(message, "خطأ في البيانات", MessageBoxButton.OK, MessageBoxImage.Warning);
        control?.Focus();
        return false;
    }

    private string HashPassword(string password)
    {
        // Using a secure password hasher from the business layer
        return _securityService.HashPassword(password);
    }

    // مخصّص لاختصارات Ctrl+S و Esc في الـXAML
    private void CanExecuteTrue(object? s, System.Windows.Input.CanExecuteRoutedEventArgs e) => e.CanExecute = true;

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    // تركت معالجات الخصم لتوافق الإصدارات القديمة — غير مستخدمة مع XAML الجديد
    private void chkCanApplyDiscounts_Checked(object sender, RoutedEventArgs e)
    {
        bool enabled = chkCanApplyDiscounts.IsChecked == true;
        txtMaxDiscountPercent.IsEnabled = enabled;
        txtMaxDiscountAmount.IsEnabled = enabled;
        if (!enabled) { txtMaxDiscountPercent.Text = "0"; txtMaxDiscountAmount.Text = "0"; }
    }

    private void chkCanApplyDiscounts_Unchecked(object sender, RoutedEventArgs e)
    {
        chkCanApplyDiscounts_Checked(sender, e);
    }
}
