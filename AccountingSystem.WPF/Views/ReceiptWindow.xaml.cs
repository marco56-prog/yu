using System.Windows;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows.Media;
using AccountingSystem.Models;

namespace AccountingSystem.WPF.Views;

/// <summary>
/// نافذة طباعة الإيصال
/// </summary>
public partial class ReceiptWindow : Window
{
    private readonly POSTransaction _transaction;
    private readonly decimal _amountPaid;
    private readonly decimal _change;
    private readonly string _cashierName;

    public ReceiptWindow(POSTransaction transaction, decimal amountPaid, decimal change, string cashierName)
    {
        InitializeComponent();
        _transaction = transaction;
        _amountPaid = amountPaid;
        _change = change;
        _cashierName = cashierName;

        LoadReceiptData();
    }

    private void LoadReceiptData()
    {
        // Header information
        lblReceiptNumber.Content = $"إيصال رقم: {_transaction.TransactionNumber}";
        lblDate.Content = $"التاريخ: {_transaction.TransactionDate:yyyy-MM-dd HH:mm}";
        lblCashier.Content = $"الكاشير: {_cashierName}";

        // Transaction items
        foreach (var item in _transaction.Items)
        {
            var itemRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 2, 0, 2)
            };

            itemRow.Children.Add(new TextBlock
            {
                Text = $"منتج {item.ProductId}", // Use ProductId since no ProductName property
                Width = 150,
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            itemRow.Children.Add(new TextBlock
            {
                Text = $"{item.Quantity}x",
                Width = 40,
                TextAlignment = TextAlignment.Center
            });

            itemRow.Children.Add(new TextBlock
            {
                Text = $"{item.UnitPrice:F2}",
                Width = 60,
                TextAlignment = TextAlignment.Right
            });

            itemRow.Children.Add(new TextBlock
            {
                Text = $"{item.LineTotal:F2}",
                Width = 80,
                TextAlignment = TextAlignment.Right,
                FontWeight = FontWeights.Bold
            });

            pnlItems.Children.Add(itemRow);
        }

        // Totals
        lblSubTotal.Content = $"{_transaction.Subtotal:F2}";
        lblDiscount.Content = $"{_transaction.DiscountAmount:F2}";
        lblTotal.Content = $"{_transaction.Total:F2}";
        lblAmountPaid.Content = $"{_amountPaid:F2}";
        lblChange.Content = $"{_change:F2}";

        // Payment method
        lblPaymentMethod.Content = $"طريقة الدفع: {_transaction.PaymentMethod}";
    }

    private void btnPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            PrintDialog printDialog = new PrintDialog();
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintDocument(CreateReceiptDocument().DocumentPaginator, "إيصال نقطة البيع");
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private IDocumentPaginatorSource CreateReceiptDocument()
    {
        var doc = new FlowDocument();
        doc.PageHeight = 800;
        doc.PageWidth = 600;
        doc.PagePadding = new Thickness(50);
        doc.FontFamily = new FontFamily("Arial");
        doc.FontSize = 12;

        // Header
        var headerParagraph = new Paragraph();
        headerParagraph.Inlines.Add(new Bold(new Run("شركة نظام المحاسبة")));
        headerParagraph.FontSize = 18;
        headerParagraph.TextAlignment = TextAlignment.Center;
        doc.Blocks.Add(headerParagraph);

        var addressParagraph = new Paragraph();
        addressParagraph.Inlines.Add(new Run("العنوان - رقم الهاتف"));
        addressParagraph.TextAlignment = TextAlignment.Center;
        addressParagraph.FontSize = 10;
        doc.Blocks.Add(addressParagraph);

        // Receipt details
        var receiptInfoParagraph = new Paragraph();
        receiptInfoParagraph.Inlines.Add(new Run($"إيصال رقم: {_transaction.TransactionNumber}"));
        receiptInfoParagraph.Inlines.Add(new LineBreak());
        receiptInfoParagraph.Inlines.Add(new Run($"التاريخ: {_transaction.TransactionDate:yyyy-MM-dd HH:mm}"));
        receiptInfoParagraph.Inlines.Add(new LineBreak());
        receiptInfoParagraph.Inlines.Add(new Run($"الكاشير: {_cashierName}"));
        doc.Blocks.Add(receiptInfoParagraph);

        // Separator
        doc.Blocks.Add(new Paragraph(new Run(new string('-', 50))));

        // Items
        foreach (var item in _transaction.Items)
        {
            var itemParagraph = new Paragraph();
            itemParagraph.Inlines.Add(new Run($"منتج {item.ProductId}"));
            itemParagraph.Inlines.Add(new LineBreak());
            itemParagraph.Inlines.Add(new Run($"  {item.Quantity} x {item.UnitPrice:F2} = {item.LineTotal:F2}"));
            doc.Blocks.Add(itemParagraph);
        }

        // Separator
        doc.Blocks.Add(new Paragraph(new Run(new string('-', 50))));

        // Totals
        var totalsParagraph = new Paragraph();
        totalsParagraph.Inlines.Add(new Run($"المجموع الفرعي: {_transaction.Subtotal:F2}"));
        totalsParagraph.Inlines.Add(new LineBreak());
        totalsParagraph.Inlines.Add(new Run($"الخصم: {_transaction.DiscountAmount:F2}"));
        totalsParagraph.Inlines.Add(new LineBreak());
        totalsParagraph.Inlines.Add(new Bold(new Run($"الإجمالي: {_transaction.Total:F2}")));
        totalsParagraph.Inlines.Add(new LineBreak());
        totalsParagraph.Inlines.Add(new Run($"المبلغ المدفوع: {_amountPaid:F2}"));
        totalsParagraph.Inlines.Add(new LineBreak());
        totalsParagraph.Inlines.Add(new Run($"الباقي: {_change:F2}"));
        doc.Blocks.Add(totalsParagraph);

        // Footer
        var footerParagraph = new Paragraph();
        footerParagraph.Inlines.Add(new LineBreak());
        footerParagraph.Inlines.Add(new Run("شكراً لتسوقكم معنا"));
        footerParagraph.TextAlignment = TextAlignment.Center;
        footerParagraph.FontStyle = FontStyles.Italic;
        doc.Blocks.Add(footerParagraph);

        return doc;
    }

    private void btnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}