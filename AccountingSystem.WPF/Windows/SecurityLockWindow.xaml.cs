using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.ComponentModel;
using System.Windows.Media;
using AccountingSystem.WPF.Services;
using AccountingSystem.WPF.Helpers;

namespace AccountingSystem.WPF.Windows
{
    /// <summary>
    /// شاشة قفل النظام مع دعم RTL والتسجيل
    /// </summary>
    public partial class SecurityLockWindow : Window, INotifyPropertyChanged
    {
        private const string ComponentName = "SecurityLockWindow";
        
        private string _username = string.Empty;
        private string _statusMessage = "النظام مقفل - الرجاء إدخال كلمة المرور";
        private bool _isUnlocking = false;
        private int _attemptCount = 0;
        private const int MaxAttempts = 3;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public bool IsUnlocking
        {
            get => _isUnlocking;
            set => SetProperty(ref _isUnlocking, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public SecurityLockWindow(string lockedUserName)
        {
            InitializeComponent();
            DataContext = this;
            
            Username = lockedUserName;
            
            // إعداد النافذة
            SetupWindow();
            
            // تسجيل العملية
            ComprehensiveLogger.LogSecurityOperation(
                $"تم عرض شاشة القفل للمستخدم: {lockedUserName}", 
                ComponentName, 
                lockedUserName);
        }

        private void SetupWindow()
        {
            try
            {
                // تعطيل إغلاق النافذة
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                Topmost = true;
                WindowState = WindowState.Maximized;
                
                // منع Alt+F4
                KeyDown += SecurityLockWindow_KeyDown;
                
                // تركيز على حقل كلمة المرور
                Loaded += (s, e) => PasswordBox.Focus();
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل إعداد نافذة القفل", ex, ComponentName);
            }
        }

        private void SecurityLockWindow_KeyDown(object sender, KeyEventArgs e)
        {
            // منع Alt+F4 و Ctrl+Alt+Del
            if ((e.Key == Key.F4 && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt) ||
                (e.Key == Key.Delete && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control && 
                 (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt))
            {
                e.Handled = true;
            }
            
            // Enter لإلغاء القفل
            if (e.Key == Key.Enter)
            {
                UnlockButton_Click(this, new RoutedEventArgs());
            }
        }

        private async void UnlockButton_Click(object sender, RoutedEventArgs e)
        {
            if (IsUnlocking) return;

            try
            {
                IsUnlocking = true;
                StatusMessage = "جاري التحقق...";

                var password = PasswordBox.Password;
                
                if (string.IsNullOrWhiteSpace(password))
                {
                    StatusMessage = "الرجاء إدخال كلمة المرور";
                    PasswordBox.Focus();
                    return;
                }

                // التحقق من كلمة المرور (يمكن تطويره لاحقاً)
                var isValid = await ValidatePasswordAsync(Username, password);

                if (isValid)
                {
                    ComprehensiveLogger.LogSecurityOperation(
                        $"تم إلغاء قفل النظام بنجاح للمستخدم: {Username}", 
                        ComponentName, 
                        Username);

                    StatusMessage = "تم إلغاء القفل بنجاح";
                    
                    // إغلاق النافذة مع نتيجة إيجابية
                    DialogResult = true;
                    Close();
                }
                else
                {
                    _attemptCount++;
                    
                    ComprehensiveLogger.LogSecurityOperation(
                        $"محاولة إلغاء قفل فاشلة رقم {_attemptCount} للمستخدم: {Username}", 
                        ComponentName, 
                        Username);

                    if (_attemptCount >= MaxAttempts)
                    {
                        StatusMessage = $"تم استنفاد المحاولات المسموحة ({MaxAttempts})";
                        UnlockButton.IsEnabled = false;
                        
                        ComprehensiveLogger.LogSecurityOperation(
                            $"تم تعطيل إلغاء القفل بعد {MaxAttempts} محاولات فاشلة", 
                            ComponentName, 
                            Username);
                    }
                    else
                    {
                        var remainingAttempts = MaxAttempts - _attemptCount;
                        StatusMessage = $"كلمة مرور خاطئة - المحاولات المتبقية: {remainingAttempts}";
                    }
                    
                    PasswordBox.Clear();
                    PasswordBox.Focus();
                }
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("خطأ في عملية إلغاء القفل", ex, ComponentName);
                StatusMessage = "حدث خطأ في النظام";
            }
            finally
            {
                IsUnlocking = false;
            }
        }

        private static async System.Threading.Tasks.Task<bool> ValidatePasswordAsync(string username, string password)
        {
            // محاكاة التحقق من كلمة المرور
            await System.Threading.Tasks.Task.Delay(1000);
            
            // تطبيق بسيط للاختبار
            return username.ToLower() switch
            {
                "admin" => password == "admin123",
                "manager" => password == "manager123",
                "user" => password == "user123",
                _ => password == "default"
            };
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingField, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (System.Collections.Generic.EqualityComparer<T>.Default.Equals(backingField, value))
                return false;

            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}