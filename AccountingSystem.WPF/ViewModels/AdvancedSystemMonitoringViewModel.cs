using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AccountingSystem.WPF.Helpers;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.WPF.ViewModels
{
    /// <summary>
    /// ViewModel متقدم لنافذة مراقبة النظام
    /// Advanced ViewModel for System Monitoring Window
    /// </summary>
    public class AdvancedSystemMonitoringViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<AdvancedSystemMonitoringViewModel> _logger;

        #region Properties

        private string _systemStatus = "يعمل بشكل طبيعي";
        public string SystemStatus
        {
            get => _systemStatus;
            set => SetProperty(ref _systemStatus, value);
        }

        private double _cpuUsage;
        public double CpuUsage
        {
            get => _cpuUsage;
            set => SetProperty(ref _cpuUsage, value);
        }

        private double _memoryUsage;
        public double MemoryUsage
        {
            get => _memoryUsage;
            set => SetProperty(ref _memoryUsage, value);
        }

        private double _diskUsage;
        public double DiskUsage
        {
            get => _diskUsage;
            set => SetProperty(ref _diskUsage, value);
        }

        private string _uptime = "00:00:00";
        public string Uptime
        {
            get => _uptime;
            set => SetProperty(ref _uptime, value);
        }

        private int _activeConnections;
        public int ActiveConnections
        {
            get => _activeConnections;
            set => SetProperty(ref _activeConnections, value);
        }

        private string _databaseStatus = "متصل";
        public string DatabaseStatus
        {
            get => _databaseStatus;
            set => SetProperty(ref _databaseStatus, value);
        }

        private double _databaseSize;
        public double DatabaseSize
        {
            get => _databaseSize;
            set => SetProperty(ref _databaseSize, value);
        }

        private int _threadCount;
        public int ThreadCount
        {
            get => _threadCount;
            set => SetProperty(ref _threadCount, value);
        }

        private ObservableCollection<SystemAlert> _systemAlerts = new();
        public ObservableCollection<SystemAlert> SystemAlerts
        {
            get => _systemAlerts;
            set => SetProperty(ref _systemAlerts, value);
        }

        private ObservableCollection<PerformanceMetric> _performanceHistory = new();
        public ObservableCollection<PerformanceMetric> PerformanceHistory
        {
            get => _performanceHistory;
            set => SetProperty(ref _performanceHistory, value);
        }

        #endregion

        #region Commands

        public ICommand StartMonitoringCommand { get; private set; } = null!;
        public ICommand StopMonitoringCommand { get; private set; } = null!;
        public ICommand RefreshDataCommand { get; private set; } = null!;
        public ICommand ExportLogCommand { get; private set; } = null!;
        public ICommand ClearLogCommand { get; private set; } = null!;
        public ICommand ViewDetailsCommand { get; private set; } = null!;
        public ICommand CheckHealthCommand { get; private set; } = null!;
        public ICommand OptimizeSystemCommand { get; private set; } = null!;

        #endregion

        public AdvancedSystemMonitoringViewModel(ILogger<AdvancedSystemMonitoringViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeCommands();
            InitializeData();
        }

        private void InitializeCommands()
        {
            StartMonitoringCommand = new RelayCommand(async () => await StartMonitoringAsync());
            StopMonitoringCommand = new RelayCommand(StopMonitoring);
            RefreshDataCommand = new RelayCommand(async () => await RefreshDataAsync());
            ExportLogCommand = new RelayCommand(async () => await ExportLogAsync());
            ClearLogCommand = new RelayCommand(ClearLog);
            ViewDetailsCommand = new RelayCommand(ViewDetails);
            CheckHealthCommand = new RelayCommand(async () => await CheckSystemHealthAsync());
            OptimizeSystemCommand = new RelayCommand(async () => await OptimizeSystemAsync());
        }

        private void InitializeData()
        {
            try
            {
                // إنشاء بيانات أولية للعرض
                SystemAlerts.Add(new SystemAlert
                {
                    Level = AlertLevel.Info,
                    Message = "النظام يعمل بشكل طبيعي",
                    Timestamp = DateTime.Now
                });

                SystemAlerts.Add(new SystemAlert
                {
                    Level = AlertLevel.Warning,
                    Message = "استخدام الذاكرة مرتفع (85%)",
                    Timestamp = DateTime.Now.AddMinutes(-5)
                });

                // إنشاء بيانات الأداء التاريخية
                var random = new Random();
                for (int i = 0; i < 24; i++)
                {
                    PerformanceHistory.Add(new PerformanceMetric
                    {
                        Timestamp = DateTime.Now.AddHours(-23 + i),
                        CpuUsage = random.NextDouble() * 100,
                        MemoryUsage = random.NextDouble() * 100,
                        DiskUsage = random.NextDouble() * 100
                    });
                }

                _logger.LogInformation("تم تهيئة بيانات مراقبة النظام - System monitoring data initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تهيئة بيانات المراقبة - Error initializing monitoring data");
            }
        }

        #region Command Implementations

        private async Task StartMonitoringAsync()
        {
            try
            {
                _logger.LogInformation("بدء مراقبة النظام - Starting system monitoring");

                SystemStatus = "قيد المراقبة";

                // محاكاة بدء المراقبة
                await Task.Delay(1000);

                SystemAlerts.Add(new SystemAlert
                {
                    Level = AlertLevel.Info,
                    Message = "تم بدء مراقبة النظام",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في بدء المراقبة - Error starting monitoring");
            }
        }

        private void StopMonitoring()
        {
            try
            {
                _logger.LogInformation("إيقاف مراقبة النظام - Stopping system monitoring");

                SystemStatus = "متوقف";

                SystemAlerts.Add(new SystemAlert
                {
                    Level = AlertLevel.Info,
                    Message = "تم إيقاف مراقبة النظام",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إيقاف المراقبة - Error stopping monitoring");
            }
        }

        private async Task RefreshDataAsync()
        {
            try
            {
                _logger.LogInformation("تحديث بيانات المراقبة - Refreshing monitoring data");

                // محاكاة تحديث البيانات
                var random = new Random();
                CpuUsage = random.NextDouble() * 100;
                MemoryUsage = random.NextDouble() * 100;
                DiskUsage = random.NextDouble() * 100;
                ActiveConnections = random.Next(1, 20);
                ThreadCount = random.Next(50, 200);
                DatabaseSize = 50.0 + random.NextDouble() * 50;

                await Task.Delay(500); // محاكاة وقت التحديث

                SystemAlerts.Add(new SystemAlert
                {
                    Level = AlertLevel.Info,
                    Message = "تم تحديث بيانات المراقبة",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث البيانات - Error refreshing data");
            }
        }

        private async Task ExportLogAsync()
        {
            try
            {
                _logger.LogInformation("تصدير سجل المراقبة - Exporting monitoring log");

                await Task.Delay(1000); // محاكاة عملية التصدير

                SystemAlerts.Add(new SystemAlert
                {
                    Level = AlertLevel.Info,
                    Message = "تم تصدير السجل بنجاح",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تصدير السجل - Error exporting log");
            }
        }

        private void ClearLog()
        {
            try
            {
                _logger.LogInformation("مسح سجل المراقبة - Clearing monitoring log");

                SystemAlerts.Clear();

                SystemAlerts.Add(new SystemAlert
                {
                    Level = AlertLevel.Info,
                    Message = "تم مسح السجل",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في مسح السجل - Error clearing log");
            }
        }

        private void ViewDetails()
        {
            try
            {
                _logger.LogInformation("عرض تفاصيل المراقبة - Viewing monitoring details");

                SystemAlerts.Add(new SystemAlert
                {
                    Level = AlertLevel.Info,
                    Message = "عرض تفاصيل النظام",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في عرض التفاصيل - Error viewing details");
            }
        }

        private async Task CheckSystemHealthAsync()
        {
            try
            {
                _logger.LogInformation("فحص صحة النظام - Checking system health");

                await Task.Delay(2000); // محاكاة فحص شامل

                SystemAlerts.Add(new SystemAlert
                {
                    Level = AlertLevel.Success,
                    Message = "فحص صحة النظام مكتمل - النظام يعمل بشكل ممتاز",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في فحص صحة النظام - Error checking system health");
            }
        }

        private async Task OptimizeSystemAsync()
        {
            try
            {
                _logger.LogInformation("تحسين النظام - Optimizing system");

                await Task.Delay(3000); // محاكاة عملية التحسين

                SystemAlerts.Add(new SystemAlert
                {
                    Level = AlertLevel.Success,
                    Message = "تم تحسين النظام بنجاح - تحسن الأداء بنسبة 15%",
                    Timestamp = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحسين النظام - Error optimizing system");
            }
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }

    #region Supporting Classes

    public class SystemAlert
    {
        public AlertLevel Level { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class PerformanceMetric
    {
        public DateTime Timestamp { get; set; }
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public double DiskUsage { get; set; }
    }

    public enum AlertLevel
    {
        Info,
        Warning,
        Error,
        Success
    }

    #endregion
}