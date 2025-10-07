using System;
using System.Collections.Generic;

namespace AccountingSystem.WPF.Services
{
    #region Export Enums

    public enum ExportFormat
    {
        PDF,
        Excel,
        Word,
        CSV,
        HTML,
        JSON,
        XML,
        PNG,
        SVG
    }

    #endregion

    #region Export Request and Result

    public class ReportExportRequest
    {
        public string ReportTitle { get; set; } = string.Empty;
        public string OutputPath { get; set; } = string.Empty;
        public List<string>? Headers { get; set; }
        public List<Dictionary<string, object>>? Data { get; set; }
        public Dictionary<string, object>? Summary { get; set; }
        public List<ChartConfiguration>? Charts { get; set; }
        public ExportSettings? Settings { get; set; }
        public string? TemplatePath { get; set; }
        public Dictionary<string, object>? Parameters { get; set; }
    }

    public class ExportResult
    {
        public ExportFormat Format { get; set; }
        public bool IsSuccess { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string? ErrorMessage { get; set; }
        public TimeSpan ProcessingTime { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class ExportSettings
    {
        public string PageSize { get; set; } = "A4";
        public string Orientation { get; set; } = "Portrait";
        public string Margins { get; set; } = "20,20,20,20";
        public bool IncludeHeader { get; set; } = true;
        public bool IncludeFooter { get; set; } = true;
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeStyles { get; set; } = true;
        public string Quality { get; set; } = "High";
        public string Encoding { get; set; } = "UTF-8";
        public string Delimiter { get; set; } = ",";
        public bool AutoFitColumns { get; set; } = true;
        public bool FreezeFirstRow { get; set; } = true;
        public bool QuoteFields { get; set; } = true;
        public bool Responsive { get; set; } = true;
        public string Theme { get; set; } = "default";
        public Dictionary<string, object> CustomSettings { get; set; } = new();

        public ExportSettings() { }

        public ExportSettings(ExportSettings other)
        {
            PageSize = other.PageSize;
            Orientation = other.Orientation;
            Margins = other.Margins;
            IncludeHeader = other.IncludeHeader;
            IncludeFooter = other.IncludeFooter;
            IncludeCharts = other.IncludeCharts;
            IncludeStyles = other.IncludeStyles;
            Quality = other.Quality;
            Encoding = other.Encoding;
            Delimiter = other.Delimiter;
            AutoFitColumns = other.AutoFitColumns;
            FreezeFirstRow = other.FreezeFirstRow;
            QuoteFields = other.QuoteFields;
            Responsive = other.Responsive;
            Theme = other.Theme;
            CustomSettings = new Dictionary<string, object>(other.CustomSettings);
        }
    }

    #endregion

    #region Export Templates

    public class ExportTemplate
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public ExportFormat Format { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty; // HTML, CSS, או template content
        public ExportSettings Settings { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsDefault { get; set; }
        public List<TemplateVariable> Variables { get; set; } = new();
    }

    public class TemplateVariable
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // String, Number, Date, Boolean
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public object? DefaultValue { get; set; }
        public bool IsRequired { get; set; }
        public List<string>? AllowedValues { get; set; } // للقيم المحددة مسبقاً
    }

    #endregion

    #region Preview

    public class PreviewResult
    {
        public bool IsSuccess { get; set; }
        public string? Content { get; set; } // HTML preview content
        public string? ErrorMessage { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public List<string>? Warnings { get; set; }
    }

    #endregion

    #region Batch Export

    public class BatchExportRequest
    {
        public string BatchName { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
        public List<ReportExportRequest> Reports { get; set; } = new();
        public ExportFormat[] Formats { get; set; } = Array.Empty<ExportFormat>();
        public bool CompressResults { get; set; } = true;
        public string? NotificationEmail { get; set; }
    }

    public class BatchExportResult
    {
        public string BatchName { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public TimeSpan TotalTime { get; set; }
        public int TotalReports { get; set; }
        public int SuccessfulExports { get; set; }
        public int FailedExports { get; set; }
        public List<ExportResult> Results { get; set; } = new();
        public string? CompressedFilePath { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    #endregion

    #region Chart Export

    public class ChartExportRequest
    {
        public ChartConfiguration Chart { get; set; } = new();
        public string OutputPath { get; set; } = string.Empty;
        public ExportFormat Format { get; set; } = ExportFormat.PNG;
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 600;
        public int Quality { get; set; } = 90; // للـ JPEG
        public bool TransparentBackground { get; set; } = false;
        public string BackgroundColor { get; set; } = "#FFFFFF";
    }

    #endregion

    #region PDF Specific

    public class PdfExportOptions
    {
        public string PageSize { get; set; } = "A4"; // A4, Letter, Legal
        public string Orientation { get; set; } = "Portrait"; // Portrait, Landscape
        public MarginSettings Margins { get; set; } = new();
        public HeaderFooterSettings Header { get; set; } = new();
        public HeaderFooterSettings Footer { get; set; } = new();
        public bool IncludePageNumbers { get; set; } = true;
        public bool IncludeTableOfContents { get; set; } = false;
        public WatermarkSettings? Watermark { get; set; }
        public SecuritySettings? Security { get; set; }
        public string FontFamily { get; set; } = "Arial";
        public int FontSize { get; set; } = 11;
    }

    public class MarginSettings
    {
        public double Top { get; set; } = 20;
        public double Right { get; set; } = 20;
        public double Bottom { get; set; } = 20;
        public double Left { get; set; } = 20;
        public string Unit { get; set; } = "mm"; // mm, pt, in
    }

    public class HeaderFooterSettings
    {
        public bool Enabled { get; set; } = true;
        public string Content { get; set; } = string.Empty;
        public string Alignment { get; set; } = "Center"; // Left, Center, Right
        public int FontSize { get; set; } = 9;
        public bool IncludePage { get; set; } = true;
        public bool IncludeDate { get; set; } = true;
    }

    public class WatermarkSettings
    {
        public string Text { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
        public double Opacity { get; set; } = 0.3;
        public string Color { get; set; } = "#CCCCCC";
        public int FontSize { get; set; } = 48;
        public double Rotation { get; set; } = -45; // degrees
    }

    public class SecuritySettings
    {
        public bool RestrictPrinting { get; set; } = false;
        public bool RestrictCopying { get; set; } = false;
        public bool RestrictEditing { get; set; } = false;
        public string? UserPassword { get; set; }
        public string? OwnerPassword { get; set; }
    }

    #endregion

    #region Excel Specific

    public class ExcelExportOptions
    {
        public string WorksheetName { get; set; } = "تقرير";
        public bool AutoFitColumns { get; set; } = true;
        public bool FreezeHeaders { get; set; } = true;
        public bool IncludeCharts { get; set; } = true;
        public bool IncludeFormulas { get; set; } = false;
        public string DateFormat { get; set; } = "yyyy-mm-dd";
        public string NumberFormat { get; set; } = "#,##0.00";
        public string CurrencyFormat { get; set; } = "#,##0.00 \"ج.م\"";
        public HeaderStyle HeaderStyle { get; set; } = new();
        public DataStyle DataStyle { get; set; } = new();
        public bool ProtectWorksheet { get; set; } = false;
        public string? Password { get; set; }
    }

    public class HeaderStyle
    {
        public string BackgroundColor { get; set; } = "#4472C4";
        public string FontColor { get; set; } = "#FFFFFF";
        public bool Bold { get; set; } = true;
        public int FontSize { get; set; } = 12;
        public string FontName { get; set; } = "Calibri";
    }

    public class DataStyle
    {
        public string FontName { get; set; } = "Calibri";
        public int FontSize { get; set; } = 11;
        public bool AlternatingRowColors { get; set; } = true;
        public string EvenRowColor { get; set; } = "#F2F2F2";
        public string OddRowColor { get; set; } = "#FFFFFF";
        public bool ShowGridlines { get; set; } = true;
    }

    #endregion

    #region Word Specific

    public class WordExportOptions
    {
        public string DocumentTitle { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public bool IncludeCoverPage { get; set; } = false;
        public bool IncludeTableOfContents { get; set; } = false;
        public string FontFamily { get; set; } = "Calibri";
        public int FontSize { get; set; } = 11;
        public double LineSpacing { get; set; } = 1.15;
        public PageSettings Page { get; set; } = new();
        public bool TrackChanges { get; set; } = false;
        public bool ReadOnlyRecommended { get; set; } = false;
    }

    public class PageSettings
    {
        public string Size { get; set; } = "A4";
        public string Orientation { get; set; } = "Portrait";
        public MarginSettings Margins { get; set; } = new();
        public bool ShowPageNumbers { get; set; } = true;
        public string PageNumberFormat { get; set; } = "Arabic"; // Arabic, Roman, Alphabetic
    }

    #endregion

    #region HTML Specific

    public class HtmlExportOptions
    {
        public string Title { get; set; } = string.Empty;
        public string Theme { get; set; } = "modern"; // modern, classic, minimal
        public bool IncludeInlineStyles { get; set; } = true;
        public bool Responsive { get; set; } = true;
        public bool IncludeCharts { get; set; } = true;
        public bool DarkModeSupport { get; set; } = false;
        public string Language { get; set; } = "ar";
        public string Direction { get; set; } = "rtl";
        public string Charset { get; set; } = "UTF-8";
        public List<string> ExternalStylesheets { get; set; } = new();
        public List<string> ExternalScripts { get; set; } = new();
        public Dictionary<string, string> MetaTags { get; set; } = new();
    }

    #endregion

    #region Export Progress and Notifications

    public class ExportProgress
    {
        public string TaskId { get; set; } = string.Empty;
        public string TaskName { get; set; } = string.Empty;
        public int TotalSteps { get; set; }
        public int CompletedSteps { get; set; }
        public double PercentComplete => TotalSteps > 0 ? (double)CompletedSteps / TotalSteps * 100 : 0;
        public string CurrentOperation { get; set; } = string.Empty;
        public DateTime StartedAt { get; set; }
        public TimeSpan ElapsedTime => DateTime.Now - StartedAt;
        public TimeSpan? EstimatedTimeRemaining { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCanceled { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class ExportNotification
    {
        public string TaskId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; } = NotificationType.Info;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string? FilePath { get; set; }
        public long? FileSize { get; set; }
        public Dictionary<string, object>? Data { get; set; }
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }

    #endregion
}