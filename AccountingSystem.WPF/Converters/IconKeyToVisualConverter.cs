using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace AccountingSystem.WPF.Converters
{
    /// <summary>
    /// محول تحويل مفتاح الأيقونة إلى عنصر مرئي
    /// </summary>
    public class IconKeyToVisualConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string iconKey && !string.IsNullOrEmpty(iconKey))
            {
                try
                {
                    // محاولة العثور على المورد في الموارد العامة للتطبيق
                    if (Application.Current.Resources.Contains(iconKey))
                    {
                        return Application.Current.Resources[iconKey];
                    }

                    // إنشاء أيقونة افتراضية بسيطة إذا لم يتم العثور على المورد
                    return CreateDefaultIcon(iconKey);
                }
                catch
                {
                    return CreateDefaultIcon(iconKey);
                }
            }

            return null;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// إنشاء أيقونة افتراضية بسيطة
        /// </summary>
        private object CreateDefaultIcon(string iconKey)
        {
            // إرجاع رمز نصي بسيط حسب نوع الأيقونة
            return iconKey switch
            {
                "IconSales" or "IconInvoice" => "🧾",
                "IconInvoices" => "📋",
                "IconReturn" => "↩️",
                "IconPurchase" => "🛒",
                "IconProduct" => "📦",
                "IconCategory" => "🏷️",
                "IconWarehouse" => "🏪",
                "IconCustomers" => "👥",
                "IconSuppliers" => "🏢",
                "IconReports" => "📊",
                "IconSettings" => "⚙️",
                _ => "📄"
            };
        }
    }


}