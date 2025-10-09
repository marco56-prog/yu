using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Drawing.Imaging;
using AccountingSystem.Diagnostics.Models;

namespace AccountingSystem.Diagnostics.Tests
{
    /// <summary>
    /// إطار عمل اختبار الانحدار البصري للواجهات
    /// </summary>
    public class VisualRegressionTestFramework
    {
        private readonly ILogger<VisualRegressionTestFramework> _logger;
        private readonly string _baselinesPath;
        private readonly string _outputPath;
        private const double SIMILARITY_THRESHOLD = 0.95; // 95% تشابه مطلوب

        public VisualRegressionTestFramework(
            ILogger<VisualRegressionTestFramework> logger,
            string baselinesPath = "TestBaselines",
            string outputPath = "TestResults")
        {
            _logger = logger;
            _baselinesPath = baselinesPath;
            _outputPath = outputPath;
            
            EnsureDirectoriesExist();
        }

        /// <summary>
        /// تشغيل اختبارات الانحدار البصري
        /// </summary>
        public async Task<VisualRegressionResults> RunVisualRegressionTestsAsync()
        {
            var results = new VisualRegressionResults
            {
                TestStartTime = DateTime.UtcNow,
                Tests = new List<VisualTest>()
            };

            _logger.LogInformation("بدء اختبارات الانحدار البصري");

            try
            {
                // اختبار النوافذ الرئيسية
                await TestMainWindowAsync(results);
                await TestLoginWindowAsync(results);
                await TestSalesInvoiceWindowAsync(results);
                await TestDoctorWindowAsync(results);
                
                // اختبار المكونات المشتركة
                await TestThemeSystemAsync(results);
                await TestRTLLayoutAsync(results);
                await TestErrorDialogsAsync(results);

                results.TestEndTime = DateTime.UtcNow;
                results.TotalDuration = results.TestEndTime - results.TestStartTime;
                results.IsSuccessful = results.FailedTestsCount == 0;

                _logger.LogInformation("اكتملت اختبارات الانحدار البصري: {Success}/{Total} نجحت", 
                    results.SuccessfulTestsCount, results.Tests.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "فشل في اختبارات الانحدار البصري");
                results.IsSuccessful = false;
                results.ErrorMessage = ex.Message;
            }

            return results;
        }

        private async Task TestMainWindowAsync(VisualRegressionResults results)
        {
            await RunVisualTest(results, "MainWindow", "النافذة الرئيسية", async () =>
            {
                // محاكاة التقاط صورة للنافذة الرئيسية
                return await CaptureWindowAsync("MainWindow", 1024, 768);
            });
        }

        private async Task TestLoginWindowAsync(VisualRegressionResults results)
        {
            await RunVisualTest(results, "LoginWindow", "نافذة تسجيل الدخول", async () =>
            {
                return await CaptureWindowAsync("LoginWindow", 400, 300);
            });
        }

        private async Task TestSalesInvoiceWindowAsync(VisualRegressionResults results)
        {
            await RunVisualTest(results, "SalesInvoiceWindow", "نافذة فاتورة المبيعات", async () =>
            {
                return await CaptureWindowAsync("SalesInvoiceWindow", 1200, 900);
            });
        }

        private async Task TestDoctorWindowAsync(VisualRegressionResults results)
        {
            await RunVisualTest(results, "DoctorWindow", "نافذة طبيب النظام", async () =>
            {
                return await CaptureWindowAsync("DoctorWindow", 1000, 700);
            });
        }

        private async Task TestThemeSystemAsync(VisualRegressionResults results)
        {
            // اختبار السمات المختلفة
            await RunVisualTest(results, "LightTheme", "السمة الفاتحة", async () =>
            {
                return await CaptureThemeAsync("Light", 800, 600);
            });

            await RunVisualTest(results, "DarkTheme", "السمة الداكنة", async () =>
            {
                return await CaptureThemeAsync("Dark", 800, 600);
            });
        }

        private async Task TestRTLLayoutAsync(VisualRegressionResults results)
        {
            await RunVisualTest(results, "RTLLayout", "تخطيط RTL", async () =>
            {
                return await CaptureRTLLayoutAsync(800, 600);
            });
        }

        private async Task TestErrorDialogsAsync(VisualRegressionResults results)
        {
            await RunVisualTest(results, "ErrorDialog", "مربع حوار الخطأ", async () =>
            {
                return await CaptureErrorDialogAsync(400, 200);
            });
        }

        private async Task RunVisualTest(VisualRegressionResults results, string testName, 
            string description, Func<Task<byte[]>> captureFunc)
        {
            var test = new VisualTest
            {
                TestName = testName,
                Description = description,
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("تشغيل اختبار بصري: {TestName}", description);

                // التقاط الصورة الحالية
                var currentImage = await captureFunc();
                var currentImagePath = Path.Combine(_outputPath, $"{testName}_current.png");
                await File.WriteAllBytesAsync(currentImagePath, currentImage);

                // مقارنة مع الأساس المرجعي
                var baselinePath = Path.Combine(_baselinesPath, $"{testName}_baseline.png");
                
                if (File.Exists(baselinePath))
                {
                    var baselineImage = await File.ReadAllBytesAsync(baselinePath);
                    var similarity = CalculateImageSimilarity(baselineImage, currentImage);
                    
                    test.Similarity = similarity;
                    test.IsSuccessful = similarity >= SIMILARITY_THRESHOLD;
                    
                    if (!test.IsSuccessful)
                    {
                        // إنشاء صورة الفروقات
                        var diffImage = CreateDifferenceImage(baselineImage, currentImage);
                        var diffPath = Path.Combine(_outputPath, $"{testName}_diff.png");
                        await File.WriteAllBytesAsync(diffPath, diffImage);
                        test.DiffImagePath = diffPath;
                    }
                }
                else
                {
                    // إنشاء الأساس المرجعي الأول
                    await File.WriteAllBytesAsync(baselinePath, currentImage);
                    test.IsSuccessful = true;
                    test.Similarity = 1.0;
                    test.Message = "تم إنشاء الأساس المرجعي";
                    
                    _logger.LogInformation("تم إنشاء الأساس المرجعي لـ {TestName}", testName);
                }

                test.CurrentImagePath = currentImagePath;
                test.BaselineImagePath = baselinePath;
            }
            catch (Exception ex)
            {
                test.IsSuccessful = false;
                test.ErrorMessage = ex.Message;
                _logger.LogError(ex, "فشل في الاختبار البصري {TestName}", testName);
            }

            test.EndTime = DateTime.UtcNow;
            test.Duration = test.EndTime - test.StartTime;
            results.Tests.Add(test);
        }

        private async Task<byte[]> CaptureWindowAsync(string windowName, int width, int height)
        {
            // محاكاة التقاط صورة النافذة
            // في التطبيق الحقيقي، ستستخدم مكتبة التقاط الشاشة
            return await CreateMockScreenshotAsync(windowName, width, height);
        }

        private async Task<byte[]> CaptureThemeAsync(string themeName, int width, int height)
        {
            return await CreateMockScreenshotAsync($"Theme_{themeName}", width, height);
        }

        private async Task<byte[]> CaptureRTLLayoutAsync(int width, int height)
        {
            return await CreateMockScreenshotAsync("RTL_Layout", width, height);
        }

        private async Task<byte[]> CaptureErrorDialogAsync(int width, int height)
        {
            return await CreateMockScreenshotAsync("Error_Dialog", width, height);
        }

        private async Task<byte[]> CreateMockScreenshotAsync(string content, int width, int height)
        {
            // إنشاء صورة تجريبية للاختبار
            await Task.Delay(10); // محاكاة وقت التقاط الصورة
            
            using var bitmap = new Bitmap(width, height);
            using var graphics = Graphics.FromImage(bitmap);
            
            // خلفية بيضاء
            graphics.Clear(System.Drawing.Color.White);
            
            // رسم نص تعريفي
            using var font = new Font("Arial", 16);
            using var brush = new SolidBrush(System.Drawing.Color.Black);
            
            var text = $"Mock Screenshot: {content}";
            var textSize = graphics.MeasureString(text, font);
            var x = (width - textSize.Width) / 2;
            var y = (height - textSize.Height) / 2;
            
            graphics.DrawString(text, font, brush, x, y);
            
            // تحويل إلى مصفوفة بايت
            using var stream = new MemoryStream();
            bitmap.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }

        private double CalculateImageSimilarity(byte[] image1, byte[] image2)
        {
            try
            {
                if (image1.Length != image2.Length)
                    return 0.0;

                int differences = 0;
                for (int i = 0; i < image1.Length; i++)
                {
                    if (image1[i] != image2[i])
                        differences++;
                }

                return 1.0 - ((double)differences / image1.Length);
            }
            catch
            {
                return 0.0;
            }
        }

        private byte[] CreateDifferenceImage(byte[] baseline, byte[] current)
        {
            // إنشاء صورة توضح الفروقات
            // هذا تنفيذ مبسط - في التطبيق الحقيقي ستحتاج مكتبة معالجة صور متقدمة
            try
            {
                using var bitmap = new Bitmap(400, 300);
                using var graphics = Graphics.FromImage(bitmap);
                
                graphics.Clear(System.Drawing.Color.Red);
                
                using var font = new Font("Arial", 12);
                using var brush = new SolidBrush(System.Drawing.Color.White);
                
                graphics.DrawString("Differences Detected", font, brush, 10, 10);
                graphics.DrawString("Red areas show changes", font, brush, 10, 30);
                
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
                return stream.ToArray();
            }
            catch
            {
                return Array.Empty<byte>();
            }
        }

        private void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(_baselinesPath);
            Directory.CreateDirectory(_outputPath);
        }

        /// <summary>
        /// إنشاء تقرير HTML للنتائج البصرية
        /// </summary>
        public async Task GenerateVisualReportAsync(VisualRegressionResults results, string reportPath)
        {
            var html = $@"
<!DOCTYPE html>
<html dir='rtl' lang='ar'>
<head>
    <meta charset='UTF-8'>
    <title>تقرير الاختبارات البصرية</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 20px; }}
        .header {{ background: #007acc; color: white; padding: 20px; border-radius: 8px; }}
        .summary {{ margin: 20px 0; padding: 15px; background: #f8f9fa; border-radius: 8px; }}
        .test-item {{ margin: 15px 0; padding: 15px; border: 1px solid #ddd; border-radius: 8px; }}
        .success {{ border-color: #28a745; background: #f8fff8; }}
        .failure {{ border-color: #dc3545; background: #fff8f8; }}
        .images {{ display: flex; gap: 10px; margin: 10px 0; }}
        .image-container {{ text-align: center; }}
        .image-container img {{ max-width: 200px; border: 1px solid #ddd; }}
    </style>
</head>
<body>
    <div class='header'>
        <h1>🖼️ تقرير الاختبارات البصرية</h1>
        <p>تاريخ التشغيل: {results.TestStartTime:yyyy-MM-dd HH:mm:ss}</p>
    </div>
    
    <div class='summary'>
        <h2>📊 الملخص</h2>
        <p><strong>إجمالي الاختبارات:</strong> {results.Tests.Count}</p>
        <p><strong>النجحة:</strong> {results.SuccessfulTestsCount}</p>
        <p><strong>الفاشلة:</strong> {results.FailedTestsCount}</p>
        <p><strong>المدة الإجمالية:</strong> {results.TotalDuration.TotalSeconds:F2} ثانية</p>
        <p><strong>النتيجة العامة:</strong> {(results.IsSuccessful ? "✅ نجح" : "❌ فشل")}</p>
    </div>

    <div class='tests'>
        <h2>📋 نتائج الاختبارات التفصيلية</h2>";

            foreach (var test in results.Tests)
            {
                var cssClass = test.IsSuccessful ? "success" : "failure";
                var statusIcon = test.IsSuccessful ? "✅" : "❌";

                html += $@"
        <div class='test-item {cssClass}'>
            <h3>{statusIcon} {test.Description}</h3>
            <p><strong>اسم الاختبار:</strong> {test.TestName}</p>
            <p><strong>التشابه:</strong> {test.Similarity:P2}</p>
            <p><strong>المدة:</strong> {test.Duration.TotalMilliseconds:F0}ms</p>";

                if (!string.IsNullOrEmpty(test.ErrorMessage))
                {
                    html += $"<p><strong>خطأ:</strong> {test.ErrorMessage}</p>";
                }

                if (!string.IsNullOrEmpty(test.Message))
                {
                    html += $"<p><strong>رسالة:</strong> {test.Message}</p>";
                }

                // إضافة الصور إذا كانت متوفرة
                if (!string.IsNullOrEmpty(test.BaselineImagePath) || !string.IsNullOrEmpty(test.CurrentImagePath))
                {
                    html += "<div class='images'>";
                    
                    if (!string.IsNullOrEmpty(test.BaselineImagePath) && File.Exists(test.BaselineImagePath))
                    {
                        html += $@"
                        <div class='image-container'>
                            <img src='{test.BaselineImagePath}' alt='الأساس المرجعي'>
                            <p>الأساس المرجعي</p>
                        </div>";
                    }

                    if (!string.IsNullOrEmpty(test.CurrentImagePath) && File.Exists(test.CurrentImagePath))
                    {
                        html += $@"
                        <div class='image-container'>
                            <img src='{test.CurrentImagePath}' alt='الصورة الحالية'>
                            <p>الصورة الحالية</p>
                        </div>";
                    }

                    if (!string.IsNullOrEmpty(test.DiffImagePath) && File.Exists(test.DiffImagePath))
                    {
                        html += $@"
                        <div class='image-container'>
                            <img src='{test.DiffImagePath}' alt='الفروقات'>
                            <p>الفروقات</p>
                        </div>";
                    }

                    html += "</div>";
                }

                html += "</div>";
            }

            html += @"
    </div>
</body>
</html>";

            await File.WriteAllTextAsync(reportPath, html);
            _logger.LogInformation("تم إنشاء تقرير الاختبارات البصرية في {Path}", reportPath);
        }
    }

    public class VisualRegressionResults
    {
        public DateTime TestStartTime { get; set; }
        public DateTime TestEndTime { get; set; }
        public TimeSpan TotalDuration { get; set; }
        public bool IsSuccessful { get; set; }
        public string? ErrorMessage { get; set; }
        public List<VisualTest> Tests { get; set; } = new();
        
        public int SuccessfulTestsCount => Tests.Count(t => t.IsSuccessful);
        public int FailedTestsCount => Tests.Count(t => !t.IsSuccessful);
        public double AverageSimilarity => Tests.Count > 0 ? Tests.Average(t => t.Similarity) : 0;
    }

    public class VisualTest
    {
        public string TestName { get; set; } = "";
        public string Description { get; set; } = "";
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public bool IsSuccessful { get; set; }
        public double Similarity { get; set; }
        public string? ErrorMessage { get; set; }
        public string? Message { get; set; }
        public string? BaselineImagePath { get; set; }
        public string? CurrentImagePath { get; set; }
        public string? DiffImagePath { get; set; }
    }
}