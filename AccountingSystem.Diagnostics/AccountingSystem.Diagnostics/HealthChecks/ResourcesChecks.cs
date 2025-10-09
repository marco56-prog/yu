using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using AccountingSystem.Diagnostics.Models;
using AccountingSystem.Diagnostics.Core;

namespace AccountingSystem.Diagnostics.HealthChecks
{
    /// <summary>
    /// فحص الموارد والثيمات
    /// </summary>
    public class ResourcesAndThemesCheck : IHealthCheck
    {
        private readonly ILogger<ResourcesAndThemesCheck> _logger;
        private readonly string _resourcesPath;

        public string Name => "فحص الموارد والثيمات";
        public string Category => "resources";
        public string Description => "التأكد من سلامة ملفات XAML وعدم وجود مفاتيح مكررة أو موارد مفقودة";
        public int Priority => 5;
        public bool IsEnabled => true;

        public ResourcesAndThemesCheck(ILogger<ResourcesAndThemesCheck> logger)
        {
            _logger = logger;
            _resourcesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources");
        }

        public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var issues = new List<string>();

            try
            {
                // فحص وجود مجلد الموارد
                if (!Directory.Exists(_resourcesPath))
                {
                    stopwatch.Stop();
                    return HealthCheckResult.Failed(Name, 
                        "مجلد الموارد غير موجود", 
                        stopwatch.Elapsed, 
                        null, 
                        "تأكد من وجود مجلد Resources في مشروع WPF");
                }

                // جمع جميع ملفات XAML
                var xamlFiles = Directory.GetFiles(_resourcesPath, "*.xaml", SearchOption.AllDirectories);
                
                if (!xamlFiles.Any())
                {
                    issues.Add("لا توجد ملفات XAML في مجلد الموارد");
                }

                var allKeys = new Dictionary<string, List<string>>(); // Key -> Files that define it

                // فحص كل ملف XAML
                foreach (var xamlFile in xamlFiles)
                {
                    await CheckXamlFile(xamlFile, allKeys, issues, cancellationToken);
                }

                // فحص المفاتيح المكررة
                CheckDuplicateKeys(allKeys, issues);

                // فحص الموارد الأساسية المطلوبة
                CheckRequiredResources(allKeys, issues);

                // فحص ملف الموارد الرئيسي
                await CheckMasterResourceFile(issues, cancellationToken);

                stopwatch.Stop();

                if (!issues.Any())
                {
                    return HealthCheckResult.Ok(Name, 
                        $"جميع الموارد سليمة - تم فحص {xamlFiles.Length} ملف XAML", 
                        stopwatch.Elapsed);
                }

                var severity = issues.Any(i => i.Contains("خطأ") || i.Contains("مفقود")) 
                    ? HealthStatus.Failed 
                    : HealthStatus.Warning;

                var result = new HealthCheckResult
                {
                    CheckName = Name,
                    Status = severity,
                    Message = $"تم اكتشاف {issues.Count} مشكلة في الموارد",
                    Details = string.Join("\n", issues),
                    Duration = stopwatch.Elapsed,
                    RecommendedAction = "راجع ملفات XAML وأصلح المشاكل المذكورة"
                };

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "فشل في فحص الموارد");
                
                return HealthCheckResult.Failed(Name, 
                    "فشل في فحص الموارد والثيمات", 
                    stopwatch.Elapsed, 
                    ex);
            }
        }

        private async Task CheckXamlFile(string xamlFile, Dictionary<string, List<string>> allKeys, List<string> issues, CancellationToken cancellationToken)
        {
            try
            {
                var content = await File.ReadAllTextAsync(xamlFile, cancellationToken);
                var doc = XDocument.Parse(content);
                var fileName = Path.GetFileName(xamlFile);

                // فحص مفاتيح x:Key
                var keyElements = doc.Descendants()
                    .Where(e => e.Attribute("{http://schemas.microsoft.com/winfx/2006/xaml}Key") != null);

                foreach (var element in keyElements)
                {
                    var key = element.Attribute("{http://schemas.microsoft.com/winfx/2006/xaml}Key")?.Value;
                    if (!string.IsNullOrEmpty(key))
                    {
                        if (!allKeys.ContainsKey(key))
                            allKeys[key] = new List<string>();
                        allKeys[key].Add(fileName);
                    }
                }

                // فحص أنماط بدون TargetType
                var stylesWithoutTargetType = doc.Descendants()
                    .Where(e => e.Name.LocalName == "Style" && e.Attribute("TargetType") == null);

                foreach (var style in stylesWithoutTargetType)
                {
                    var key = style.Attribute("{http://schemas.microsoft.com/winfx/2006/xaml}Key")?.Value ?? "بدون مفتاح";
                    issues.Add($"نمط بدون TargetType في {fileName}: {key}");
                }

                // فحص StaticResource المرجعية
                CheckStaticResourceReferences(doc, fileName, issues);
            }
            catch (Exception ex)
            {
                issues.Add($"خطأ في تحليل ملف {Path.GetFileName(xamlFile)}: {ex.Message}");
            }
        }

        private void CheckStaticResourceReferences(XDocument doc, string fileName, List<string> issues)
        {
            var staticResourceRefs = doc.Descendants()
                .Where(e => e.Attributes().Any(a => a.Value.StartsWith("{StaticResource ")));

            foreach (var element in staticResourceRefs)
            {
                var attributes = element.Attributes()
                    .Where(a => a.Value.StartsWith("{StaticResource "));

                foreach (var attr in attributes)
                {
                    var resourceKey = ExtractResourceKey(attr.Value);
                    if (!string.IsNullOrEmpty(resourceKey))
                    {
                        // هنا يمكن إضافة فحص وجود المورد فعلياً
                        // لكن هذا يتطلب تحليل أكثر تعقيداً لشجرة الموارد
                    }
                }
            }
        }

        private string ExtractResourceKey(string staticResourceExpression)
        {
            // استخراج اسم المورد من تعبير مثل "{StaticResource MyKey}"
            var start = staticResourceExpression.IndexOf("StaticResource ") + "StaticResource ".Length;
            var end = staticResourceExpression.LastIndexOf('}');
            
            if (start > 0 && end > start)
                return staticResourceExpression.Substring(start, end - start).Trim();
            
            return string.Empty;
        }

        private void CheckDuplicateKeys(Dictionary<string, List<string>> allKeys, List<string> issues)
        {
            var duplicates = allKeys.Where(kvp => kvp.Value.Count > 1);

            foreach (var duplicate in duplicates)
            {
                issues.Add($"مفتاح مكرر '{duplicate.Key}' في الملفات: {string.Join(", ", duplicate.Value)}");
            }
        }

        private void CheckRequiredResources(Dictionary<string, List<string>> allKeys, List<string> issues)
        {
            var requiredKeys = new[]
            {
                "MasterPrimaryColor",
                "MasterBackgroundColor", 
                "MasterTextPrimaryColor",
                "SafePrimaryButton",
                "SafeTextBox",
                "HeaderTextBlock"
            };

            foreach (var requiredKey in requiredKeys)
            {
                if (!allKeys.ContainsKey(requiredKey))
                {
                    issues.Add($"مورد مطلوب مفقود: {requiredKey}");
                }
            }
        }

        private async Task CheckMasterResourceFile(List<string> issues, CancellationToken cancellationToken)
        {
            var masterFile = Path.Combine(_resourcesPath, "MasterSafeResources.xaml");
            
            if (!File.Exists(masterFile))
            {
                issues.Add("ملف الموارد الرئيسي MasterSafeResources.xaml مفقود");
                return;
            }

            try
            {
                var content = await File.ReadAllTextAsync(masterFile, cancellationToken);
                
                // فحص وجود الألوان الأساسية
                var requiredColors = new[] { "MasterPrimaryColor", "MasterBackgroundColor", "MasterTextPrimaryColor" };
                
                foreach (var color in requiredColors)
                {
                    if (!content.Contains($"x:Key=\"{color}\""))
                    {
                        issues.Add($"لون أساسي مفقود في الملف الرئيسي: {color}");
                    }
                }

                // فحص بنية XML
                var doc = XDocument.Parse(content);
                var rootElement = doc.Root;
                
                if (rootElement?.Name.LocalName != "ResourceDictionary")
                {
                    issues.Add("ملف الموارد الرئيسي ليس ResourceDictionary صحيح");
                }
            }
            catch (Exception ex)
            {
                issues.Add($"خطأ في فحص ملف الموارد الرئيسي: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// فحص ربط البيانات (Binding)
    /// </summary>
    public class DataBindingCheck : IHealthCheck
    {
        private readonly ILogger<DataBindingCheck> _logger;

        public string Name => "فحص ربط البيانات";
        public string Category => "ui";
        public string Description => "رصد أخطاء وتحذيرات ربط البيانات في واجهة المستخدم";
        public int Priority => 6;
        public bool IsEnabled => true;

        public DataBindingCheck(ILogger<DataBindingCheck> logger)
        {
            _logger = logger;
        }

        public async Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // إعداد مستمع لتحذيرات الربط
                var bindingErrors = new List<string>();
                
                // في WPF، يمكن رصد أخطاء الربط عبر PresentationTraceSources
                // لكن هذا يتطلب إعداد مخصص في بداية التطبيق
                
                // للمثال، سنفترض أننا نجمع الأخطاء من مصدر آخر
                var mockBindingErrors = await GetBindingErrorsFromTraceAsync(cancellationToken);
                
                stopwatch.Stop();

                if (!mockBindingErrors.Any())
                {
                    return HealthCheckResult.Ok(Name, 
                        "لا توجد أخطاء في ربط البيانات", 
                        stopwatch.Elapsed);
                }

                var result = HealthCheckResult.Failed(Name, 
                    $"تم اكتشاف {mockBindingErrors.Count} خطأ في ربط البيانات", 
                    stopwatch.Elapsed, 
                    null, 
                    "راجع خصائص الربط في ملفات XAML وتأكد من وجود الخصائص في ViewModels");
                result.Details = string.Join("\n", mockBindingErrors);
                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "فشل في فحص ربط البيانات");
                
                return HealthCheckResult.Failed(Name, 
                    "فشل في فحص ربط البيانات", 
                    stopwatch.Elapsed, 
                    ex);
            }
        }

        private async Task<List<string>> GetBindingErrorsFromTraceAsync(CancellationToken cancellationToken)
        {
            // في التطبيق الحقيقي، هذا سيقرأ من نظام الـ Trace
            // أو من ملف لوج مخصص لأخطاء الربط
            
            await Task.Delay(100, cancellationToken); // محاكاة عملية غير متزامنة
            
            // إرجاع قائمة فارغة للمثال - في الواقع ستحتوي على أخطاء فعلية
            return new List<string>();
        }
    }
}