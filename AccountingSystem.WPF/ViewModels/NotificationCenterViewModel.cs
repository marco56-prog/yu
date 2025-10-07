// File: NotificationCenterViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using AccountingSystem.Business;
using AccountingSystem.Models;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.WPF.Views;

namespace AccountingSystem.WPF.ViewModels
{
    /// <summary>
    /// ViewModel لمركز الإشعارات
    /// </summary>
    public class NotificationCenterViewModel : BaseViewModel
    {
        #region الحقول الخاصة

        private readonly ISmartNotificationService _notificationService;
        private readonly IServiceProvider _serviceProvider;
        private bool _isLoading;
        private string _currentFilter = "All";
        private ObservableCollection<Notification> _allNotifications = new();
        private ICollectionView _filteredNotificationsView;

        #endregion

        #region المنشئ

        public NotificationCenterViewModel(
            ISmartNotificationService notificationService,
            IServiceProvider serviceProvider)
        {
            _notificationService = notificationService;
            _serviceProvider = serviceProvider;

            // تهيئة المجموعة والرؤية المفلترة
            AllNotifications = new ObservableCollection<Notification>();
            _filteredNotificationsView = CollectionViewSource.GetDefaultView(AllNotifications);
            _filteredNotificationsView.Filter = FilterNotifications;

            // تابع تغيّرات العناصر لتحديث العدّادات تلقائيًا
            HookCollectionChanged(AllNotifications);

            // الأوامر
            RefreshCommand = new RelayCommand(async () => await LoadNotificationsAsync());
            MarkAllAsReadCommand = new RelayCommand(async () => await MarkAllAsReadAsync());
            FilterCommand = new RelayCommand<string?>(ApplyFilter);
            NotificationClickCommand = new RelayCommand<Notification?>(OnNotificationClick);
            MarkAsReadCommand = new RelayCommand<Notification?>(async n => await MarkAsReadAsync(n));
            DeleteNotificationCommand = new RelayCommand<Notification?>(async n => await DeleteNotificationAsync(n));

            // تحميل أوّل بيانات
            _ = LoadNotificationsAsync();
        }

        #endregion

        #region الخصائص العامة

        public ObservableCollection<Notification> AllNotifications
        {
            get => _allNotifications;
            set
            {
                if (SetProperty(ref _allNotifications, value))
                {
                    // إعادة تهيئة الـView عند تغيير المصدر
                    _filteredNotificationsView = CollectionViewSource.GetDefaultView(_allNotifications);
                    _filteredNotificationsView.Filter = FilterNotifications;

                    // إعادة ربط CollectionChanged
                    HookCollectionChanged(_allNotifications);

                    _filteredNotificationsView.Refresh();
                    RaiseCounters();
                }
            }
        }

        public ICollectionView FilteredNotifications => _filteredNotificationsView;

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string CurrentFilter
        {
            get => _currentFilter;
            set
            {
                if (SetProperty(ref _currentFilter, value))
                {
                    _filteredNotificationsView.Refresh();
                }
            }
        }

        public int TotalNotifications => AllNotifications.Count;
        public int UnreadCount => AllNotifications.Count(n => n.Status == NotificationStatus.Unread);
        public bool HasUnreadNotifications => UnreadCount > 0;

        #endregion

        #region الأوامر

        public ICommand RefreshCommand { get; }
        public ICommand MarkAllAsReadCommand { get; }
        public ICommand FilterCommand { get; }
        public ICommand NotificationClickCommand { get; }
        public ICommand MarkAsReadCommand { get; }
        public ICommand DeleteNotificationCommand { get; }

        #endregion

        #region الطرق العامة

        public async Task LoadNotificationsAsync()
        {
            try
            {
                if (IsLoading) return;
                IsLoading = true;

                var notifications = await _notificationService.GetAllNotificationsAsync() 
                                    ?? Enumerable.Empty<Notification>();

                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    AllNotifications.Clear();
                    foreach (var notification in notifications.OrderByDescending(n => n.CreatedDate))
                        AllNotifications.Add(notification);

                    _filteredNotificationsView.Refresh();
                    RaiseCounters();
                });
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تحميل الإشعارات: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        #endregion

        #region الطرق الخاصة

        private void HookCollectionChanged(ObservableCollection<Notification> collection)
        {
            // فك الاشتراك القديم إن وجد
            if (collection == null) return;

            // لضمان عدم الاشتراك المكرر، نفك ثم نعيد الاشتراك
            try
            {
                // لا نملك مرجعًا للاشتراك السابق بشكل منفصل؛
                // لكن بما أننا نعيّن مجموعة جديدة، يكفي الاشتراك في الجديدة
                collection.CollectionChanged += (_, __) =>
                {
                    // تحديث العدّادات وتجديد الفلتر عند أي تغيير
                    RaiseCounters();
                    _filteredNotificationsView?.Refresh();
                };
            }
            catch { /* تجاهُل آمن */ }
        }

        private void RaiseCounters()
        {
            OnPropertiesChanged(nameof(TotalNotifications), nameof(UnreadCount), nameof(HasUnreadNotifications));
        }

        private bool FilterNotifications(object obj)
        {
            if (obj is not Notification notification)
                return false;

            var filter = (CurrentFilter ?? "All").Trim().ToLowerInvariant();

            return filter switch
            {
                "all" => true,
                "unread" => notification.Status == NotificationStatus.Unread,
                "important" => notification.Priority == NotificationPriority.High,
                "system" => notification.Type == NotificationType.SystemAlert,
                "stock" => notification.Type == NotificationType.LowStock,
                _ => true
            };
        }

        private void ApplyFilter(string? filterType)
        {
            CurrentFilter = filterType ?? "All";
        }

        private async void OnNotificationClick(Notification? notification)
        {
            if (notification == null) return;

            try
            {
                // تحديد كمقروء
                if (notification.Status == NotificationStatus.Unread)
                {
                    await _notificationService.MarkAsReadAsync(notification.NotificationId, 0);
                    notification.Status = NotificationStatus.Read;
                    notification.ReadDate = DateTime.Now;

                    RaiseCounters();
                }

                // إجراء إضافي حسب نوع الإشعار
                if (!string.IsNullOrEmpty(notification.ActionUrl))
                {
                    await HandleNotificationAction(notification);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في معالجة الإشعار: {ex.Message}");
            }
        }

        private async Task MarkAsReadAsync(Notification? notification)
        {
            if (notification == null || notification.Status == NotificationStatus.Read) return;

            try
            {
                await _notificationService.MarkAsReadAsync(notification.NotificationId, 0);
                notification.Status = NotificationStatus.Read;
                notification.ReadDate = DateTime.Now;

                RaiseCounters();
                _filteredNotificationsView.Refresh();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تحديث حالة الإشعار: {ex.Message}");
            }
        }

        private async Task MarkAllAsReadAsync()
        {
            try
            {
                if (IsLoading) return;
                IsLoading = true;

                var unread = AllNotifications.Where(n => n.Status == NotificationStatus.Unread).ToList();
                foreach (var notification in unread)
                {
                    await _notificationService.MarkAsReadAsync(notification.NotificationId, 0);
                    notification.Status = NotificationStatus.Read;
                    notification.ReadDate = DateTime.Now;
                }

                RaiseCounters();
                _filteredNotificationsView.Refresh();

                ShowSuccessMessage("تم تحديد جميع الإشعارات كمقروءة");
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تحديث الإشعارات: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteNotificationAsync(Notification? notification)
        {
            if (notification == null) return;

            try
            {
                var result = MessageBox.Show(
                    "هل تريد حذف هذا الإشعار؟",
                    "تأكيد الحذف",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await _notificationService.DismissNotificationAsync(notification.NotificationId, 0);

                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        AllNotifications.Remove(notification);
                        RaiseCounters();
                        _filteredNotificationsView.Refresh();
                    });
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في حذف الإشعار: {ex.Message}");
            }
        }

        private async Task HandleNotificationAction(Notification notification)
        {
            try
            {
                // معالجة الإجراءات المختلفة حسب نوع الإشعار
                switch (notification.ActionUrl?.ToLowerInvariant())
                {
                    case "sales":
                        await OpenSalesWindow();
                        break;
                    case "inventory":
                        await OpenInventoryWindow();
                        break;
                    case "customers":
                        await OpenCustomersWindow();
                        break;
                    case "reports":
                        await OpenReportsWindow();
                        break;
                    default:
                        // عمل افتراضي
                        ShowInfoMessage($"الإشعار: {notification.Title}");
                        break;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في تنفيذ إجراء الإشعار: {ex.Message}");
            }
        }

        private async Task OpenSalesWindow()
        {
            try
            {
                var salesWindow = _serviceProvider.GetService<SalesInvoicesListWindow>();
                salesWindow?.Show();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في فتح نافذة المبيعات: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        private async Task OpenInventoryWindow()
        {
            try
            {
                var inventoryWindow = _serviceProvider.GetService<ProductsWindow>();
                inventoryWindow?.Show();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في فتح نافذة المخزون: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        private async Task OpenCustomersWindow()
        {
            try
            {
                var customersWindow = _serviceProvider.GetService<CustomersWindow>();
                customersWindow?.Show();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في فتح نافذة العملاء: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        private async Task OpenReportsWindow()
        {
            try
            {
                var reportsWindow = _serviceProvider.GetService<SalesReportsWindow>();
                reportsWindow?.Show();
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"خطأ في فتح نافذة التقارير: {ex.Message}");
            }
            await Task.CompletedTask;
        }

        private void ShowSuccessMessage(string message)
        {
            MessageBox.Show(message, "نجح",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ShowInfoMessage(string message)
        {
            MessageBox.Show(message, "معلومات",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ShowErrorMessage(string message)
        {
            MessageBox.Show(message, "خطأ",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        #endregion
    }
}
