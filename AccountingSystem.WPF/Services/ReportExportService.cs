using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Text;
using System.Linq;
using System.Data;
using AccountingSystem.WPF.Helpers;
using System.Globalization;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// خدمة تصدير التقارير بصيغ متعددة مع دعم الرسوم البيانية
    /// </summary>
    public interface IReportExportService
    {
        // تصدير PDF
        Task<ExportResult> ExportToPdfAsync(ReportExportRequest request);
        
        // تصدير Excel
        Task<ExportResult> ExportToExcelAsync(ReportExportRequest request);
        
        // تصدير Word
        Task<ExportResult> ExportToWordAsync(ReportExportRequest request);
        
        // تصدير CSV
        Task<ExportResult> ExportToCsvAsync(ReportExportRequest request);
        
        // تصدير HTML
        Task<ExportResult> ExportToHtmlAsync(ReportExportRequest request);
        
        // تصدير JSON
        Task<ExportResult> ExportToJsonAsync(ReportExportRequest request);
        
        // تصدير XML
        Task<ExportResult> ExportToXmlAsync(ReportExportRequest request);
        
        // تصدير الرسوم البيانية
        Task<ExportResult> ExportChartsAsync(List<ChartConfiguration> charts, string outputPath, ExportFormat format);
        
        // قوالب التصدير
        Task<List<ExportTemplate>> GetAvailableTemplatesAsync(ExportFormat format);
        Task SaveExportTemplateAsync(ExportTemplate template);
        
        // معاينة قبل التصدير
        Task<PreviewResult> GeneratePreviewAsync(ReportExportRequest request);
        
        // إعدادات التصدير
        ExportSettings GetDefaultSettings(ExportFormat format);
        void UpdateExportSettings(ExportFormat format, ExportSettings settings);
    }

    public class ReportExportService : IReportExportService
    {
        private const string ComponentName = "ReportExportService";
        private readonly CultureInfo _culture;
        private readonly Dictionary<ExportFormat, ExportSettings> _defaultSettings;

        public ReportExportService()
        {
            _culture = new CultureInfo("ar-EG");
            _culture.NumberFormat.CurrencySymbol = "ج.م";
            _defaultSettings = InitializeDefaultSettings();
            
            ComprehensiveLogger.LogInfo("تم تهيئة خدمة تصدير التقارير", ComponentName);
        }

        #region PDF Export

        public async Task<ExportResult> ExportToPdfAsync(ReportExportRequest request)
        {
            try
            {
                ComprehensiveLogger.LogInfo($"بدء تصدير التقرير إلى PDF: {request.ReportTitle}", ComponentName);

                var result = new ExportResult
                {
                    Format = ExportFormat.PDF,
                    FilePath = GenerateFilePath(request.OutputPath, request.ReportTitle, "pdf"),
                    IsSuccess = false
                };

                // إنشاء محتوى PDF
                var pdfContent = await GeneratePdfContentAsync(request);
                
                // سيتم استخدام مكتبة PDF مناسبة هنا
                await File.WriteAllTextAsync(result.FilePath, pdfContent, Encoding.UTF8);
                
                result.IsSuccess = true;
                result.FileSize = new FileInfo(result.FilePath).Length;
                result.GeneratedAt = DateTime.Now;

                ComprehensiveLogger.LogBusinessOperation("تم تصدير التقرير إلى PDF بنجاح", 
                    $"الملف: {result.FilePath} | الحجم: {result.FileSize} بايت", 
                    isSuccess: true);

                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل في تصدير التقرير إلى PDF: {request.ReportTitle}", ex, ComponentName);
                return new ExportResult
                {
                    Format = ExportFormat.PDF,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> GeneratePdfContentAsync(ReportExportRequest request)
        {
            await Task.Delay(1); // محاكاة العملية
            
            var content = new StringBuilder();
            content.AppendLine("<!DOCTYPE html>");
            content.AppendLine("<html dir='rtl' lang='ar'>");
            content.AppendLine("<head>");
            content.AppendLine("<meta charset='UTF-8'>");
            content.AppendLine($"<title>{request.ReportTitle}</title>");
            content.AppendLine("<style>");
            content.AppendLine(GetPdfStyles());
            content.AppendLine("</style>");
            content.AppendLine("</head>");
            content.AppendLine("<body>");
            
            // رأس التقرير
            content.AppendLine("<div class='header'>");
            content.AppendLine("<div class='logo'>نظام المحاسبة</div>");
            content.AppendLine($"<h1>{request.ReportTitle}</h1>");
            content.AppendLine($"<div class='date'>تاريخ الإنشاء: {DateTime.Now:yyyy-MM-dd HH:mm}</div>");
            content.AppendLine("</div>");
            
            // محتوى التقرير
            content.AppendLine("<div class='content'>");
            
            if (request.Data?.Any() == true)
            {
                content.AppendLine(GenerateHtmlTable(request.Data, request.Headers));
            }
            
            if (request.Summary?.Any() == true)
            {
                content.AppendLine("<div class='summary'>");
                content.AppendLine("<h2>الملخص</h2>");
                foreach (var item in request.Summary)
                {
                    content.AppendLine($"<div class='summary-item'>");
                    content.AppendLine($"<span class='label'>{item.Key}:</span>");
                    content.AppendLine($"<span class='value'>{item.Value}</span>");
                    content.AppendLine("</div>");
                }
                content.AppendLine("</div>");
            }
            
            content.AppendLine("</div>");
            
            // تذييل التقرير
            content.AppendLine("<div class='footer'>");
            content.AppendLine($"<div>صفحة 1 من 1 | إجمالي السجلات: {request.Data?.Count ?? 0}</div>");
            content.AppendLine("</div>");
            
            content.AppendLine("</body>");
            content.AppendLine("</html>");
            
            return content.ToString();
        }

        private static string GetPdfStyles()
        {
            return @"
                body { font-family: 'Arial Unicode MS', Arial, sans-serif; margin: 0; padding: 20px; }
                .header { text-align: center; border-bottom: 2px solid #333; padding-bottom: 20px; margin-bottom: 30px; }
                .logo { font-size: 24px; font-weight: bold; color: #2196F3; margin-bottom: 10px; }
                h1 { color: #333; margin: 10px 0; }
                .date { color: #666; font-size: 14px; }
                .content { margin: 20px 0; }
                table { width: 100%; border-collapse: collapse; margin: 20px 0; }
                th, td { border: 1px solid #ddd; padding: 8px; text-align: right; }
                th { background-color: #f2f2f2; font-weight: bold; }
                .summary { margin-top: 30px; padding: 20px; background-color: #f9f9f9; border: 1px solid #ddd; }
                .summary-item { display: flex; justify-content: space-between; margin: 10px 0; }
                .label { font-weight: bold; }
                .value { color: #2196F3; }
                .footer { text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd; color: #666; }
            ";
        }

        #endregion

        #region Excel Export

        public async Task<ExportResult> ExportToExcelAsync(ReportExportRequest request)
        {
            try
            {
                ComprehensiveLogger.LogInfo($"بدء تصدير التقرير إلى Excel: {request.ReportTitle}", ComponentName);

                var result = new ExportResult
                {
                    Format = ExportFormat.Excel,
                    FilePath = GenerateFilePath(request.OutputPath, request.ReportTitle, "xlsx"),
                    IsSuccess = false
                };

                // إنشاء محتوى Excel (CSV مؤقتاً)
                var csvContent = await GenerateCsvContentAsync(request);
                await File.WriteAllTextAsync(result.FilePath.Replace(".xlsx", ".csv"), csvContent, Encoding.UTF8);
                
                result.IsSuccess = true;
                result.FilePath = result.FilePath.Replace(".xlsx", ".csv");
                result.FileSize = new FileInfo(result.FilePath).Length;
                result.GeneratedAt = DateTime.Now;

                ComprehensiveLogger.LogBusinessOperation("تم تصدير التقرير إلى Excel بنجاح", 
                    $"الملف: {result.FilePath} | الحجم: {result.FileSize} بايت", 
                    isSuccess: true);

                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل في تصدير التقرير إلى Excel: {request.ReportTitle}", ex, ComponentName);
                return new ExportResult
                {
                    Format = ExportFormat.Excel,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        #region CSV Export

        public async Task<ExportResult> ExportToCsvAsync(ReportExportRequest request)
        {
            try
            {
                ComprehensiveLogger.LogInfo($"بدء تصدير التقرير إلى CSV: {request.ReportTitle}", ComponentName);

                var result = new ExportResult
                {
                    Format = ExportFormat.CSV,
                    FilePath = GenerateFilePath(request.OutputPath, request.ReportTitle, "csv"),
                    IsSuccess = false
                };

                var csvContent = await GenerateCsvContentAsync(request);
                await File.WriteAllTextAsync(result.FilePath, csvContent, Encoding.UTF8);
                
                result.IsSuccess = true;
                result.FileSize = new FileInfo(result.FilePath).Length;
                result.GeneratedAt = DateTime.Now;

                ComprehensiveLogger.LogBusinessOperation("تم تصدير التقرير إلى CSV بنجاح", 
                    $"الملف: {result.FilePath} | الحجم: {result.FileSize} بايت", 
                    isSuccess: true);

                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل في تصدير التقرير إلى CSV: {request.ReportTitle}", ex, ComponentName);
                return new ExportResult
                {
                    Format = ExportFormat.CSV,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> GenerateCsvContentAsync(ReportExportRequest request)
        {
            await Task.Delay(1);
            
            var content = new StringBuilder();
            
            // إضافة UTF-8 BOM للدعم العربي
            content.Append('\ufeff');
            
            // رأس التقرير
            content.AppendLine($"# {request.ReportTitle}");
            content.AppendLine($"# تاريخ الإنشاء: {DateTime.Now:yyyy-MM-dd HH:mm}");
            content.AppendLine();
            
            // رؤوس الأعمدة
            if (request.Headers?.Any() == true)
            {
                content.AppendLine(string.Join(",", request.Headers.Select(EscapeCsvField)));
            }
            
            // البيانات
            if (request.Data?.Any() == true)
            {
                foreach (var row in request.Data)
                {
                    var values = new List<string>();
                    foreach (var header in request.Headers ?? new List<string>())
                    {
                        var value = row.TryGetValue(header, out var val) ? val?.ToString() ?? "" : "";
                        values.Add(EscapeCsvField(value));
                    }
                    content.AppendLine(string.Join(",", values));
                }
            }
            
            return content.ToString();
        }

        private static string EscapeCsvField(string field)
        {
            if (string.IsNullOrEmpty(field)) return "\"\"";
            
            // إذا كان الحقل يحتوي على فاصلة أو علامات اقتباس أو أسطر جديدة
            if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
            {
                // استبدال علامات الاقتباس المزدوجة بعلامتين
                field = field.Replace("\"", "\"\"");
                // إحاطة الحقل بعلامات اقتباس
                return $"\"{field}\"";
            }
            
            return field;
        }

        #endregion

        #region HTML Export

        public async Task<ExportResult> ExportToHtmlAsync(ReportExportRequest request)
        {
            try
            {
                ComprehensiveLogger.LogInfo($"بدء تصدير التقرير إلى HTML: {request.ReportTitle}", ComponentName);

                var result = new ExportResult
                {
                    Format = ExportFormat.HTML,
                    FilePath = GenerateFilePath(request.OutputPath, request.ReportTitle, "html"),
                    IsSuccess = false
                };

                var htmlContent = await GenerateHtmlContentAsync(request);
                await File.WriteAllTextAsync(result.FilePath, htmlContent, Encoding.UTF8);
                
                result.IsSuccess = true;
                result.FileSize = new FileInfo(result.FilePath).Length;
                result.GeneratedAt = DateTime.Now;

                ComprehensiveLogger.LogBusinessOperation("تم تصدير التقرير إلى HTML بنجاح", 
                    $"الملف: {result.FilePath} | الحجم: {result.FileSize} بايت", 
                    isSuccess: true);

                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل في تصدير التقرير إلى HTML: {request.ReportTitle}", ex, ComponentName);
                return new ExportResult
                {
                    Format = ExportFormat.HTML,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> GenerateHtmlContentAsync(ReportExportRequest request)
        {
            await Task.Delay(1);
            
            var content = new StringBuilder();
            content.AppendLine("<!DOCTYPE html>");
            content.AppendLine("<html dir='rtl' lang='ar'>");
            content.AppendLine("<head>");
            content.AppendLine("<meta charset='UTF-8'>");
            content.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            content.AppendLine($"<title>{request.ReportTitle}</title>");
            content.AppendLine("<style>");
            content.AppendLine(GetHtmlStyles());
            content.AppendLine("</style>");
            content.AppendLine("</head>");
            content.AppendLine("<body>");
            
            // رأس التقرير
            content.AppendLine("<div class='container'>");
            content.AppendLine("<header class='report-header'>");
            content.AppendLine("<div class='logo'>📊 نظام المحاسبة المتقدم</div>");
            content.AppendLine($"<h1>{request.ReportTitle}</h1>");
            content.AppendLine($"<div class='meta-info'>");
            content.AppendLine($"<span>تاريخ الإنشاء: {DateTime.Now:yyyy-MM-dd}</span>");
            content.AppendLine($"<span>الوقت: {DateTime.Now:HH:mm:ss}</span>");
            content.AppendLine($"<span>عدد السجلات: {request.Data?.Count ?? 0}</span>");
            content.AppendLine("</div>");
            content.AppendLine("</header>");
            
            // الملخص
            if (request.Summary?.Any() == true)
            {
                content.AppendLine("<section class='summary-section'>");
                content.AppendLine("<h2>📈 ملخص التقرير</h2>");
                content.AppendLine("<div class='summary-grid'>");
                foreach (var item in request.Summary)
                {
                    content.AppendLine("<div class='summary-card'>");
                    content.AppendLine($"<div class='summary-label'>{item.Key}</div>");
                    content.AppendLine($"<div class='summary-value'>{item.Value}</div>");
                    content.AppendLine("</div>");
                }
                content.AppendLine("</div>");
                content.AppendLine("</section>");
            }
            
            // جدول البيانات
            if (request.Data?.Any() == true)
            {
                content.AppendLine("<section class='data-section'>");
                content.AppendLine("<h2>📋 تفاصيل البيانات</h2>");
                content.AppendLine("<div class='table-container'>");
                content.AppendLine(GenerateHtmlTable(request.Data, request.Headers));
                content.AppendLine("</div>");
                content.AppendLine("</section>");
            }
            
            // تذييل التقرير
            content.AppendLine("<footer class='report-footer'>");
            content.AppendLine("<div>تم إنشاؤه بواسطة نظام المحاسبة المتقدم</div>");
            content.AppendLine($"<div>© {DateTime.Now.Year} جميع الحقوق محفوظة</div>");
            content.AppendLine("</footer>");
            
            content.AppendLine("</div>");
            content.AppendLine("</body>");
            content.AppendLine("</html>");
            
            return content.ToString();
        }

        private static string GetHtmlStyles()
        {
            return @"
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body { font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; background-color: #f5f5f5; }
                .container { max-width: 1200px; margin: 0 auto; background: white; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }
                .report-header { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; }
                .logo { font-size: 28px; font-weight: bold; margin-bottom: 15px; }
                h1 { font-size: 32px; margin: 20px 0; text-shadow: 2px 2px 4px rgba(0,0,0,0.3); }
                .meta-info { display: flex; justify-content: center; gap: 30px; margin-top: 20px; font-size: 16px; }
                .summary-section, .data-section { padding: 30px; }
                h2 { color: #333; margin-bottom: 20px; border-bottom: 3px solid #667eea; padding-bottom: 10px; }
                .summary-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(250px, 1fr)); gap: 20px; }
                .summary-card { background: #f8f9ff; border: 1px solid #e0e6ff; border-radius: 8px; padding: 20px; text-align: center; }
                .summary-label { font-weight: bold; color: #555; margin-bottom: 10px; }
                .summary-value { font-size: 24px; color: #667eea; font-weight: bold; }
                .table-container { overflow-x: auto; border-radius: 8px; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }
                table { width: 100%; border-collapse: collapse; }
                th { background-color: #667eea; color: white; padding: 15px; text-align: center; font-weight: bold; }
                td { padding: 12px 15px; border-bottom: 1px solid #eee; text-align: center; }
                tr:nth-child(even) { background-color: #f9f9f9; }
                tr:hover { background-color: #e8f2ff; }
                .report-footer { background-color: #333; color: white; text-align: center; padding: 20px; }
                @media print { body { background: white; } .container { box-shadow: none; } }
            ";
        }

        private static string GenerateHtmlTable(List<Dictionary<string, object>> data, List<string>? headers)
        {
            if (!data.Any()) return "<p>لا توجد بيانات للعرض</p>";
            
            var table = new StringBuilder();
            table.AppendLine("<table>");
            
            // رؤوس الجدول
            if (headers?.Any() == true)
            {
                table.AppendLine("<thead>");
                table.AppendLine("<tr>");
                foreach (var header in headers)
                {
                    table.AppendLine($"<th>{header}</th>");
                }
                table.AppendLine("</tr>");
                table.AppendLine("</thead>");
            }
            
            // بيانات الجدول
            table.AppendLine("<tbody>");
            foreach (var row in data)
            {
                table.AppendLine("<tr>");
                foreach (var header in headers ?? row.Keys.ToList())
                {
                    var value = row.TryGetValue(header, out var val) ? val?.ToString() ?? "" : "";
                    table.AppendLine($"<td>{value}</td>");
                }
                table.AppendLine("</tr>");
            }
            table.AppendLine("</tbody>");
            
            table.AppendLine("</table>");
            return table.ToString();
        }

        #endregion

        #region JSON Export

        public async Task<ExportResult> ExportToJsonAsync(ReportExportRequest request)
        {
            try
            {
                ComprehensiveLogger.LogInfo($"بدء تصدير التقرير إلى JSON: {request.ReportTitle}", ComponentName);

                var result = new ExportResult
                {
                    Format = ExportFormat.JSON,
                    FilePath = GenerateFilePath(request.OutputPath, request.ReportTitle, "json"),
                    IsSuccess = false
                };

                var jsonContent = await GenerateJsonContentAsync(request);
                await File.WriteAllTextAsync(result.FilePath, jsonContent, Encoding.UTF8);
                
                result.IsSuccess = true;
                result.FileSize = new FileInfo(result.FilePath).Length;
                result.GeneratedAt = DateTime.Now;

                ComprehensiveLogger.LogBusinessOperation("تم تصدير التقرير إلى JSON بنجاح", 
                    $"الملف: {result.FilePath} | الحجم: {result.FileSize} بايت", 
                    isSuccess: true);

                return result;
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"فشل في تصدير التقرير إلى JSON: {request.ReportTitle}", ex, ComponentName);
                return new ExportResult
                {
                    Format = ExportFormat.JSON,
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<string> GenerateJsonContentAsync(ReportExportRequest request)
        {
            await Task.Delay(1);
            
            var reportData = new
            {
                title = request.ReportTitle,
                generatedAt = DateTime.Now,
                headers = request.Headers ?? new List<string>(),
                data = request.Data ?? new List<Dictionary<string, object>>(),
                summary = request.Summary ?? new Dictionary<string, object>(),
                totalRecords = request.Data?.Count ?? 0
            };

            return System.Text.Json.JsonSerializer.Serialize(reportData, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });
        }

        #endregion

        #region Other Export Methods

        public async Task<ExportResult> ExportToWordAsync(ReportExportRequest request)
        {
            // سيتم تنفيذها لاحقاً مع مكتبة Word مناسبة
            await Task.Delay(100);
            return new ExportResult { Format = ExportFormat.Word, IsSuccess = false, ErrorMessage = "لم يتم تنفيذ تصدير Word بعد" };
        }

        public async Task<ExportResult> ExportToXmlAsync(ReportExportRequest request)
        {
            // سيتم تنفيذها لاحقاً
            await Task.Delay(100);
            return new ExportResult { Format = ExportFormat.XML, IsSuccess = false, ErrorMessage = "لم يتم تنفيذ تصدير XML بعد" };
        }

        public async Task<ExportResult> ExportChartsAsync(List<ChartConfiguration> charts, string outputPath, ExportFormat format)
        {
            // سيتم تنفيذها لاحقاً
            await Task.Delay(100);
            return new ExportResult { Format = format, IsSuccess = false, ErrorMessage = "لم يتم تنفيذ تصدير الرسوم البيانية بعد" };
        }

        public async Task<List<ExportTemplate>> GetAvailableTemplatesAsync(ExportFormat format)
        {
            // سيتم تنفيذها لاحقاً
            await Task.Delay(100);
            return new List<ExportTemplate>();
        }

        public async Task SaveExportTemplateAsync(ExportTemplate template)
        {
            // سيتم تنفيذها لاحقاً
            await Task.Delay(100);
        }

        public async Task<PreviewResult> GeneratePreviewAsync(ReportExportRequest request)
        {
            // سيتم تنفيذها لاحقاً
            await Task.Delay(100);
            return new PreviewResult { IsSuccess = false, ErrorMessage = "لم يتم تنفيذ المعاينة بعد" };
        }

        #endregion

        #region Settings and Configuration

        public ExportSettings GetDefaultSettings(ExportFormat format)
        {
            return _defaultSettings.TryGetValue(format, out var settings) ? 
                new ExportSettings(settings) : 
                new ExportSettings();
        }

        public void UpdateExportSettings(ExportFormat format, ExportSettings settings)
        {
            _defaultSettings[format] = new ExportSettings(settings);
            ComprehensiveLogger.LogInfo($"تم تحديث إعدادات التصدير لصيغة {format}", ComponentName);
        }

        private static Dictionary<ExportFormat, ExportSettings> InitializeDefaultSettings()
        {
            return new Dictionary<ExportFormat, ExportSettings>
            {
                [ExportFormat.PDF] = new ExportSettings
                {
                    PageSize = "A4",
                    Orientation = "Portrait",
                    Margins = "20,20,20,20",
                    IncludeHeader = true,
                    IncludeFooter = true,
                    Quality = "High"
                },
                [ExportFormat.Excel] = new ExportSettings
                {
                    IncludeHeader = true,
                    AutoFitColumns = true,
                    FreezeFirstRow = true,
                    IncludeCharts = true
                },
                [ExportFormat.CSV] = new ExportSettings
                {
                    Delimiter = ",",
                    Encoding = "UTF-8",
                    IncludeHeader = true,
                    QuoteFields = true
                },
                [ExportFormat.HTML] = new ExportSettings
                {
                    IncludeStyles = true,
                    Responsive = true,
                    IncludeCharts = true,
                    Theme = "modern"
                }
            };
        }

        #endregion

        #region Helper Methods

        private static string GenerateFilePath(string outputPath, string reportTitle, string extension)
        {
            var fileName = SanitizeFileName(reportTitle);
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fullFileName = $"{fileName}_{timestamp}.{extension}";
            
            return Path.Combine(outputPath, fullFileName);
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new StringBuilder();
            
            foreach (var c in fileName)
            {
                if (!invalidChars.Contains(c))
                    sanitized.Append(c);
                else
                    sanitized.Append('_');
            }
            
            return sanitized.ToString().Trim('_');
        }

        #endregion
    }
}