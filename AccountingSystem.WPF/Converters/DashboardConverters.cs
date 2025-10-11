using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;

namespace AccountingSystem.WPF.Converters;

/// <summary>
/// محول لون التحذيرات حسب حالة المخزون
/// </summary>
public class LowStockColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? new SolidColorBrush(Colors.Orange) : new SolidColorBrush(Colors.Green);
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// محول لون الفواتير المتأخرة
/// </summary>
public class OverdueColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? new SolidColorBrush(Colors.Red) : new SolidColorBrush(Colors.Green);
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// محول إخفاء العنصر عند الصفر
/// </summary>
public class ZeroToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// محول عكس البوليان
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return !boolValue;
        }
        return false;
    }
}

/// <summary>
/// محول تاريخ آخر تحديث
/// </summary>
public class LastRefreshConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalMinutes < 1)
                return "تم التحديث الآن";

            if (timeSpan.TotalMinutes < 60)
                return $"تم التحديث منذ {timeSpan.Minutes} دقيقة";

            if (timeSpan.TotalHours < 24)
                return $"تم التحديث منذ {timeSpan.Hours} ساعة";

            return $"تم التحديث في {dateTime:MM/dd HH:mm}";
        }

        return "لم يتم التحديث بعد";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// محول أولوية الإشعارات إلى لون
/// </summary>
public class NotificationPriorityToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AccountingSystem.Models.NotificationPriority priority)
        {
            return priority switch
            {
                AccountingSystem.Models.NotificationPriority.Critical => new SolidColorBrush(Colors.Red),
                AccountingSystem.Models.NotificationPriority.Urgent => new SolidColorBrush(Colors.OrangeRed),
                AccountingSystem.Models.NotificationPriority.High => new SolidColorBrush(Colors.Orange),
                AccountingSystem.Models.NotificationPriority.Medium => new SolidColorBrush(Colors.Blue),
                AccountingSystem.Models.NotificationPriority.Low => new SolidColorBrush(Colors.Green),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }

        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// محول حالة الإشعار إلى نص
/// </summary>
public class NotificationStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is AccountingSystem.Models.NotificationStatus status)
        {
            return status switch
            {
                AccountingSystem.Models.NotificationStatus.Unread => "غير مقروء",
                AccountingSystem.Models.NotificationStatus.Read => "مقروء",
                AccountingSystem.Models.NotificationStatus.Acknowledged => "تم الاطلاع",
                AccountingSystem.Models.NotificationStatus.Dismissed => "تم الإغلاق",
                AccountingSystem.Models.NotificationStatus.Archived => "مؤرشف",
                _ => "غير معروف"
            };
        }

        return "غير معروف";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// محول العملة إلى تنسيق مع الرمز
/// </summary>
public class CurrencyConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal amount)
        {
            return $"{amount:N0} ج.م";
        }

        if (value is double doubleAmount)
        {
            return $"{doubleAmount:N0} ج.م";
        }

        if (value is int intAmount)
        {
            return $"{intAmount:N0} ج.م";
        }

        return "0 ج.م";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string text)
        {
            var cleanText = text.Replace("ج.م", "").Replace(",", "").Trim();
            if (decimal.TryParse(cleanText, out decimal result))
            {
                return result;
            }
        }

        return 0m;
    }
}