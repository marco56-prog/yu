using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccountingSystem.Models
{
    /// <summary>
    /// قالب طباعة قابل للتخصيص
    /// </summary>
    public class PrintTemplate
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        // أبعاد القالب بالمليمتر
        public double Width { get; set; } = 210; // A4 افتراضياً
        public double Height { get; set; } = 297; // A4 افتراضياً

        [StringLength(20)]
        public string Orientation { get; set; } = "Portrait"; // Portrait أو Landscape

        public TemplateMargins Margins { get; set; } = new TemplateMargins();

        [StringLength(10)]
        public string BackgroundColor { get; set; } = "#FFFFFF";

        public List<TemplateElement> Elements { get; set; } = new List<TemplateElement>();

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;

        [StringLength(100)]
        public string CreatedBy { get; set; } = "System";
    }

    /// <summary>
    /// هوامش القالب
    /// </summary>
    public class TemplateMargins
    {
        public double Top { get; set; } = 20;
        public double Right { get; set; } = 15;
        public double Bottom { get; set; } = 20;
        public double Left { get; set; } = 15;
    }

    /// <summary>
    /// عنصر في قالب الطباعة
    /// </summary>
    public class TemplateElement
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // Text, Image, Line, Rectangle, Table, Barcode

        public string Content { get; set; } = string.Empty;

        // موضع العنصر (بالمليمتر من الأعلى والشمال)
        public double X { get; set; } = 0;
        public double Y { get; set; } = 0;
        public double Width { get; set; } = 100;
        public double Height { get; set; } = 20;

        // خصائص النص
        [StringLength(50)]
        public string FontFamily { get; set; } = "Arial";
        public double FontSize { get; set; } = 12;
        [StringLength(20)]
        public string FontWeight { get; set; } = "Normal"; // Normal, Bold
        [StringLength(20)]
        public string FontStyle { get; set; } = "Normal"; // Normal, Italic
        [StringLength(20)]
        public string TextDecoration { get; set; } = "None"; // None, Underline, Strikethrough
        [StringLength(20)]
        public string TextAlign { get; set; } = "Left"; // Left, Center, Right, Justify

        // الألوان
        [StringLength(10)]
        public string Color { get; set; } = "#000000"; // لون النص أو الخط
        [StringLength(10)]
        public string BackgroundColor { get; set; } = "Transparent";
        [StringLength(10)]
        public string BorderColor { get; set; } = "#000000";
        public double BorderWidth { get; set; } = 0;

        // خصائص خاصة للجداول
        public int? ColumnCount { get; set; }
        public int? RowCount { get; set; }
        public List<string>? ColumnHeaders { get; set; }
        public List<double>? ColumnWidths { get; set; }

        // خصائص خاصة للصور
        [StringLength(500)]
        public string ImagePath { get; set; } = string.Empty;
        [StringLength(20)]
        public string ImageStretch { get; set; } = "Uniform"; // None, Fill, Uniform, UniformToFill

        // خصائص خاصة للباركود
        [StringLength(50)]
        public string BarcodeFormat { get; set; } = "CODE128"; // CODE128, QR_CODE, EAN13, etc.

        // خصائص عامة
        public int ZIndex { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public bool IsLocked { get; set; } = false;

        // التدوير بالدرجات
        public double Rotation { get; set; } = 0;

        // الشفافية (0-100)
        public double Opacity { get; set; } = 100;
    }

    /// <summary>
    /// إعدادات طباعة متقدمة
    /// </summary>
    public class PrintSettings
    {
        public string Id { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string PrinterName { get; set; } = string.Empty;

        [StringLength(50)]
        public string PaperSize { get; set; } = "A4";

        [StringLength(20)]
        public string Orientation { get; set; } = "Portrait";

        // الجودة (150, 300, 600 DPI)
        public int Quality { get; set; } = 300;

        // عدد النسخ
        public int Copies { get; set; } = 1;

        // طباعة ملونة أم أبيض وأسود
        public bool IsColorPrint { get; set; } = true;

        // طباعة على وجهين
        public bool IsDuplexPrint { get; set; } = false;

        // حفظ كـ PDF تلقائياً
        public bool AutoSaveAsPdf { get; set; } = false;
        [StringLength(500)]
        public string PdfSavePath { get; set; } = string.Empty;

        // إعدادات الهوامش
        public TemplateMargins CustomMargins { get; set; } = new TemplateMargins();

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now;

        public bool IsDefault { get; set; } = false;
    }

    /// <summary>
    /// سجل طباعة
    /// </summary>
    public class PrintLog
    {
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string DocumentType { get; set; } = string.Empty; // Invoice, Report, Receipt

        [StringLength(100)]
        public string DocumentId { get; set; } = string.Empty;

        [StringLength(100)]
        public string TemplateName { get; set; } = string.Empty;

        [StringLength(100)]
        public string PrinterName { get; set; } = string.Empty;

        public int Copies { get; set; } = 1;

        public DateTime PrintedDate { get; set; } = DateTime.Now;

        [StringLength(100)]
        public string PrintedBy { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Success"; // Success, Failed

        [StringLength(500)]
        public string ErrorMessage { get; set; } = string.Empty;

        // معلومات إضافية
        public int PageCount { get; set; } = 1;
        public decimal PrintCost { get; set; } = 0;
    }

    /// <summary>
    /// بيانات ديناميكية للطباعة
    /// </summary>
    public class PrintData
    {
        public Dictionary<string, object> Fields { get; set; } = new Dictionary<string, object>();
        public List<Dictionary<string, object>> TableData { get; set; } = new List<Dictionary<string, object>>();

        // دوال مساعدة
        public void SetField(string key, object value)
        {
            Fields[key] = value;
        }

        public T GetField<T>(string key, T defaultValue = default)
        {
            if (Fields.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }
            return defaultValue;
        }

        public void AddTableRow(Dictionary<string, object> rowData)
        {
            TableData.Add(rowData);
        }

        public void SetTableData(IEnumerable<Dictionary<string, object>> data)
        {
            TableData.Clear();
            TableData.AddRange(data);
        }
    }

    /// <summary>
    /// أنواع قوالب الطباعة المدعومة
    /// </summary>
    public static class TemplateTypes
    {
        public const string SalesInvoice = "فاتورة بيع";
        public const string PurchaseInvoice = "فاتورة شراء";
        public const string POSReceipt = "إيصال نقطة البيع";
        public const string ThermalReceipt = "إيصال حراري";
        public const string SalesReport = "تقرير مبيعات";
        public const string InventoryReport = "تقرير مخزون";
        public const string CustomerStatement = "كشف حساب عميل";
        public const string Check = "شيك";
        public const string Custom = "مخصص";

        public static readonly List<string> AllTypes = new()
        {
            SalesInvoice, PurchaseInvoice, POSReceipt, ThermalReceipt,
            SalesReport, InventoryReport, CustomerStatement, Check, Custom
        };
    }

    /// <summary>
    /// أنواع عناصر القوالب المدعومة
    /// </summary>
    public static class ElementTypes
    {
        public const string Text = "Text";
        public const string Image = "Image";
        public const string Line = "Line";
        public const string Rectangle = "Rectangle";
        public const string Table = "Table";
        public const string Barcode = "Barcode";
        public const string QRCode = "QRCode";
        public const string Chart = "Chart";

        public static readonly List<string> AllTypes = new()
        {
            Text, Image, Line, Rectangle, Table, Barcode, QRCode, Chart
        };
    }
}