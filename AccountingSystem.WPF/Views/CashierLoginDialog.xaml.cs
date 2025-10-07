using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AccountingSystem.Business;
using AccountingSystem.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// منيو تسجيل دخول الكاشير
    /// </summary>
    public partial class CashierLoginDialog : Window
    {
        private readonly ICashierService? _cashierService;
        private bool _isBusy;

        /// <summary>
        /// الكاشير الذي تم تسجيل دخوله بنجاح (إن وُجد)
        /// </summary>
        public Cashier? LoggedInCashier { get; private set; }

        /// <summary>
        /// مُنشئ افتراضي (للمصمم/الاختبارات). يفضَّل استخدام المُنشئ الذي يستقبل IServiceProvider في التطبيق الفعلي.
        /// </summary>
        public CashierLoginDialog()
        {
            InitializeComponent();
            Loaded += (_, __) => txtUsername.Focus();
        }

        /// <summary>
        /// مُنشئ يعتمد على DI للحصول على ICashierService
        /// </summary>
        public CashierLoginDialog(IServiceProvider serviceProvider) : this()
        {
            _cashierService = serviceProvider.GetService<ICashierService>();
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            await TryLoginAsync();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void txtUsername_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // انتقل لحقل كلمة المرور أو نفّذ دخول لو موجودة
                if (string.IsNullOrWhiteSpace(txtPassword.Password))
                    txtPassword.Focus();
                else
                    await TryLoginAsync();
            }
            else if (e.Key == Key.Escape)
            {
                btnCancel_Click(sender, e);
            }
        }

        private async void txtPassword_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await TryLoginAsync();
            }
            else if (e.Key == Key.Escape)
            {
                btnCancel_Click(sender, e);
            }
        }

        private async Task TryLoginAsync()
        {
            if (_isBusy) return;

            var username = txtUsername.Text?.Trim() ?? string.Empty;
            var password = txtPassword.Password ?? string.Empty;

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("من فضلك أدخل اسم المستخدم", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtUsername.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("من فضلك أدخل كلمة المرور", "تنبيه", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPassword.Focus();
                return;
            }

            try
            {
                SetBusy(true);

                if (_cashierService == null)
                {
                    // لو الخدمة غير متاحة (تشغيل سريع/مصمم)، ارفض الدخول بشكل صريح
                    MessageBox.Show("خدمة التحقق من بيانات الدخول غير متاحة حالياً.", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // استبدل اسم الدالة حسب الخدمة عندك لو مختلف:
                // مثال بدائل شائعة: ValidateLoginAsync / AuthenticateCashierAsync
                var cashier = await _cashierService.AuthenticateCashierAsync(username, password);

                if (cashier != null && cashier.IsActive)
                {
                    LoggedInCashier = cashier;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("بيانات الدخول غير صحيحة أو الحساب غير مُفعّل.", "رفض الدخول", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtPassword.Clear();
                    txtPassword.Focus();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"حدث خطأ أثناء التحقق من بيانات الدخول:\n{ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            btnLogin.IsEnabled = !busy;
            btnCancel.IsEnabled = !busy;
            btnLogin.Content = busy ? "جارٍ التحقق..." : "تسجيل دخول";
        }
    }
}
