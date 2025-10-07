using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AccountingSystem.WPF.Services;
using AccountingSystem.WPF.Commands;

namespace AccountingSystem.WPF.ViewModels
{
    /// <summary>
    /// نموذج عرض القائمة الجانبية
    /// </summary>
    public class SidebarViewModel : INotifyPropertyChanged
    {
        private readonly INavigationService _navigationService;

        /// <summary>
        /// قائمة عناصر القائمة الجانبية
        /// </summary>
        public ObservableCollection<MenuItemVm> MenuItems { get; } = new();

        public SidebarViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            BuildMenu();
        }

        /// <summary>
        /// بناء القائمة الجانبية برمجياً
        /// </summary>
        private void BuildMenu()
        {
            // قسم المبيعات
            var salesSection = new MenuItemVm
            {
                Title = "المبيعات",
                IconKey = "IconSales",
                IsGroup = true,
                IsExpanded = true,
                ToggleCommand = new RelayCommand<MenuItemVm>(ToggleSection)
            };

            salesSection.Children.Add(new MenuItemVm
            {
                Title = "فاتورة مبيعات جديدة",
                IconKey = "IconInvoice",
                Shortcut = "Ctrl+I",
                Target = "SalesInvoice",
                Command = new RelayCommand(() => _navigationService.NavigateTo("SalesInvoice"))
            });

            salesSection.Children.Add(new MenuItemVm
            {
                Title = "قائمة فواتير المبيعات",
                IconKey = "IconInvoices",
                Shortcut = "Ctrl+L",
                Target = "SalesInvoicesList",
                Command = new RelayCommand(() => _navigationService.NavigateTo("SalesInvoicesList"))
            });

            salesSection.Children.Add(new MenuItemVm
            {
                Title = "مرتجعات المبيعات",
                IconKey = "IconReturn",
                Target = "SalesReturn",
                Command = new RelayCommand(() => _navigationService.NavigateTo("SalesReturn"))
            });

            // قسم المشتريات
            var purchaseSection = new MenuItemVm
            {
                Title = "المشتريات",
                IconKey = "IconPurchase",
                IsGroup = true,
                IsExpanded = true,
                ToggleCommand = new RelayCommand<MenuItemVm>(ToggleSection)
            };

            purchaseSection.Children.Add(new MenuItemVm
            {
                Title = "فاتورة شراء جديدة",
                IconKey = "IconPurchase",
                Shortcut = "Ctrl+P",
                Target = "PurchaseInvoice",
                Command = new RelayCommand(() => _navigationService.NavigateTo("PurchaseInvoice"))
            });

            purchaseSection.Children.Add(new MenuItemVm
            {
                Title = "مرتجعات الشراء",
                IconKey = "IconReturn",
                Target = "PurchaseReturn",
                Command = new RelayCommand(() => _navigationService.NavigateTo("PurchaseReturn"))
            });

            // قسم المخزون
            var inventorySection = new MenuItemVm
            {
                Title = "المخزون والمنتجات",
                IconKey = "IconProduct",
                IsGroup = true,
                IsExpanded = false,
                ToggleCommand = new RelayCommand<MenuItemVm>(ToggleSection)
            };

            inventorySection.Children.Add(new MenuItemVm
            {
                Title = "إدارة المنتجات",
                IconKey = "IconProduct",
                Shortcut = "Ctrl+M",
                Target = "Products",
                Command = new RelayCommand(() => _navigationService.NavigateTo("Products"))
            });

            inventorySection.Children.Add(new MenuItemVm
            {
                Title = "إدارة الفئات",
                IconKey = "IconCategory",
                Target = "Categories",
                Command = new RelayCommand(() => _navigationService.NavigateTo("Categories"))
            });

            inventorySection.Children.Add(new MenuItemVm
            {
                Title = "إدارة المخازن",
                IconKey = "IconWarehouse",
                Target = "Warehouse",
                Command = new RelayCommand(() => _navigationService.NavigateTo("Warehouse"))
            });

            // قسم العملاء والموردين
            var contactsSection = new MenuItemVm
            {
                Title = "العملاء والموردين",
                IconKey = "IconCustomers",
                IsGroup = true,
                IsExpanded = false,
                ToggleCommand = new RelayCommand<MenuItemVm>(ToggleSection)
            };

            contactsSection.Children.Add(new MenuItemVm
            {
                Title = "إدارة العملاء",
                IconKey = "IconCustomers",
                Shortcut = "Ctrl+U",
                Target = "Customers",
                Command = new RelayCommand(() => _navigationService.NavigateTo("Customers"))
            });

            contactsSection.Children.Add(new MenuItemVm
            {
                Title = "إدارة الموردين",
                IconKey = "IconSuppliers",
                Target = "Suppliers",
                Command = new RelayCommand(() => _navigationService.NavigateTo("Suppliers"))
            });

            // العناصر المفردة
            var reportsItem = new MenuItemVm
            {
                Title = "التقارير",
                IconKey = "IconReports",
                Shortcut = "Ctrl+R",
                Target = "Reports",
                Command = new RelayCommand(() => _navigationService.NavigateTo("Reports"))
            };

            var settingsItem = new MenuItemVm
            {
                Title = "الإعدادات",
                IconKey = "IconSettings",
                Shortcut = "Ctrl+S",
                Target = "Settings",
                Command = new RelayCommand(() => _navigationService.NavigateTo("Settings"))
            };

            // إضافة جميع العناصر للقائمة الرئيسية
            MenuItems.Add(salesSection);
            MenuItems.Add(purchaseSection);
            MenuItems.Add(inventorySection);
            MenuItems.Add(contactsSection);
            MenuItems.Add(reportsItem);
            MenuItems.Add(settingsItem);
        }

        /// <summary>
        /// تبديل حالة توسع القسم
        /// </summary>
        private void ToggleSection(MenuItemVm? section)
        {
            if (section != null)
            {
                section.IsExpanded = !section.IsExpanded;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}