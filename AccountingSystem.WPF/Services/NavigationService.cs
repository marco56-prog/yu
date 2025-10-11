using System;
using System.Collections.Generic;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// تنفيذ خدمة التنقل بين الشاشات
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Type> _routes = new();

        public NavigationService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            RegisterRoutes();
        }

        /// <summary>
        /// تسجيل جميع المسارات المتوفرة في النظام
        /// </summary>
        private void RegisterRoutes()
        {
            // المبيعات
            RegisterRoute("SalesInvoice", typeof(Views.SalesInvoiceWindow));
            RegisterRoute("SalesInvoicesList", typeof(Views.SalesInvoicesListWindow));
            RegisterRoute("SalesReturn", typeof(Views.SalesReturnWindow));

            // المشتريات
            RegisterRoute("PurchaseInvoice", typeof(Views.PurchaseInvoiceWindow));
            RegisterRoute("PurchaseReturn", typeof(Views.PurchaseReturnWindow));

            // المخزون والمنتجات
            RegisterRoute("Products", typeof(Views.ProductsWindow));
            RegisterRoute("Categories", typeof(Views.CategoriesWindow));
            // RegisterRoute("Warehouse", typeof(Views.WarehouseWindow)); // Will be implemented later

            // العملاء والموردين
            RegisterRoute("Customers", typeof(Views.CustomersWindow));
            RegisterRoute("Suppliers", typeof(Views.SuppliersWindow));

            // النظام
            RegisterRoute("Reports", typeof(Views.ReportsWindow));
            RegisterRoute("Settings", typeof(Views.SettingsWindow));
            RegisterRoute("CashierManagement", typeof(Views.CashierManagementWindow));
        }

        /// <summary>
        /// تسجيل مسار جديد
        /// </summary>
        private void RegisterRoute(string routeName, Type windowType)
        {
            _routes[routeName] = windowType;
        }

        public void RegisterWindow<T>(string routeName) where T : Window
        {
            _routes[routeName] = typeof(T);
        }

        public void NavigateTo(string routeName)
        {
            try
            {
                if (_routes.TryGetValue(routeName, out var windowType))
                {
                    var window = CreateWindow(windowType);
                    window?.Show();
                }
                else
                {
                    // إنشاء نافذة وهمية للاختبار
                    CreateStubWindow(routeName)?.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح النافذة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public bool? ShowDialog(string routeName)
        {
            try
            {
                if (_routes.TryGetValue(routeName, out var windowType))
                {
                    var window = CreateWindow(windowType);
                    return window?.ShowDialog();
                }
                else
                {
                    // إنشاء حوار وهمي للاختبار
                    return CreateStubWindow(routeName)?.ShowDialog();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح الحوار: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        public void CloseWindow(Window window)
        {
            window?.Close();
        }

        /// <summary>
        /// إنشاء نافذة من النوع المحدد
        /// </summary>
        private Window? CreateWindow(Type windowType)
        {
            try
            {
                // محاولة إنشاء النافذة من DI أولاً
                if (_serviceProvider.GetService(windowType) is Window window)
                {
                    return window;
                }

                // إنشاء النافذة باستخدام Activator كخيار احتياطي
                return Activator.CreateInstance(windowType) as Window;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// إنشاء نافذة وهمية للاختبار عندما لا تكون النافذة المطلوبة متوفرة
        /// </summary>
        private Window CreateStubWindow(string routeName)
        {
            var stubWindow = new Window
            {
                Title = GetArabicTitle(routeName),
                Width = 800,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                FlowDirection = FlowDirection.RightToLeft,
                FontFamily = new System.Windows.Media.FontFamily("Segoe UI, Tahoma"),
                Content = new System.Windows.Controls.TextBlock
                {
                    Text = $"شاشة {GetArabicTitle(routeName)}\n\nهذه نافذة تجريبية - سيتم تطوير المحتوى لاحقاً",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontSize = 18,
                    TextAlignment = TextAlignment.Center
                }
            };

            return stubWindow;
        }

        /// <summary>
        /// الحصول على العنوان العربي للمسار
        /// </summary>
        private string GetArabicTitle(string routeName)
        {
            return routeName switch
            {
                "SalesInvoice" => "فاتورة مبيعات جديدة",
                "SalesInvoicesList" => "قائمة فواتير المبيعات",
                "SalesReturn" => "مرتجعات المبيعات",
                "PurchaseInvoice" => "فاتورة شراء جديدة",
                "PurchaseReturn" => "مرتجعات الشراء",
                "Products" => "إدارة المنتجات",
                "Categories" => "إدارة الفئات",
                "Warehouse" => "إدارة المخازن",
                "Customers" => "إدارة العملاء",
                "Suppliers" => "إدارة الموردين",
                "Reports" => "التقارير",
                "Settings" => "الإعدادات",
                _ => routeName
            };
        }
    }
}