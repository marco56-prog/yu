using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using AccountingSystem.WPF.Helpers;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// خدمة الأمان والتدقيق مع دعم الصلاحيات وتسجيل العمليات
    /// </summary>
    public interface ISecurityAuditService
    {
        UserRole CurrentUserRole { get; }
        string CurrentUserName { get; }
        bool HasPermission(Permission permission);
        void LogUserAction(string action, string details = "");
        void StartIdleTimer();
        void ResetIdleTimer();
        void StopIdleTimer();
        Task<bool> AuthenticateUserAsync(string username, string password);
        event EventHandler<IdleLockEventArgs>? IdleLockTriggered;
        event EventHandler<UserActionEventArgs>? UserActionLogged;
    }

    public class SecurityAuditService : ISecurityAuditService, INotifyPropertyChanged
    {
        private const string ComponentName = "SecurityAuditService";
        private const int IdleTimeoutMinutes = 5;

        private readonly DispatcherTimer _idleTimer;
        private UserRole _currentUserRole;
        private string _currentUserName;
        private DateTime _lastActivity;

        public UserRole CurrentUserRole
        {
            get => _currentUserRole;
            private set => SetProperty(ref _currentUserRole, value);
        }

        public string CurrentUserName
        {
            get => _currentUserName;
            private set => SetProperty(ref _currentUserName, value ?? string.Empty);
        }

        public event EventHandler<IdleLockEventArgs>? IdleLockTriggered;
        public event EventHandler<UserActionEventArgs>? UserActionLogged;
        public event PropertyChangedEventHandler? PropertyChanged;

        public SecurityAuditService()
        {
            _currentUserRole = UserRole.Viewer; // افتراضي
            _currentUserName = Environment.UserName;
            _lastActivity = DateTime.Now;

            // إعداد مؤقت الخمول
            _idleTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(1) // فحص كل دقيقة
            };
            _idleTimer.Tick += IdleTimer_Tick;

            ComprehensiveLogger.LogSecurityOperation("تم تهيئة خدمة الأمان والتدقيق", ComponentName, CurrentUserName);
        }

        public bool HasPermission(Permission permission)
        {
            try
            {
                var hasPermission = CurrentUserRole switch
                {
                    UserRole.Admin => true, // الأدمن له صلاحية كاملة
                    UserRole.Manager => permission != Permission.SystemSettings && 
                                       permission != Permission.UserManagement,
                    UserRole.User => permission == Permission.ViewInvoices || 
                                    permission == Permission.CreateInvoices ||
                                    permission == Permission.EditDraftInvoices,
                    UserRole.Viewer => permission == Permission.ViewInvoices,
                    _ => false
                };

                if (!hasPermission)
                {
                    LogUserAction($"محاولة وصول مرفوضة للصلاحية: {permission}", 
                        $"المستخدم: {CurrentUserName}, الدور: {CurrentUserRole}");
                    
                    ComprehensiveLogger.LogSecurityOperation(
                        $"تم رفض الوصول للصلاحية {permission}", 
                        ComponentName, 
                        CurrentUserName);
                }

                return hasPermission;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"خطأ في فحص الصلاحية {permission}", ex, ComponentName);
                return false;
            }
        }

        public void LogUserAction(string action, string details = "")
        {
            try
            {
                var actionEvent = new UserActionEventArgs
                {
                    UserName = CurrentUserName,
                    UserRole = CurrentUserRole,
                    Action = action,
                    Details = details,
                    Timestamp = DateTime.Now,
                    IpAddress = GetLocalIPAddress(),
                    MachineName = Environment.MachineName
                };

                // حفظ في قاعدة البيانات (يمكن تنفيذه لاحقاً)
                // await SaveAuditLogAsync(actionEvent);

                // إطلاق الحدث
                UserActionLogged?.Invoke(this, actionEvent);

                // تسجيل في النظام
                ComprehensiveLogger.LogAuditOperation(action, ComponentName, CurrentUserName, details);

                // إعادة تعيين مؤقت الخمول
                ResetIdleTimer();
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل تسجيل العملية: {action}", ex, ComponentName);
            }
        }

        public void StartIdleTimer()
        {
            try
            {
                _lastActivity = DateTime.Now;
                _idleTimer.Start();
                
                ComprehensiveLogger.LogSecurityOperation("تم بدء مؤقت الخمول", ComponentName, CurrentUserName);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل بدء مؤقت الخمول", ex, ComponentName);
            }
        }

        public void ResetIdleTimer()
        {
            try
            {
                _lastActivity = DateTime.Now;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل إعادة تعيين مؤقت الخمول", ex, ComponentName);
            }
        }

        public void StopIdleTimer()
        {
            try
            {
                _idleTimer.Stop();
                ComprehensiveLogger.LogSecurityOperation("تم إيقاف مؤقت الخمول", ComponentName, CurrentUserName);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل إيقاف مؤقت الخمول", ex, ComponentName);
            }
        }

        public async Task<bool> AuthenticateUserAsync(string username, string password)
        {
            try
            {
                ComprehensiveLogger.LogSecurityOperation($"محاولة تسجيل دخول للمستخدم: {username}", ComponentName);

                // تحقق من بيانات المستخدم (يمكن تطويره لاحقاً)
                var isAuthenticated = await ValidateCredentialsAsync(username, password);

                if (isAuthenticated)
                {
                    CurrentUserName = username;
                    CurrentUserRole = await GetUserRoleAsync(username);
                    
                    LogUserAction("تسجيل دخول ناجح", $"المستخدم: {username}");
                    
                    ComprehensiveLogger.LogSecurityOperation(
                        $"تم تسجيل دخول ناجح للمستخدم: {username}", 
                        ComponentName, 
                        username);
                }
                else
                {
                    LogUserAction("محاولة تسجيل دخول فاشلة", $"المستخدم: {username}");
                    
                    ComprehensiveLogger.LogSecurityOperation(
                        $"فشل تسجيل دخول للمستخدم: {username}", 
                        ComponentName, 
                        username);
                }

                return isAuthenticated;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"خطأ في تسجيل الدخول للمستخدم: {username}", ex, ComponentName);
                return false;
            }
        }

        private async Task<bool> ValidateCredentialsAsync(string username, string password)
        {
            // تطبيق بسيط - يمكن تطويره لاحقاً للتحقق من قاعدة البيانات
            await Task.Delay(100); // محاكاة التحقق

            // افتراضيات للاختبار
            return username.ToLower() switch
            {
                "admin" => password == "admin123",
                "manager" => password == "manager123",
                "user" => password == "user123",
                _ => false
            };
        }

        private async Task<UserRole> GetUserRoleAsync(string username)
        {
            await Task.Delay(50); // محاكاة البحث في قاعدة البيانات

            return username.ToLower() switch
            {
                "admin" => UserRole.Admin,
                "manager" => UserRole.Manager,
                "user" => UserRole.User,
                _ => UserRole.Viewer
            };
        }

        private void IdleTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                var idleTime = DateTime.Now - _lastActivity;
                
                if (idleTime.TotalMinutes >= IdleTimeoutMinutes)
                {
                    TriggerIdleLock();
                }
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("خطأ في مؤقت الخمول", ex, ComponentName);
            }
        }

        private void TriggerIdleLock()
        {
            try
            {
                StopIdleTimer();
                
                var lockEvent = new IdleLockEventArgs
                {
                    UserName = CurrentUserName,
                    IdleTime = DateTime.Now - _lastActivity,
                    LockTime = DateTime.Now
                };

                LogUserAction("قفل تلقائي بسبب الخمول", $"مدة الخمول: {lockEvent.IdleTime.TotalMinutes:F1} دقيقة");

                IdleLockTriggered?.Invoke(this, lockEvent);

                ComprehensiveLogger.LogSecurityOperation(
                    $"تم قفل النظام تلقائياً بسبب الخمول", 
                    ComponentName, 
                    CurrentUserName);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل تنفيذ القفل التلقائي", ex, ComponentName);
            }
        }

        private static string GetLocalIPAddress()
        {
            try
            {
                var hostName = System.Net.Dns.GetHostName();
                var addresses = System.Net.Dns.GetHostAddresses(hostName);
                
                foreach (var address in addresses)
                {
                    if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return address.ToString();
                }
                
                return "Unknown";
            }
            catch
            {
                return "Unknown";
            }
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingField, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingField, value))
                return false;

            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    #region Enums and Event Args

    public enum UserRole
    {
        Viewer,    // عرض فقط
        User,      // مستخدم عادي
        Manager,   // مدير
        Admin      // مدير نظام
    }

    public enum Permission
    {
        ViewInvoices,
        CreateInvoices,
        EditDraftInvoices,
        EditPostedInvoices,
        DeleteInvoices,
        ViewReports,
        ManageCustomers,
        ManageProducts,
        ManageInventory,
        SystemSettings,
        UserManagement
    }

    public class UserActionEventArgs : EventArgs
    {
        public string UserName { get; set; } = string.Empty;
        public UserRole UserRole { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
    }

    public class IdleLockEventArgs : EventArgs
    {
        public string UserName { get; set; } = string.Empty;
        public TimeSpan IdleTime { get; set; }
        public DateTime LockTime { get; set; }
    }

    #endregion
}