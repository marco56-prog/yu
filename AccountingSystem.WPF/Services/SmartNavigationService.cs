using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AccountingSystem.Data;
using AccountingSystem.Models;
using AccountingSystem.WPF.Helpers;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// خدمة التنقل الذكي بين الفواتير مع دعم F5/F6 وإدارة الحالة
    /// </summary>
    public interface ISmartNavigationService
    {
        Task<NavigationResult> NavigateToNextInvoiceAsync(int currentInvoiceId);
        Task<NavigationResult> NavigateToPreviousInvoiceAsync(int currentInvoiceId);
        Task<NavigationResult> NavigateToInvoiceAsync(int invoiceId);
        Task<List<int>> GetInvoiceIdsAsync();
        Task<bool> ConfirmUnsavedChangesAsync(string context = "التنقل");
        void RegisterHotKeys(Window window);
        bool CanNavigate { get; }
        event EventHandler<NavigationEventArgs>? NavigationRequested;
        void TriggerNavigation(NavigationEventArgs args);
    }

    public class SmartNavigationService : ISmartNavigationService
    {
        private const string ComponentName = "SmartNavigationService";
        private readonly AccountingDbContext _context;
        private List<int> _cachedInvoiceIds = new();
        private DateTime _lastCacheUpdate = DateTime.MinValue;
        private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

        public bool CanNavigate { get; private set; } = true;

        public event EventHandler<NavigationEventArgs>? NavigationRequested;

        public SmartNavigationService(AccountingDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        public void TriggerNavigation(NavigationEventArgs args)
        {
            NavigationRequested?.Invoke(this, args);
        }

        public async Task<NavigationResult> NavigateToNextInvoiceAsync(int currentInvoiceId)
        {
            try
            {
                ComprehensiveLogger.LogUIOperation("بدء التنقل للفاتورة التالية", ComponentName, $"الفاتورة الحالية: {currentInvoiceId}");

                var invoiceIds = await GetInvoiceIdsAsync();
                var currentIndex = invoiceIds.IndexOf(currentInvoiceId);

                if (currentIndex == -1)
                {
                    return NavigationResult.Error("لا يمكن تحديد موقع الفاتورة الحالية");
                }

                if (currentIndex >= invoiceIds.Count - 1)
                {
                    return NavigationResult.EndReached("هذه آخر فاتورة");
                }

                var nextInvoiceId = invoiceIds[currentIndex + 1];
                var invoice = await LoadInvoiceAsync(nextInvoiceId);

                if (invoice == null)
                {
                    return NavigationResult.Error("لا يمكن تحميل الفاتورة التالية");
                }

                ComprehensiveLogger.LogUIOperation("تم التنقل للفاتورة التالية بنجاح", ComponentName, 
                    $"من {currentInvoiceId} إلى {nextInvoiceId}");

                return NavigationResult.Success(invoice, currentIndex + 1, invoiceIds.Count);
            }
            catch (Exception ex)
            {
                var errorMsg = $"خطأ في التنقل للفاتورة التالية: {ex.Message}";
                ComprehensiveLogger.LogError(errorMsg, ex, ComponentName);
                return NavigationResult.Error(errorMsg);
            }
        }

        public async Task<NavigationResult> NavigateToPreviousInvoiceAsync(int currentInvoiceId)
        {
            try
            {
                ComprehensiveLogger.LogUIOperation("بدء التنقل للفاتورة السابقة", ComponentName, $"الفاتورة الحالية: {currentInvoiceId}");

                var invoiceIds = await GetInvoiceIdsAsync();
                var currentIndex = invoiceIds.IndexOf(currentInvoiceId);

                if (currentIndex == -1)
                {
                    return NavigationResult.Error("لا يمكن تحديد موقع الفاتورة الحالية");
                }

                if (currentIndex <= 0)
                {
                    return NavigationResult.EndReached("هذه أول فاتورة");
                }

                var previousInvoiceId = invoiceIds[currentIndex - 1];
                var invoice = await LoadInvoiceAsync(previousInvoiceId);

                if (invoice == null)
                {
                    return NavigationResult.Error("لا يمكن تحميل الفاتورة السابقة");
                }

                ComprehensiveLogger.LogUIOperation("تم التنقل للفاتورة السابقة بنجاح", ComponentName, 
                    $"من {currentInvoiceId} إلى {previousInvoiceId}");

                return NavigationResult.Success(invoice, currentIndex - 1, invoiceIds.Count);
            }
            catch (Exception ex)
            {
                var errorMsg = $"خطأ في التنقل للفاتورة السابقة: {ex.Message}";
                ComprehensiveLogger.LogError(errorMsg, ex, ComponentName);
                return NavigationResult.Error(errorMsg);
            }
        }

        public async Task<NavigationResult> NavigateToInvoiceAsync(int invoiceId)
        {
            try
            {
                ComprehensiveLogger.LogUIOperation("بدء التنقل لفاتورة محددة", ComponentName, $"معرف الفاتورة: {invoiceId}");

                var invoice = await LoadInvoiceAsync(invoiceId);
                if (invoice == null)
                {
                    return NavigationResult.Error($"لا يمكن العثور على الفاتورة رقم {invoiceId}");
                }

                var invoiceIds = await GetInvoiceIdsAsync();
                var position = invoiceIds.IndexOf(invoiceId) + 1;

                ComprehensiveLogger.LogUIOperation("تم التنقل للفاتورة المحددة بنجاح", ComponentName, 
                    $"الفاتورة: {invoice.InvoiceNumber}");

                return NavigationResult.Success(invoice, position, invoiceIds.Count);
            }
            catch (Exception ex)
            {
                var errorMsg = $"خطأ في التنقل للفاتورة {invoiceId}: {ex.Message}";
                ComprehensiveLogger.LogError(errorMsg, ex, ComponentName);
                return NavigationResult.Error(errorMsg);
            }
        }

        public async Task<List<int>> GetInvoiceIdsAsync()
        {
            try
            {
                // Use cache if available and not expired
                if (_cachedInvoiceIds.Any() && DateTime.Now - _lastCacheUpdate < _cacheExpiry)
                {
                    return _cachedInvoiceIds;
                }

                // Refresh cache
                _cachedInvoiceIds = await _context.SalesInvoices
                    .AsNoTracking()
                    .OrderBy(i => i.SalesInvoiceId)
                    .Select(i => i.SalesInvoiceId)
                    .ToListAsync();

                _lastCacheUpdate = DateTime.Now;

                ComprehensiveLogger.LogDataOperation("تم تحديث قائمة معرفات الفواتير", ComponentName, 
                    $"عدد الفواتير: {_cachedInvoiceIds.Count}");

                return _cachedInvoiceIds;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل تحميل قائمة معرفات الفواتير", ex, ComponentName);
                return new List<int>();
            }
        }

        public async Task<bool> ConfirmUnsavedChangesAsync(string context = "التنقل")
        {
            try
            {
                var result = MessageBox.Show(
                    $"هناك تغييرات غير محفوظة. هل تريد حفظها قبل {context}؟",
                    "تأكيد الحفظ",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                ComprehensiveLogger.LogUIOperation($"مستخدم تأكيد التغييرات غير المحفوظة: {result}", ComponentName, context);

                return result switch
                {
                    MessageBoxResult.Yes => await RequestSaveAsync(),
                    MessageBoxResult.No => true,
                    MessageBoxResult.Cancel => false,
                    _ => false
                };
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("خطأ في تأكيد التغييرات غير المحفوظة", ex, ComponentName);
                return false;
            }
        }

        public void RegisterHotKeys(Window window)
        {
            try
            {
                if (window == null) return;

                // Clear existing bindings for these keys
                var existingF5 = window.InputBindings.OfType<KeyBinding>().FirstOrDefault(kb => kb.Key == Key.F5);
                var existingF6 = window.InputBindings.OfType<KeyBinding>().FirstOrDefault(kb => kb.Key == Key.F6);

                if (existingF5 != null) window.InputBindings.Remove(existingF5);
                if (existingF6 != null) window.InputBindings.Remove(existingF6);

                // Register F5 for Previous
                var previousBinding = new KeyBinding
                {
                    Key = Key.F5,
                    Command = new SmartNavigationCommand(this, NavigationDirection.Previous)
                };
                window.InputBindings.Add(previousBinding);

                // Register F6 for Next
                var nextBinding = new KeyBinding
                {
                    Key = Key.F6,
                    Command = new SmartNavigationCommand(this, NavigationDirection.Next)
                };
                window.InputBindings.Add(nextBinding);

                ComprehensiveLogger.LogUIOperation("تم تسجيل اختصارات التنقل الذكي", ComponentName, 
                    "F5: السابق، F6: التالي");
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل تسجيل اختصارات التنقل", ex, ComponentName);
            }
        }

        private async Task<SalesInvoice?> LoadInvoiceAsync(int invoiceId)
        {
            try
            {
                return await _context.SalesInvoices
                    .AsNoTracking()
                    .Include(i => i.Customer)
                    .Include(i => i.Items).ThenInclude(item => item.Product)
                    .Include(i => i.Items).ThenInclude(item => item.Unit)
                    .FirstOrDefaultAsync(i => i.SalesInvoiceId == invoiceId);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل تحميل الفاتورة {invoiceId}", ex, ComponentName);
                return null;
            }
        }

        private async Task<bool> RequestSaveAsync()
        {
            try
            {
                // Fire event to request save from the UI
                var args = new NavigationEventArgs { Action = NavigationAction.RequestSave };
                NavigationRequested?.Invoke(this, args);
                
                // Wait for the result (this is a simplified implementation)
                // In a real scenario, you'd use a more sophisticated callback mechanism
                await Task.Delay(100); // Give UI time to process

                return args.SaveResult;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل طلب الحفظ", ex, ComponentName);
                return false;
            }
        }

        public void InvalidateCache()
        {
            _cachedInvoiceIds.Clear();
            _lastCacheUpdate = DateTime.MinValue;
            ComprehensiveLogger.LogDataOperation("تم إلغاء تحديث قائمة الفواتير", ComponentName);
        }

        public void SetNavigationEnabled(bool enabled)
        {
            CanNavigate = enabled;
            ComprehensiveLogger.LogUIOperation($"تم {(enabled ? "تفعيل" : "تعطيل")} التنقل", ComponentName);
        }
    }

    #region Supporting Classes

    public class NavigationResult
    {
        public bool IsSuccess { get; private set; }
        public string Message { get; private set; } = string.Empty;
        public SalesInvoice? Invoice { get; private set; }
        public int Position { get; private set; }
        public int TotalCount { get; private set; }
        public NavigationResultType Type { get; private set; }

        private NavigationResult() { }

        public static NavigationResult Success(SalesInvoice invoice, int position, int totalCount)
        {
            return new NavigationResult
            {
                IsSuccess = true,
                Invoice = invoice,
                Position = position,
                TotalCount = totalCount,
                Type = NavigationResultType.Success,
                Message = $"فاتورة {invoice.InvoiceNumber} ({position} من {totalCount})"
            };
        }

        public static NavigationResult Error(string message)
        {
            return new NavigationResult
            {
                IsSuccess = false,
                Message = message,
                Type = NavigationResultType.Error
            };
        }

        public static NavigationResult EndReached(string message)
        {
            return new NavigationResult
            {
                IsSuccess = false,
                Message = message,
                Type = NavigationResultType.EndReached
            };
        }
    }

    public enum NavigationResultType
    {
        Success,
        Error,
        EndReached
    }

    public enum NavigationDirection
    {
        Previous,
        Next
    }

    public enum NavigationAction
    {
        RequestSave,
        NavigatePrevious,
        NavigateNext
    }

    public class NavigationEventArgs : EventArgs
    {
        public NavigationAction Action { get; set; }
        public bool SaveResult { get; set; }
        public int? TargetInvoiceId { get; set; }
    }

    public class SmartNavigationCommand : ICommand
    {
        private readonly ISmartNavigationService _navigationService;
        private readonly NavigationDirection _direction;

        public SmartNavigationCommand(ISmartNavigationService navigationService, NavigationDirection direction)
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _direction = direction;
        }

        public bool CanExecute(object? parameter) => _navigationService.CanNavigate;

        public async void Execute(object? parameter)
        {
            try
            {
                var args = new NavigationEventArgs
                {
                    Action = _direction == NavigationDirection.Previous 
                        ? NavigationAction.NavigatePrevious 
                        : NavigationAction.NavigateNext
                };

                _navigationService.TriggerNavigation(args);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل تنفيذ أمر التنقل {_direction}", ex, "SmartNavigationCommand");
            }
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    #endregion
}