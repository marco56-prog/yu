using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using AccountingSystem.WPF.ViewModels;

namespace AccountingSystem.WPF.Helpers;

/// <summary>
/// Helper class for building FlowDocument from templates
/// </summary>
public static class DocumentBuilder
{
    /// <summary>
    /// Builds invoice FlowDocument from template and fills data
    /// </summary>
    /// <param name="vm">Print view model with invoice data</param>
    /// <returns>FlowDocument ready for printing</returns>
    public static FlowDocument BuildInvoiceDocument(SalesInvoicePrintViewModel vm)
    {
        // Ensure Arabic-Egyptian culture for proper currency formatting
        var previousCulture = CultureInfo.CurrentCulture;
        var previousUICulture = CultureInfo.CurrentUICulture;

        try
        {
            var culture = CultureInfo.GetCultureInfo("ar-EG");
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            // Load FlowDocument template from resources
            var uri = new Uri("/AccountingSystem.WPF;component/Views/PrintTemplates/InvoiceFlowDoc.xaml", UriKind.Relative);
            var doc = (FlowDocument)Application.LoadComponent(uri);

            // Set data context
            doc.DataContext = vm;

            // Find and populate the body group with invoice lines
            var bodyGroup = (TableRowGroup)doc.FindName("BodyGroup")!;
            bodyGroup.Rows.Clear();

            // Add invoice lines
            int lineNumber = 1;
            foreach (var line in vm.Lines)
            {
                var row = new TableRow();

                // Add cells for each column
                row.Cells.Add(CreateCell(lineNumber.ToString()));
                row.Cells.Add(CreateCell(line.ProductName, TextAlignment.Right));
                row.Cells.Add(CreateCell(line.UnitName));
                row.Cells.Add(CreateCell(line.Quantity.ToString("N2")));
                row.Cells.Add(CreateCell($"{line.UnitPrice:N2} ج.م"));
                row.Cells.Add(CreateCell($"{line.DiscountAmount:N2} ج.م"));
                row.Cells.Add(CreateCell($"{line.NetAmount:N2} ج.م"));

                bodyGroup.Rows.Add(row);
                lineNumber++;
            }

            return doc;
        }
        finally
        {
            // Restore previous culture
            CultureInfo.CurrentCulture = previousCulture;
            CultureInfo.CurrentUICulture = previousUICulture;
        }
    }

    /// <summary>
    /// Creates a table cell with specified text and formatting
    /// </summary>
    /// <param name="text">Cell text content</param>
    /// <param name="textAlignment">Text alignment (default: Center)</param>
    /// <returns>Formatted table cell</returns>
    private static TableCell CreateCell(string text, TextAlignment textAlignment = TextAlignment.Center)
    {
        var paragraph = new Paragraph(new Run(text))
        {
            Margin = new Thickness(0),
            TextAlignment = textAlignment
        };

        var cell = new TableCell(paragraph)
        {
            BorderThickness = new Thickness(0, 0, 1, 1),
            BorderBrush = Brushes.Black,
            Padding = new Thickness(3),
            TextAlignment = textAlignment
        };

        return cell;
    }

    /// <summary>
    /// Creates a print-ready FlowDocument with proper page settings for A4
    /// </summary>
    /// <param name="content">Source FlowDocument</param>
    /// <returns>FlowDocument optimized for A4 printing</returns>
    public static FlowDocument CreatePrintDocument(FlowDocument content)
    {
        // Clone the document for printing
        var printDoc = new FlowDocument
        {
            FontFamily = content.FontFamily,
            FontSize = content.FontSize,
            FlowDirection = content.FlowDirection,

            // A4 page settings (210mm x 297mm)
            PageWidth = 793.7, // 210mm in WPF units (1/96 inch)
            PageHeight = 1122.5, // 297mm in WPF units
            PagePadding = new Thickness(40), // 20mm margins
            ColumnWidth = double.PositiveInfinity,

            // Print-specific settings
            IsOptimalParagraphEnabled = false,
            IsHyphenationEnabled = false
        };

        // Copy content
        foreach (var block in content.Blocks)
        {
            if (block is Paragraph p)
            {
                var newP = new Paragraph();
                foreach (var inline in p.Inlines)
                {
                    if (inline is Run run)
                        newP.Inlines.Add(new Run(run.Text) { FontWeight = run.FontWeight });
                }
                printDoc.Blocks.Add(newP);
            }
            else if (block is Table table)
            {
                // Clone table with all its properties
                var newTable = CloneTable(table);
                printDoc.Blocks.Add(newTable);
            }
        }

        return printDoc;
    }

    private static Table CloneTable(Table originalTable)
    {
        var newTable = new Table
        {
            CellSpacing = originalTable.CellSpacing,
            BorderBrush = originalTable.BorderBrush,
            BorderThickness = originalTable.BorderThickness
        };

        // Clone columns
        foreach (var column in originalTable.Columns)
        {
            newTable.Columns.Add(new TableColumn { Width = column.Width });
        }

        // Clone row groups
        foreach (var rowGroup in originalTable.RowGroups)
        {
            var newRowGroup = new TableRowGroup();

            foreach (var row in rowGroup.Rows)
            {
                var newRow = new TableRow();

                foreach (var cell in row.Cells)
                {
                    var newCell = new TableCell
                    {
                        BorderBrush = cell.BorderBrush,
                        BorderThickness = cell.BorderThickness,
                        Padding = cell.Padding,
                        Background = cell.Background,
                        TextAlignment = cell.TextAlignment
                    };

                    foreach (var block in cell.Blocks)
                    {
                        if (block is Paragraph p)
                        {
                            var newP = new Paragraph();
                            foreach (var inline in p.Inlines)
                            {
                                if (inline is Run run)
                                    newP.Inlines.Add(new Run(run.Text) { FontWeight = run.FontWeight });
                            }
                            newCell.Blocks.Add(newP);
                        }
                    }

                    newRow.Cells.Add(newCell);
                }

                newRowGroup.Rows.Add(newRow);
            }

            newTable.RowGroups.Add(newRowGroup);
        }

        return newTable;
    }
}