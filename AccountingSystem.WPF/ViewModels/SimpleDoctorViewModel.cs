using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.WPF.ViewModels
{
    /// <summary>
    /// نموذج مبسط لاختبار نافذة الطبيب
    /// </summary>
    public class SimpleDoctorViewModel : BaseViewModel
    {
        private readonly ILogger<SimpleDoctorViewModel> _logger;
        private bool _isRunning;
        private double _progressValue;
        private string _statusMessage = "جاهز للبدء";
        private string _lastCheckTime = "لم يتم تشغيل فحص بعد";

        public SimpleDoctorViewModel(ILogger<SimpleDoctorViewModel> logger)
        {
            _logger = logger;
            Results = new ObservableCollection<SimpleHealthCheckResult>();
            
            // الأوامر
            RunDiagnosticsCommand = new RelayCommand(RunDiagnostics, () => !IsRunning);
            FixIssuesCommand = new RelayCommand(FixIssues, () => !IsRunning && Results.Count > 0);
            RefreshCommand = new RelayCommand(Refresh, () => !IsRunning);
            ExportReportCommand = new RelayCommand(ExportReport, () => Results.Count > 0);
            
            LoadMockData();
        }

        // Properties
        public ObservableCollection<SimpleHealthCheckResult> Results { get; }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                _isRunning = value;
                OnPropertyChanged();
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        public double ProgressValue
        {
            get => _progressValue;
            set
            {
                _progressValue = value;
                OnPropertyChanged();
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged();
            }
        }

        public string LastCheckTime
        {
            get => _lastCheckTime;
            set
            {
                _lastCheckTime = value;
                OnPropertyChanged();
            }
        }

        // عدادات النتائج
        public int OkCount => Results.Count(r => r.Status == "Ok");
        public int WarningCount => Results.Count(r => r.Status == "Warning");
        public int FailedCount => Results.Count(r => r.Status == "Failed");
        public bool IsSystemHealthy => FailedCount == 0;

        // الأوامر
        public ICommand RunDiagnosticsCommand { get; }
        public ICommand FixIssuesCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ExportReportCommand { get; }

        // الطرق
        private async void RunDiagnostics()
        {
            IsRunning = true;
            ProgressValue = 0;
            StatusMessage = "جاري بدء التشخيص...";
            
            try
            {
                Results.Clear();
                
                var checks = new[]
                {
                    "فحص اتصال قاعدة البيانات",
                    "فحص الترحيلات المعلقة", 
                    "فحص موارد النظام",
                    "فحص الثيمات والأنماط",
                    "فحص أداء النظام",
                    "فحص الأمان",
                    "فحص تكامل البيانات",
                    "فحص استهلاك الذاكرة"
                };

                for (int i = 0; i < checks.Length; i++)
                {
                    StatusMessage = $"جاري تشغيل: {checks[i]}";
                    ProgressValue = ((double)(i + 1) / checks.Length) * 100;
                    
                    await Task.Delay(500); // محاكاة وقت المعالجة
                    
                    // إضافة نتيجة عشوائية
                    var status = Random.Shared.Next(10) switch
                    {
                        < 7 => "Ok",
                        < 9 => "Warning", 
                        _ => "Failed"
                    };
                    
                    var result = new SimpleHealthCheckResult
                    {
                        CheckName = checks[i],
                        Status = status,
                        Message = GetMockMessage(status),
                        Category = GetMockCategory(i),
                        DurationText = $"{Random.Shared.Next(50, 500)}ms",
                        StatusIcon = status switch
                        {
                            "Ok" => "✅",
                            "Warning" => "⚠️",
                            "Failed" => "❌",
                            _ => "❓"
                        }
                    };
                    
                    Results.Add(result);
                    OnPropertyChanged(nameof(OkCount));
                    OnPropertyChanged(nameof(WarningCount));
                    OnPropertyChanged(nameof(FailedCount));
                    OnPropertyChanged(nameof(IsSystemHealthy));
                }
                
                StatusMessage = $"اكتمل التشخيص - {Results.Count} فحص تم تنفيذه";
                LastCheckTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تشغيل التشخيص");
                StatusMessage = "حدث خطأ أثناء التشخيص";
            }
            finally
            {
                IsRunning = false;
                ProgressValue = 100;
            }
        }

        private void FixIssues()
        {
            var issuesCount = Results.Count(r => r.Status != "Ok");
            StatusMessage = $"تم إصلاح {issuesCount} مشكلة بنجاح";
            
            // تحويل جميع المشاكل إلى حالة سليمة
            foreach (var result in Results.Where(r => r.Status != "Ok"))
            {
                result.Status = "Ok";
                result.StatusIcon = "✅";
                result.Message = "تم الإصلاح تلقائياً";
            }
            
            OnPropertyChanged(nameof(OkCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(FailedCount));
            OnPropertyChanged(nameof(IsSystemHealthy));
        }

        private void Refresh()
        {
            LoadMockData();
            StatusMessage = "تم تحديث البيانات";
        }

        private void ExportReport()
        {
            StatusMessage = "تم تصدير التقرير بنجاح";
            // يمكن إضافة منطق التصدير الحقيقي هنا
        }

        private void LoadMockData()
        {
            Results.Clear();
            
            var mockData = new[]
            {
                new SimpleHealthCheckResult { CheckName = "اتصال قاعدة البيانات", Status = "Ok", Message = "الاتصال يعمل بشكل طبيعي", Category = "قاعدة البيانات", DurationText = "45ms", StatusIcon = "✅" },
                new SimpleHealthCheckResult { CheckName = "فحص الترحيلات", Status = "Warning", Message = "يوجد 2 ترحيلة معلقة", Category = "قاعدة البيانات", DurationText = "120ms", StatusIcon = "⚠️" },
                new SimpleHealthCheckResult { CheckName = "فحص الموارد", Status = "Ok", Message = "جميع الموارد متوفرة", Category = "موارد النظام", DurationText = "30ms", StatusIcon = "✅" },
                new SimpleHealthCheckResult { CheckName = "فحص الأمان", Status = "Failed", Message = "ثغرة أمنية مكتشفة", Category = "الأمان", DurationText = "200ms", StatusIcon = "❌" }
            };

            foreach (var item in mockData)
            {
                Results.Add(item);
            }
            
            OnPropertyChanged(nameof(OkCount));
            OnPropertyChanged(nameof(WarningCount));
            OnPropertyChanged(nameof(FailedCount));
            OnPropertyChanged(nameof(IsSystemHealthy));
            
            LastCheckTime = DateTime.Now.AddMinutes(-5).ToString("yyyy-MM-dd HH:mm:ss");
        }

        private string GetMockMessage(string status) => status switch
        {
            "Ok" => "الفحص تم بنجاح بدون مشاكل",
            "Warning" => "تم اكتشاف مشكلة بسيطة تحتاج متابعة",
            "Failed" => "فشل الفحص - يتطلب تدخل فوري",
            _ => "حالة غير معروفة"
        };

        private string GetMockCategory(int index) => index switch
        {
            < 2 => "قاعدة البيانات",
            < 4 => "موارد النظام", 
            < 6 => "الأداء",
            _ => "الأمان"
        };
    }

    /// <summary>
    /// نموذج بسيط لنتيجة الفحص الصحي
    /// </summary>
    public class SimpleHealthCheckResult : BaseViewModel
    {
        private string _status = "";
        private string _statusIcon = "";
        private string _message = "";

        public string CheckName { get; set; } = "";
        
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }
        
        public string StatusIcon
        {
            get => _statusIcon;
            set
            {
                _statusIcon = value;
                OnPropertyChanged();
            }
        }
        
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }
        
        public string Category { get; set; } = "";
        public string DurationText { get; set; } = "";
        public string? Details { get; set; }
        public string? RecommendedAction { get; set; }
    }
}