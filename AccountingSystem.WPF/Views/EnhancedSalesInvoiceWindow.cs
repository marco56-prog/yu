using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using AccountingSystem.Models;
using AccountingSystem.WPF.Helpers;
using AccountingSystem.WPF.Services;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// Enhanced Sales Invoice Window with modern features
    /// </summary>
    public partial class EnhancedSalesInvoiceWindow : Window
    {
        private readonly ISmartNavigationService _navigationService;
        private readonly IPerformanceOptimizationService _performanceService;
        private readonly IAutoCalculationsEngine _calculationsEngine;
        private readonly IThemeIntegrationService _themeService;

        public EnhancedSalesInvoiceWindow(
            IServiceProvider serviceProvider,
            ISmartNavigationService navigationService,
            IPerformanceOptimizationService performanceService,
            IAutoCalculationsEngine calculationsEngine,
            IThemeIntegrationService themeService) : base()
        {
            _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
            _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
            _calculationsEngine = calculationsEngine ?? throw new ArgumentNullException(nameof(calculationsEngine));
            _themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

            InitializeComponent();
            InitializeServices();
        }

        private void InitializeServices()
        {
            try
            {
                // تسجيل اختصارات التنقل الذكي
                _navigationService.RegisterHotKeys(this);

                // استماع لأحداث التنقل
                _navigationService.NavigationRequested += OnNavigationRequested;

                // استماع لتغيير الثيم
                _themeService.ThemeChanged += OnThemeChanged;

                // استماع لتغيير الحسابات
                _calculationsEngine.CalculationChanged += OnCalculationChanged;

                ComprehensiveLogger.LogUIOperation("تم تهيئة النافذة المحسنة بنجاح", "EnhancedSalesInvoiceWindow");
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل تهيئة النافذة المحسنة", ex, "EnhancedSalesInvoiceWindow");
                MessageBox.Show($"خطأ في تهيئة النافذة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnNavigationRequested(object? sender, NavigationEventArgs e)
        {
            try
            {
                switch (e.Action)
                {
                    case NavigationAction.NavigatePrevious:
                        await HandlePreviousNavigation();
                        break;
                    case NavigationAction.NavigateNext:
                        await HandleNextNavigation();
                        break;
                    case NavigationAction.RequestSave:
                        e.SaveResult = await HandleSaveRequest();
                        break;
                }
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل معالجة طلب التنقل", ex, "EnhancedSalesInvoiceWindow");
                MessageBox.Show($"خطأ في التنقل: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async Task HandlePreviousNavigation()
        {
            if (HasUnsavedChanges())
            {
                if (!await _navigationService.ConfirmUnsavedChangesAsync("التنقل للفاتورة السابقة"))
                    return;
            }

            var currentId = GetCurrentInvoiceId();
            if (currentId.HasValue)
            {
                var result = await _navigationService.NavigateToPreviousInvoiceAsync(currentId.Value);
                await HandleNavigationResult(result);
            }
        }

        private async Task HandleNextNavigation()
        {
            if (HasUnsavedChanges())
            {
                if (!await _navigationService.ConfirmUnsavedChangesAsync("التنقل للفاتورة التالية"))
                    return;
            }

            var currentId = GetCurrentInvoiceId();
            if (currentId.HasValue)
            {
                var result = await _navigationService.NavigateToNextInvoiceAsync(currentId.Value);
                await HandleNavigationResult(result);
            }
        }

        private async Task<bool> HandleSaveRequest()
        {
            try
            {
                // Implementation would depend on your existing save logic
                // This is a placeholder
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل حفظ الفاتورة من التنقل", ex, "EnhancedSalesInvoiceWindow");
                return false;
            }
        }

        private async Task HandleNavigationResult(NavigationResult result)
        {
            try
            {
                if (result.IsSuccess && result.Invoice != null)
                {
                    // Load invoice to form
                    await LoadInvoiceToFormAsync(result.Invoice);
                    
                    // Update status
                    UpdateNavigationStatus(result);
                    
                    ComprehensiveLogger.LogUIOperation(
                        $"تم التنقل بنجاح للفاتورة {result.Invoice.InvoiceNumber}", 
                        "EnhancedSalesInvoiceWindow");
                }
                else
                {
                    ShowMessage(result.Message, result.Type == NavigationResultType.EndReached 
                        ? MessageBoxImage.Information 
                        : MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل معالجة نتيجة التنقل", ex, "EnhancedSalesInvoiceWindow");
            }
        }

        private async Task LoadInvoiceToFormAsync(SalesInvoice invoice)
        {
            // Implementation would load the invoice data to your form controls
            // This is a placeholder
            await Task.CompletedTask;
        }

        private void UpdateNavigationStatus(NavigationResult result)
        {
            // Update status bar or other UI elements
            var statusMessage = $"فاتورة {result.Position} من {result.TotalCount}";
            // Update your status display here
        }

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            try
            {
                // Handle theme change
                ComprehensiveLogger.LogUIOperation(
                    $"تم تغيير الثيم من {e.PreviousTheme?.Name} إلى {e.NewTheme?.Name}", 
                    "EnhancedSalesInvoiceWindow");
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل معالجة تغيير الثيم", ex, "EnhancedSalesInvoiceWindow");
            }
        }

        private void OnCalculationChanged(object? sender, CalculationChangedEventArgs e)
        {
            try
            {
                // Handle calculation changes
                if (!e.IsValid)
                {
                    ComprehensiveLogger.LogBusinessOperation(
                        "تحذير: حسابات غير صحيحة", 
                        $"نوع الحساب: {e.CalculationType}", 
                        isSuccess: false);
                }
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل معالجة تغيير الحسابات", ex, "EnhancedSalesInvoiceWindow");
            }
        }

        private int? GetCurrentInvoiceId()
        {
            // Implementation would return the current invoice ID
            // This is a placeholder
            return null;
        }

        private bool HasUnsavedChanges()
        {
            // Implementation would check for unsaved changes
            // This is a placeholder
            return false;
        }

        private void ShowMessage(string message, MessageBoxImage icon)
        {
            MessageBox.Show(message, "تنبيه", MessageBoxButton.OK, icon);
        }

        protected override void OnClosed(EventArgs e)
        {
            try
            {
                // تنظيف الموارد
                if (_navigationService != null)
                    _navigationService.NavigationRequested -= OnNavigationRequested;

                if (_themeService != null)
                    _themeService.ThemeChanged -= OnThemeChanged;

                if (_calculationsEngine != null)
                    _calculationsEngine.CalculationChanged -= OnCalculationChanged;

                ComprehensiveLogger.LogUIOperation("تم تنظيف موارد النافذة المحسنة", "EnhancedSalesInvoiceWindow");
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("فشل تنظيف موارد النافذة", ex, "EnhancedSalesInvoiceWindow");
            }
            finally
            {
                base.OnClosed(e);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("خطأ في إغلاق النافذة", ex, "EnhancedSalesInvoiceWindow");
                MessageBox.Show($"خطأ في إغلاق النافذة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}