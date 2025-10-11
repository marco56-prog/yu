using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.Business;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views
{
    public partial class LoginWindow : Window
    {
        private readonly ISecurityService _securityService;

        public LoginWindow(ISecurityService securityService)
        {
            // التأكد من أننا على UI Thread مع STA
            if (!Application.Current?.Dispatcher.CheckAccess() == true)
            {
                throw new InvalidOperationException("LoginWindow must be created on the main UI thread with STA apartment state");
            }

            InitializeComponent();
            _securityService = securityService;

            // تعيين القيم الافتراضية للاختبار
            txtUsername.Text = "admin";
            txtPassword.Password = "admin";

            // التركيز على زر تسجيل الدخول
            Loaded += (s, e) => btnLogin.Focus();

            // إظهار رسالة ترحيب
            ShowMessage("البيانات محملة افتراضياً - اضغط تسجيل الدخول", true);
        }

        public User? LoggedInUser { get; private set; }
        public bool LoginSuccessful { get; private set; }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowMessage("جاري المعالجة...", true);

                // التحقق من البيانات
                if (string.IsNullOrWhiteSpace(txtUsername.Text))
                {
                    ShowMessage("يرجى إدخال اسم المستخدم");
                    txtUsername.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtPassword.Password))
                {
                    ShowMessage("يرجى إدخال كلمة المرور");
                    txtPassword.Focus();
                    return;
                }

                // تعطيل الأزرار أثناء المعالجة
                btnLogin.IsEnabled = false;
                btnCancel.IsEnabled = false;
                btnLogin.Content = "جاري التحقق...";

                // إضافة تأخير صغير لإظهار التحميل
                await System.Threading.Tasks.Task.Delay(500);

                // محاولة تسجيل الدخول
                var loginRequest = new LoginRequest
                {
                    UserName = txtUsername.Text.Trim(),
                    Password = txtPassword.Password,
                    RememberMe = chkRememberMe.IsChecked ?? false
                };

                var result = await _securityService.LoginAsync(loginRequest);

                if (result.IsSuccess && result.User != null)
                {
                    LoggedInUser = result.User;
                    LoginSuccessful = true;

                    ShowMessage("تم تسجيل الدخول بنجاح! جاري فتح النظام...", true);
                    await System.Threading.Tasks.Task.Delay(500);

                    DialogResult = true;
                    Close();
                }
                else
                {
                    ShowMessage($"فشل تسجيل الدخول: {result.Message ?? "بيانات الدخول غير صحيحة"}");
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (System.Exception ex)
            {
                ShowMessage($"خطأ في تسجيل الدخول: {ex.Message}");
                System.Windows.MessageBox.Show($"تفاصيل الخطأ:\n{ex}", "خطأ في النظام",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            finally
            {
                // إعادة تفعيل الأزرار
                btnLogin.IsEnabled = true;
                btnCancel.IsEnabled = true;
                btnLogin.Content = "تسجيل الدخول";
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            LoginSuccessful = false;
            DialogResult = false;
            Close();
        }

        private void ShowMessage(string message, bool isSuccess = false)
        {
            lblMessage.Text = message;
            lblMessage.Foreground = isSuccess ?
                System.Windows.Media.Brushes.Green :
                System.Windows.Media.Brushes.Red;
            lblMessage.Visibility = Visibility.Visible;
        }
    }
}
