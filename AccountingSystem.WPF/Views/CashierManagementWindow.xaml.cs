using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using AccountingSystem.Business;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// نافذة إدارة الكاشير
    /// </summary>
    public partial class CashierManagementWindow : Window
    {
        private readonly ICashierService _cashierService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IServiceProvider? _serviceProvider;
        private readonly ObservableCollection<Cashier> _cashiers;
        private readonly ICollectionView _view;

        public CashierManagementWindow(ICashierService cashierService, IUnitOfWork unitOfWork, IServiceProvider? serviceProvider = null)
        {
            _cashierService = cashierService;
            _unitOfWork = unitOfWork;
            _serviceProvider = serviceProvider;
            _cashiers = new ObservableCollection<Cashier>();
            
            InitializeComponent();

            // Data binding
            dgCashiers.ItemsSource = _cashiers;

            // View/Filter
            _view = CollectionViewSource.GetDefaultView(_cashiers);
            _view.Filter = FilterCashiers;

            Loaded += CashierManagementWindow_Loaded;
        }

        private async void CashierManagementWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadCashiersAsync();
        }

        private async Task LoadCashiersAsync()
        {
            try
            {
                var cashiers = await _cashierService.GetAllCashiersAsync();
                _cashiers.Clear();
                foreach (var cashier in cashiers.OrderBy(c => c.Name))
                    _cashiers.Add(cashier);

                AccountingSystem.WPF.Helpers.CollectionViewHelper.SafeRefresh(_view);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل بيانات الكاشير: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool FilterCashiers(object obj)
        {
            if (obj is not Cashier c) return false;

            var q = (txtSearch?.Text ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(q)) return true;

            bool Contains(string? s) => (s ?? string.Empty).ToLowerInvariant().Contains(q);

            return Contains(c.Name) ||
                   Contains(c.CashierCode) ||
                   Contains(c.Phone) ||
                   Contains(c.Email) ||
                   Contains(c.Username);
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            AccountingSystem.WPF.Helpers.CollectionViewHelper.SafeRefresh(_view);
        }

        private async void btnAddCashier_Click(object sender, RoutedEventArgs e)
        {
            var settingsService = App.ServiceProvider.GetRequiredService<ISystemSettingsService>();
                var addWindow = new AddCashierWindow(_cashierService, settingsService);
            if (addWindow.ShowDialog() == true)
            {
                await LoadCashiersAsync();
            }
        }

        private async void btnEditCashier_Click(object sender, RoutedEventArgs e)
        {
            if (dgCashiers.SelectedItem is not Cashier selectedCashier)
            {
                MessageBox.Show("يرجى اختيار كاشير للتعديل", "تنبيه", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // مهم: مرر نسخة جديدة لو نافذة التعديل بتعدل الكائن مباشرة
            var settingsService = App.ServiceProvider.GetRequiredService<ISystemSettingsService>();
                var editWindow = new AddCashierWindow(_cashierService, settingsService, selectedCashier);
            if (editWindow.ShowDialog() == true)
            {
                await LoadCashiersAsync();
            }
        }

        private async void btnDeleteCashier_Click(object sender, RoutedEventArgs e)
        {
            if (dgCashiers.SelectedItem is not Cashier selectedCashier)
            {
                MessageBox.Show("يرجى اختيار كاشير للحذف", "تنبيه", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var result = MessageBox.Show(
                $"هل أنت متأكد من حذف الكاشير '{selectedCashier.Name}'؟",
                "تأكيد الحذف", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            try
            {
                var deleted = await _cashierService.DeleteCashierAsync(selectedCashier.Id);
                if (deleted)
                {
                    MessageBox.Show("تم حذف الكاشير بنجاح", "نجح", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCashiersAsync();
                }
                else
                {
                    MessageBox.Show("فشل في حذف الكاشير", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حذف الكاشير: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await LoadCashiersAsync();
        }

        private async void btnActivateCashier_Click(object sender, RoutedEventArgs e)
        {
            if (dgCashiers.SelectedItem is not Cashier selectedCashier)
            {
                MessageBox.Show("يرجى اختيار كاشير لتحديث حالته", "تنبيه", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                selectedCashier.IsActive = !selectedCashier.IsActive;
                var updated = await _cashierService.UpdateCashierAsync(selectedCashier);
                if (updated)
                {
                    var status = selectedCashier.IsActive ? "تم تفعيل" : "تم إلغاء تفعيل";
                    MessageBox.Show($"{status} الكاشير بنجاح", "نجح", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadCashiersAsync();
                }
                else
                {
                    MessageBox.Show("فشل في تحديث حالة الكاشير", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحديث حالة الكاشير: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnPermissions_Click(object sender, RoutedEventArgs e)
        {
            if (dgCashiers.SelectedItem is not Cashier selectedCashier)
            {
                MessageBox.Show("يرجى اختيار كاشير لتعديل صلاحياته", "تنبيه", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            MessageBox.Show("إدارة الصلاحيات - قيد التطوير", "معلومات", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private async void dgCashiers_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                btnEditCashier_Click(sender, new RoutedEventArgs());
            });
        }

        private void btnOpenPOS_Click(object sender, RoutedEventArgs e)
        {
            if (dgCashiers.SelectedItem is not Cashier selectedCashier)
            {
                MessageBox.Show("يرجى اختيار كاشير لفتح نقطة البيع", "تنبيه", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!selectedCashier.IsActive)
            {
                MessageBox.Show("لا يمكن فتح نقطة البيع لكاشير غير مفعل", "تنبيه", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (_serviceProvider == null)
                {
                    MessageBox.Show("خدمات النظام غير متاحة", "خطأ", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var posService = _serviceProvider.GetRequiredService<IPosService>();
                var discountService = _serviceProvider.GetRequiredService<IDiscountService>();
                
                var posWindow = new POSWindow(_unitOfWork, _cashierService, posService, discountService)
                {
                    Owner = this
                };
                posWindow.Show();
                posWindow.Activate();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نقطة البيع: {ex.Message}", "خطأ", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
