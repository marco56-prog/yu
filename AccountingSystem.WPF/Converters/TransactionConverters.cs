using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Converters;

public sealed class TransactionTypeToArabicConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is TransactionType t ? t switch
        {
            TransactionType.Income   => "دخل",
            TransactionType.Expense  => "مصروف",
            TransactionType.Transfer => "تحويل",
            _ => value.ToString() ?? ""
        } : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class AmountBrushByTypeConverter : IValueConverter
{
    private static readonly Brush Green = Brushes.Green;
    private static readonly Brush Red   = Brushes.Red;
    private static readonly Brush Black = Brushes.Black;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is TransactionType t ? t switch
        {
            TransactionType.Income  => Green,
            TransactionType.Expense => Red,
            _ => Black
        } : Black;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}