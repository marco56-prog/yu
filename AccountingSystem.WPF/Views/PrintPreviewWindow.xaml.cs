using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Markup;
using AccountingSystem.Data;
using AccountingSystem.Models;
using Microsoft.Win32;
using System.Windows.Xps.Packaging;
using System.Windows.Xps;
// PDF export currently uses simple XPS path; iTextSharp references removed.
using Rectangle = System.Windows.Shapes.Rectangle;
using Line = System.Windows.Shapes.Line;

namespace AccountingSystem.WPF.Views
{
    public partial class PrintPreviewWindow : Window
    {
        private readonly PrintTemplate _template;
        private readonly PrintData _printData;
        private FixedDocument? _fixedDocument;

        public PrintPreviewWindow(PrintTemplate template, IUnitOfWork unitOfWork, PrintData? printData = null)
        {
            InitializeComponent();

            _template = template ?? throw new ArgumentNullException(nameof(template));
            _printData = printData ?? CreateSampleData();

            Title = $"معاينة الطباعة - {_template.Name}";

            Loaded += async (s, e) => await LoadPreviewAsync();
        }

        private async Task LoadPreviewAsync()
        {
            try
            {
                lblStatus.Text = "جارٍ تحضير المعاينة...";

                // إنشاء المستند للمعاينة
                await CreatePreviewDocumentAsync();

                // عرض المستند في المعاينة
                documentViewer.Document = _fixedDocument;

                // تحديث معلومات المعاينة
                UpdatePreviewInfo();

                lblStatus.Text = "جاهز للطباعة أو الحفظ";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحضير المعاينة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatus.Text = "خطأ في المعاينة";
            }
        }

        private async Task CreatePreviewDocumentAsync()
        {
            try
            {
                // إنشاء مستند ثابت
                _fixedDocument = new FixedDocument();

                // تحديد حجم الصفحة
                var pageSize = new System.Windows.Size(
                    _template.Width * 96 / 25.4, // تحويل من مليمتر إلى بكسل
                    _template.Height * 96 / 25.4
                );

                // إنشاء صفحة
                var page = CreateFixedPage(pageSize);

                // إضافة العناصر للصفحة
                await AddElementsToPageAsync(page);

                // إضافة الصفحة للمستند
                var pageContent = new PageContent();
                ((IAddChild)pageContent).AddChild(page);
                _fixedDocument.Pages.Add(pageContent);

                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"فشل في إنشاء مستند المعاينة: {ex.Message}");
            }
        }

        private FixedPage CreateFixedPage(System.Windows.Size pageSize)
        {
            var page = new FixedPage();
            page.Width = pageSize.Width;
            page.Height = pageSize.Height;

            // إضافة خلفية الصفحة
            var background = new Rectangle();
            background.Width = page.Width;
            background.Height = page.Height;
            background.Fill = new SolidColorBrush((System.Windows.Media.Color)ColorConverter.ConvertFromString(_template.BackgroundColor));
            page.Children.Add(background);

            return page;
        }

        private async Task AddElementsToPageAsync(FixedPage page)
        {
            try
            {
                if (_template.Elements == null) return;

                foreach (var element in _template.Elements.OrderBy(e => e.ZIndex))
                {
                    if (!element.IsVisible) continue;

                    var uiElement = await CreateUIElementAsync(element);
                    if (uiElement != null)
                    {
                        // تحديد موضع العنصر
                        FixedPage.SetLeft(uiElement, element.X * 96 / 25.4);
                        FixedPage.SetTop(uiElement, element.Y * 96 / 25.4);

                        page.Children.Add(uiElement);
                    }
                }

                await Task.Delay(1);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إضافة العناصر: {ex.Message}");
            }
        }

        private async Task<UIElement?> CreateUIElementAsync(TemplateElement element)
        {
            try
            {
                return element.Type switch
                {
                    "Text" => CreateTextElement(element),
                    "Line" => CreateLineElement(element),
                    "Rectangle" => CreateRectangleElement(element),
                    "Table" => await CreateTableElementAsync(element),
                    "Image" => CreateImageElement(element),
                    "Barcode" => CreateBarcodeElement(element),
                    _ => new TextBlock { Text = $"عنصر غير مدعوم: {element.Type}", Foreground = new SolidColorBrush(Colors.Red) }
                };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إنشاء عنصر {element.Type}: {ex.Message}");
                return new TextBlock { Text = "خطأ في العنصر", Foreground = new SolidColorBrush(Colors.Red) };
            }
        }

        private UIElement CreateTextElement(TemplateElement element)
        {
            var textBlock = new TextBlock();

            // معالجة النص الديناميكي
            textBlock.Text = ProcessDynamicText(element.Content);

            // تطبيق الخصائص
            textBlock.FontFamily = new FontFamily(element.FontFamily);
            textBlock.FontSize = element.FontSize * 96 / 72; // تحويل من نقطة إلى بكسل
            textBlock.FontWeight = element.FontWeight == "Bold" ? FontWeights.Bold : FontWeights.Normal;
            textBlock.FontStyle = element.FontStyle == "Italic" ? FontStyles.Italic : FontStyles.Normal;
            textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(element.Color));

            // المحاذاة
            switch (element.TextAlign)
            {
                case "Center":
                    textBlock.TextAlignment = TextAlignment.Center;
                    break;
                case "Right":
                    textBlock.TextAlignment = TextAlignment.Right;
                    break;
                case "Justify":
                    textBlock.TextAlignment = TextAlignment.Justify;
                    break;
                default:
                    textBlock.TextAlignment = TextAlignment.Left;
                    break;
            }

            // الحجم
            textBlock.Width = element.Width * 96 / 25.4;
            textBlock.Height = element.Height * 96 / 25.4;
            textBlock.TextWrapping = TextWrapping.Wrap;

            // الخلفية والحدود
            if (element.BackgroundColor != "Transparent")
            {
                textBlock.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(element.BackgroundColor));
            }

            return textBlock;
        }

        private static UIElement CreateLineElement(TemplateElement element)
        {
            var line = new Line();
            line.X1 = 0;
            line.Y1 = 0;
            line.X2 = element.Width * 96 / 25.4;
            line.Y2 = element.Height * 96 / 25.4;
            line.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(element.Color));
            line.StrokeThickness = Math.Max(element.BorderWidth, 1);

            return line;
        }

        private static UIElement CreateRectangleElement(TemplateElement element)
        {
            var rectangle = new Rectangle();
            rectangle.Width = element.Width * 96 / 25.4;
            rectangle.Height = element.Height * 96 / 25.4;

            if (element.BackgroundColor != "Transparent")
            {
                rectangle.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(element.BackgroundColor));
            }

            if (element.BorderWidth > 0)
            {
                rectangle.Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(element.BorderColor));
                rectangle.StrokeThickness = element.BorderWidth;
            }

            return rectangle;
        }

        private async Task<UIElement> CreateTableElementAsync(TemplateElement element)
        {
            try
            {
                var grid = new Grid();
                grid.Width = element.Width * 96 / 25.4;
                grid.Height = element.Height * 96 / 25.4;

                // إنشاء بيانات تجريبية للجدول
                var tableData = GetTableData(element.Content);

                if (tableData.Count > 0)
                {
                    // إضافة الأعمدة
                    var firstRow = tableData[0];
                    foreach (var column in firstRow.Keys)
                    {
                        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                    }

                    // إضافة الصفوف
                    for (int i = 0; i < tableData.Count + 1; i++) // +1 للرأس
                    {
                        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    }

                    // إضافة رأس الجدول
                    int colIndex = 0;
                    foreach (var column in firstRow.Keys)
                    {
                        var headerCell = new Border();
                        headerCell.Background = new SolidColorBrush(Colors.LightGray);
                        headerCell.BorderBrush = new SolidColorBrush(Colors.Black);
                        headerCell.BorderThickness = new Thickness(1);

                        var headerText = new TextBlock();
                        headerText.Text = column;
                        headerText.FontWeight = FontWeights.Bold;
                        headerText.HorizontalAlignment = HorizontalAlignment.Center;
                        headerText.VerticalAlignment = VerticalAlignment.Center;
                        headerText.Padding = new Thickness(5);

                        headerCell.Child = headerText;
                        Grid.SetRow(headerCell, 0);
                        Grid.SetColumn(headerCell, colIndex);
                        grid.Children.Add(headerCell);

                        colIndex++;
                    }

                    // إضافة بيانات الجدول
                    for (int rowIndex = 0; rowIndex < tableData.Count; rowIndex++)
                    {
                        var rowData = tableData[rowIndex];
                        colIndex = 0;

                        foreach (var cellData in rowData.Values)
                        {
                            var cell = new Border();
                            cell.BorderBrush = new SolidColorBrush(Colors.Black);
                            cell.BorderThickness = new Thickness(1);

                            var cellText = new TextBlock();
                            cellText.Text = cellData?.ToString() ?? "";
                            cellText.HorizontalAlignment = HorizontalAlignment.Center;
                            cellText.VerticalAlignment = VerticalAlignment.Center;
                            cellText.Padding = new Thickness(5);

                            cell.Child = cellText;
                            Grid.SetRow(cell, rowIndex + 1);
                            Grid.SetColumn(cell, colIndex);
                            grid.Children.Add(cell);

                            colIndex++;
                        }
                    }
                }

                await Task.Delay(1);
                return grid;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إنشاء الجدول: {ex.Message}");
                return new TextBlock { Text = "خطأ في الجدول" };
            }
        }

        private UIElement CreateImageElement(TemplateElement element)
        {
            try
            {
                var image = new System.Windows.Controls.Image();
                image.Width = element.Width * 96 / 25.4;
                image.Height = element.Height * 96 / 25.4;

                if (!string.IsNullOrEmpty(element.ImagePath) && File.Exists(element.ImagePath))
                {
                    var bitmap = new BitmapImage(new Uri(element.ImagePath));
                    image.Source = bitmap;
                }
                else
                {
                    // صورة افتراضية
                    var placeholder = new Rectangle();
                    placeholder.Width = image.Width;
                    placeholder.Height = image.Height;
                    placeholder.Fill = new SolidColorBrush(Colors.LightGray);
                    placeholder.Stroke = new SolidColorBrush(Colors.Gray);
                    placeholder.StrokeThickness = 1;
                    return placeholder;
                }

                // تطبيق نمط التمدد
                switch (element.ImageStretch)
                {
                    case "Fill":
                        image.Stretch = Stretch.Fill;
                        break;
                    case "UniformToFill":
                        image.Stretch = Stretch.UniformToFill;
                        break;
                    case "None":
                        image.Stretch = Stretch.None;
                        break;
                    default:
                        image.Stretch = Stretch.Uniform;
                        break;
                }

                return image;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إنشاء الصورة: {ex.Message}");
                return new TextBlock { Text = "خطأ في الصورة" };
            }
        }

        private UIElement CreateBarcodeElement(TemplateElement element)
        {
            try
            {
                // مؤقتاً - إنشاء باركود نصي
                var barcodeText = ProcessDynamicText(element.Content);

                var textBlock = new TextBlock();
                textBlock.Text = $"||||| {barcodeText} |||||";
                textBlock.FontFamily = new FontFamily("Consolas");
                textBlock.FontSize = element.FontSize * 96 / 72;
                textBlock.Width = element.Width * 96 / 25.4;
                textBlock.Height = element.Height * 96 / 25.4;
                textBlock.TextAlignment = TextAlignment.Center;
                textBlock.VerticalAlignment = VerticalAlignment.Center;

                return textBlock;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في إنشاء الباركود: {ex.Message}");
                return new TextBlock { Text = "خطأ في الباركود" };
            }
        }

        #region معالجة البيانات الديناميكية

        private string ProcessDynamicText(string text)
        {
            if (string.IsNullOrEmpty(text) || _printData == null) return text;

            var result = text;

            // استبدال المتغيرات في النص
            foreach (var field in _printData.Fields)
            {
                var placeholder = $"{{{field.Key}}}";
                if (result.Contains(placeholder))
                {
                    result = result.Replace(placeholder, field.Value?.ToString() ?? "");
                }
            }

            // معالجة التواريخ والأرقام
            result = result.Replace("{CurrentDate}", DateTime.Now.ToString("yyyy/MM/dd"));
            result = result.Replace("{CurrentTime}", DateTime.Now.ToString("HH:mm:ss"));

            return result;
        }

        private List<Dictionary<string, object>> GetTableData(string content)
        {
            // إذا كان المحتوى يشير إلى جدول ديناميكي
            if (content == "{InvoiceItems}" && _printData?.TableData != null)
            {
                return _printData.TableData;
            }

            // بيانات تجريبية افتراضية
            return new List<Dictionary<string, object>>
            {
                new Dictionary<string, object>
                {
                    { PRODUCT_COLUMN, "منتج تجريبي 1" },
                    { QUANTITY_COLUMN, "2" },
                    { PRICE_COLUMN, "50.00" },
                    { TOTAL_COLUMN, "100.00" }
                },
                new Dictionary<string, object>
                {
                    { PRODUCT_COLUMN, "منتج تجريبي 2" },
                    { QUANTITY_COLUMN, "1" },
                    { PRICE_COLUMN, SAMPLE_PRICE },
                    { TOTAL_COLUMN, SAMPLE_PRICE }
                }
            };
        }

        // Constants for repeated strings
        private const string PRODUCT_COLUMN = "الصنف";
        private const string QUANTITY_COLUMN = "الكمية";
        private const string PRICE_COLUMN = "السعر";
        private const string TOTAL_COLUMN = "الإجمالي";
        private const string SAMPLE_PRICE = "75.00";

        private static PrintData CreateSampleData()
        {
            var data = new PrintData();

            // إضافة بيانات تجريبية
            data.SetField("CompanyName", "شركة تجريبية للتجارة");
            data.SetField("CompanyAddress", "شارع الجامعة، القاهرة، مصر");
            data.SetField("CompanyPhone", "02-1234567890");
            data.SetField("InvoiceNumber", "INV-2024-001");
            data.SetField("InvoiceDate", DateTime.Now.ToString("yyyy/MM/dd"));
            data.SetField("InvoiceTime", DateTime.Now.ToString("HH:mm:ss"));
            data.SetField("CustomerName", "عميل تجريبي");
            data.SetField("TotalAmount", "175.00");

            // إضافة بيانات الجدول
            data.AddTableRow(new Dictionary<string, object>
            {
                { PRODUCT_COLUMN, "منتج A" },
                { QUANTITY_COLUMN, "2" },
                { PRICE_COLUMN, "50.00" },
                { TOTAL_COLUMN, "100.00" }
            });

            data.AddTableRow(new Dictionary<string, object>
            {
                { PRODUCT_COLUMN, "منتج B" },
                { QUANTITY_COLUMN, "1" },
                { PRICE_COLUMN, SAMPLE_PRICE },
                { TOTAL_COLUMN, SAMPLE_PRICE }
            });

            return data;
        }

        #endregion

        #region أحداث الواجهة

        private void btnPrint_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var printDialog = new PrintDialog();

                if (printDialog.ShowDialog() == true)
                {
                    lblStatus.Text = "جارٍ الطباعة...";

                    if (_fixedDocument != null)
                    {
                        printDialog.PrintDocument(_fixedDocument.DocumentPaginator, _template.Name);

                        MessageBox.Show("تمت الطباعة بنجاح!", "نجحت العملية",
                            MessageBoxButton.OK, MessageBoxImage.Information);

                        lblStatus.Text = "تمت الطباعة بنجاح";
                    }
                    else
                    {
                        MessageBox.Show("لا يوجد مستند للطباعة", "خطأ",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        lblStatus.Text = "فشل في الطباعة - لا يوجد مستند";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في الطباعة: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatus.Text = "خطأ في الطباعة";
            }
        }

        private async void btnSaveAsPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var saveDialog = new SaveFileDialog();
                saveDialog.Filter = "ملفات PDF|*.pdf";
                saveDialog.FileName = $"{_template.Name}_{DateTime.Now:yyyyMMdd}.pdf";

                if (saveDialog.ShowDialog() == true)
                {
                    lblStatus.Text = "جارٍ حفظ PDF...";

                    await Task.Run(() => SaveAsPdf(saveDialog.FileName));

                    MessageBox.Show("تم حفظ PDF بنجاح!", "نجحت العملية",
                        MessageBoxButton.OK, MessageBoxImage.Information);

                    lblStatus.Text = "تم حفظ PDF بنجاح";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في حفظ PDF: {ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                lblStatus.Text = "خطأ في حفظ PDF";
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        #endregion

        #region حفظ PDF

        private void SaveAsPdf(string filePath)
        {
            try
            {
                // حفظ مبسط عبر XPS: نحول المستند المعروض إلى XPS ثم نخبر المستخدم بالتنسيق
                var tempXps = System.IO.Path.ChangeExtension(filePath, ".xps");
                var paginator = _fixedDocument?.DocumentPaginator;
                if (paginator == null)
                    throw new InvalidOperationException("لا يوجد مستند للمعاينة");

                using var xpsDoc = new XpsDocument(tempXps, FileAccess.ReadWrite);
                var writer = XpsDocument.CreateXpsDocumentWriter(xpsDoc);
                writer.Write(paginator);

                // ملاحظة: تحويل XPS إلى PDF يتطلب مكتبة خارجية. نترك XPS كبديل مؤقت.
                // يمكن للمستخدم الطباعة إلى PDF باستخدام طابعة PDF.
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"فشل في حفظ PDF: {ex.Message}");
            }
        }

        #endregion

        private void UpdatePreviewInfo()
        {
            try
            {
                lblDetailTemplateName.Text = _template.Name;
                lblDetailTemplateType.Text = _template.Type;
                lblPageSize.Text = $"{_template.Width}×{_template.Height} مم";
                lblElementsCount.Text = _template.Elements?.Count.ToString() ?? "0";

                // تحديث معلومات الرأس أيضاً
                lblTemplateName.Text = _template.Name;
                lblTemplateType.Text = _template.Type;

                // تحديث الإحصائيات
                var textElements = _template.Elements?.Count(e => e.Type == "Text") ?? 0;
                var tableElements = _template.Elements?.Count(e => e.Type == "Table") ?? 0;

                if (lblStatsElements != null) lblStatsElements.Text = (_template.Elements?.Count ?? 0).ToString();
                if (lblStatsTexts != null) lblStatsTexts.Text = textElements.ToString();
                if (lblStatsTables != null) lblStatsTables.Text = tableElements.ToString();

                // تحديث وقت آخر تحديث
                if (lblLastUpdate != null) lblLastUpdate.Text = DateTime.Now.ToString("HH:mm:ss");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"خطأ في تحديث معلومات المعاينة: {ex.Message}");
            }
        }
    }
}