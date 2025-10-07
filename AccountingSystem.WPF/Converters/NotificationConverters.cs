using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Converters
{
    public class BoolToNotificationStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isRead && Application.Current != null)
            {
                var styleName = isRead ? "NotificationItemStyle" : "UnreadNotificationStyle";
                var res = Application.Current.TryFindResource(styleName);
                if (res != null) return res;
            }
            return Application.Current?.TryFindResource("NotificationItemStyle") ?? Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PriorityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NotificationPriority priority)
            {
                return priority switch
                {
                    NotificationPriority.Low => new SolidColorBrush(Colors.Gray),
                    NotificationPriority.Medium => new SolidColorBrush(Colors.Orange),
                    NotificationPriority.High => new SolidColorBrush(Colors.Red),
                    _ => new SolidColorBrush(Colors.Blue)
                };
            }
            return new SolidColorBrush(Colors.Blue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility != Visibility.Visible;
            }
            return false;
        }
    }

    public class NotificationTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is NotificationType type)
            {
                return type switch
                {
                    NotificationType.LowStock => "📦",
                    NotificationType.OverduePayment => "⚠️",
                    NotificationType.ProductExpiry => "❌",
                    NotificationType.SystemAlert => "🔔",
                    NotificationType.SalesTarget => "🎯",
                    NotificationType.CustomerDebt => "💳",
                    NotificationType.SupplierPayment => "💸",
                    NotificationType.BackupReminder => "💾",
                    NotificationType.UserActivity => "👤",
                    NotificationType.SystemMaintenance => "🔧",
                    _ => "ℹ️"
                };
            }
            return "ℹ️";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}