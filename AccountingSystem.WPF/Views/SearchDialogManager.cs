using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using AccountingSystem.Models;
using AccountingSystem.WPF.Models;

namespace AccountingSystem.WPF.Views
{
    /// <summary>
    /// وحدة تحكم متكاملة لإدارة نوافذ البحث في نظام المحاسبة
    /// </summary>
    public static class SearchDialogManager
    {
        /// <summary>
        /// عرض نافذة بحث العملاء
        /// </summary>
        public static Customer? ShowCustomerSearch(Window parent, List<Customer> customers)
        {
            try
            {
                var dialog = new CustomerSearchDialog(customers) { Owner = parent };
                return dialog.ShowDialog() == true ? dialog.SelectedCustomer : null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح بحث العملاء: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// عرض نافذة بحث المنتجات
        /// </summary>
        public static ProductSearchItem? ShowProductSearch(Window parent, List<Product> products)
        {
            try
            {
                var productItems = products.Select(p => ProductSearchItem.Create(
                    p.ProductId,
                    p.ProductCode ?? $"P{p.ProductId:000}",
                    p.ProductName,
                    p.MainUnit?.UnitName ?? "قطعة",
                    p.SalePrice,
                    p.CurrentStock)).ToList();

                var dialog = new ProductSearchDialog(productItems) { Owner = parent };
                return dialog.ShowDialog() == true ? dialog.Selected : null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح بحث المنتجات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// عرض نافذة بحث المستودعات
        /// </summary>
        public static Warehouse? ShowWarehouseSearch(Window parent, List<Warehouse> warehouses)
        {
            try
            {
                var dialog = new WarehouseSearchDialog(warehouses) { Owner = parent };
                return dialog.ShowDialog() == true ? dialog.SelectedWarehouse : null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح بحث المستودعات: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// عرض نافذة بحث المندوبين
        /// </summary>
        public static Representative? ShowRepresentativeSearch(Window parent, List<Representative> representatives)
        {
            try
            {
                var dialog = new RepresentativeSearchDialog(representatives) { Owner = parent };
                return dialog.ShowDialog() == true ? dialog.SelectedRepresentative : null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح بحث المندوبين: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// عرض نافذة إدخال الصنف
        /// </summary>
        public static SalesInvoiceItem? ShowItemEntry(Window parent, List<Product> products, List<Customer> customers, int customerId = 0)
        {
            try
            {
                var dialog = new SimpleItemEntryDialog(products, customers, customerId) { Owner = parent };
                return dialog.ShowDialog() == true ? dialog.ResultItem : null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في فتح نافذة إدخال الصنف: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }

        /// <summary>
        /// إنشاء بيانات تجريبية للمستودعات
        /// </summary>
        public static List<Warehouse> CreateTestWarehouses()
        {
            return new List<Warehouse>
            {
                new Warehouse { WarehouseId = 1, WarehouseCode = "W001", WarehouseName = "المستودع الرئيسي", Address = "القاهرة", ManagerName = "أحمد محمد" },
                new Warehouse { WarehouseId = 2, WarehouseCode = "W002", WarehouseName = "مستودع الإسكندرية", Address = "الإسكندرية", ManagerName = "محمود علي" },
                new Warehouse { WarehouseId = 3, WarehouseCode = "W003", WarehouseName = "مستودع الجيزة", Address = "الجيزة", ManagerName = "فاطمة أحمد" }
            };
        }

        /// <summary>
        /// إنشاء بيانات تجريبية للمندوبين
        /// </summary>
        public static List<Representative> CreateTestRepresentatives()
        {
            return new List<Representative>
            {
                new Representative { RepresentativeId = 1, RepresentativeCode = "R001", RepresentativeName = "خالد محمد سالم", Phone = "01012345678", CommissionRate = 2.5m },
                new Representative { RepresentativeId = 2, RepresentativeCode = "R002", RepresentativeName = "نورا أحمد إبراهيم", Phone = "01098765432", CommissionRate = 3.0m },
                new Representative { RepresentativeId = 3, RepresentativeCode = "R003", RepresentativeName = "محمد علي حسن", Phone = "01055555555", CommissionRate = 2.0m }
            };
        }
    }
}