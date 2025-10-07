using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AccountingSystem.WPF.Views
{
    // هوامش القالب
    public class TemplateMargins
    {
        public double Top { get; set; } = 20;
        public double Right { get; set; } = 15;
        public double Bottom { get; set; } = 20;
        public double Left { get; set; } = 15;
    }

    // نموذج قالب الطباعة
    public class PrintTemplate
    {
        public int Id { get; set; }
        
        [Required, StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;
        
        [StringLength(50)]
        public string Type { get; set; } = "Invoice"; // Invoice, Receipt, Report
        
        public double Width { get; set; } = 210; // ملليمتر
        public double Height { get; set; } = 297; // ملليمتر
        
        [StringLength(20)]
        public string BackgroundColor { get; set; } = "#FFFFFF";
        
        [StringLength(20)]
        public string Orientation { get; set; } = "Portrait"; // Portrait, Landscape
        
        public TemplateMargins Margins { get; set; } = new();
        
        public List<TemplateElement> Elements { get; set; } = new();
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime LastModified { get; set; } = DateTime.Now;
        public DateTime ModifiedDate { get; set; } = DateTime.Now; // للتوافق
        
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; } = false;
    }

    // عنصر في قالب الطباعة
    public class TemplateElement
    {
        public int Id { get; set; }
        public int TemplateId { get; set; }
        
        [Required, StringLength(20)]
        public string Type { get; set; } = "Text"; // Text, Image, Table, Line, Rectangle, Barcode
        
        public double X { get; set; } // موضع أفقي
        public double Y { get; set; } // موضع عمودي
        public double Width { get; set; }
        public double Height { get; set; }
        
        [StringLength(500)]
        public string Content { get; set; } = string.Empty;
        
        // خصائص النص
        [StringLength(50)]
        public string FontFamily { get; set; } = "Arial";
        public double FontSize { get; set; } = 12;
        
        [StringLength(20)]
        public string FontWeight { get; set; } = "Normal"; // Normal, Bold
        
        [StringLength(20)]
        public string FontStyle { get; set; } = "Normal"; // Normal, Italic
        
        [StringLength(20)]
        public string Color { get; set; } = "#000000";
        
        [StringLength(20)]
        public string BackgroundColor { get; set; } = "Transparent";
        
        [StringLength(20)]
        public string TextAlign { get; set; } = "Left"; // Left, Center, Right, Justify
        
        // خصائص الحدود والصور
        public double BorderWidth { get; set; } = 0;
        
        [StringLength(20)]
        public string BorderColor { get; set; } = "#000000";
        
        [StringLength(200)]
        public string ImagePath { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string ImageStretch { get; set; } = "Uniform"; // None, Fill, Uniform, UniformToFill
        
        // خصائص الترتيب والرؤية
        public int ZIndex { get; set; } = 0;
        public bool IsVisible { get; set; } = true;
        public bool IsLocked { get; set; } = false;
    }

    // بيانات الطباعة الديناميكية
    public class PrintData
    {
        public Dictionary<string, object?> Fields { get; set; } = new();
        public List<Dictionary<string, object>> TableData { get; set; } = new();
        
        public void SetField(string key, object? value)
        {
            Fields[key] = value;
        }
        
        public T? GetField<T>(string key)
        {
            if (Fields.TryGetValue(key, out var value) && value is T result)
                return result;
            return default(T);
        }
        
        public void AddTableRow(Dictionary<string, object> row)
        {
            TableData.Add(row);
        }
        
        public void ClearTableData()
        {
            TableData.Clear();
        }
    }

    // إعدادات الطباعة
    public class PrintSettings
    {
        public string PrinterName { get; set; } = string.Empty;
        public int Copies { get; set; } = 1;
        public bool ColorPrint { get; set; } = true;
        public bool Duplex { get; set; } = false;
        public string PaperSize { get; set; } = "A4";
        public int Quality { get; set; } = 300; // DPI
        
        // إعدادات PDF
        public bool OptimizeForWeb { get; set; } = true;
        public bool PasswordProtected { get; set; } = false;
        public string Password { get; set; } = string.Empty;
        public string CompressionLevel { get; set; } = "High";
    }

    // أنواع القوالب المدعومة
    public static class TemplateTypes
    {
        public const string SalesInvoice = "SalesInvoice";
        public const string PurchaseInvoice = "PurchaseInvoice";
        public const string POSReceipt = "POSReceipt";
        public const string ThermalReceipt = "ThermalReceipt";
        public const string Report = "Report";
        public const string Label = "Label";
        public const string Certificate = "Certificate";
    }

    // أنواع العناصر المدعومة
    public static class ElementTypes
    {
        public const string Text = "Text";
        public const string Image = "Image";
        public const string Table = "Table";
        public const string Line = "Line";
        public const string Rectangle = "Rectangle";
        public const string Barcode = "Barcode";
        public const string QRCode = "QRCode";
    }
}