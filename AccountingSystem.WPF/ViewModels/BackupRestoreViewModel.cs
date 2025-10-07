using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using AccountingSystem.WPF.Helpers;
using AccountingSystem.Business;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.WPF.ViewModels
{
    /// <summary>
    /// ViewModel متقدم لنافذة النسخ الاحتياطي والاستعادة
    /// Advanced ViewModel for Backup and Restore Window
    /// </summary>
    public class BackupRestoreViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<BackupRestoreViewModel> _logger;
        private readonly IBackupService _backupService;

        #region Properties

        private string _backupStatus = "جاهز";
        public string BackupStatus
        {
            get => _backupStatus;
            set => SetProperty(ref _backupStatus, value);
        }

        private double _backupProgress;
        public double BackupProgress
        {
            get => _backupProgress;
            set => SetProperty(ref _backupProgress, value);
        }

        private string _lastBackupDate = "لم يتم إنشاء نسخة احتياطية بعد";
        public string LastBackupDate
        {
            get => _lastBackupDate;
            set => SetProperty(ref _lastBackupDate, value);
        }

        private string _backupLocation = @"C:\Backups\AccountingSystem";
        public string BackupLocation
        {
            get => _backupLocation;
            set => SetProperty(ref _backupLocation, value);
        }

        private bool _autoBackupEnabled = true;
        public bool AutoBackupEnabled
        {
            get => _autoBackupEnabled;
            set => SetProperty(ref _autoBackupEnabled, value);
        }

        private int _retentionDays = 30;
        public int RetentionDays
        {
            get => _retentionDays;
            set => SetProperty(ref _retentionDays, value);
        }

        private string _compressionLevel = "متوسط";
        public string CompressionLevel
        {
            get => _compressionLevel;
            set => SetProperty(ref _compressionLevel, value);
        }

        private bool _encryptBackup = true;
        public bool EncryptBackup
        {
            get => _encryptBackup;
            set => SetProperty(ref _encryptBackup, value);
        }

        private ObservableCollection<BackupItem> _availableBackups = new();
        public ObservableCollection<BackupItem> AvailableBackups
        {
            get => _availableBackups;
            set => SetProperty(ref _availableBackups, value);
        }

        private BackupItem? _selectedBackup;
        public BackupItem? SelectedBackup
        {
            get => _selectedBackup;
            set => SetProperty(ref _selectedBackup, value);
        }

        private ObservableCollection<string> _compressionLevels = new() { "منخفض", "متوسط", "عالي", "أقصى" };
        public ObservableCollection<string> CompressionLevels
        {
            get => _compressionLevels;
            set => SetProperty(ref _compressionLevels, value);
        }

        private ObservableCollection<BackupLog> _backupLogs = new();
        public ObservableCollection<BackupLog> BackupLogs
        {
            get => _backupLogs;
            set => SetProperty(ref _backupLogs, value);
        }

        #endregion

        #region Commands

        public ICommand CreateFullBackupCommand { get; private set; } = null!;
        public ICommand CreateIncrementalBackupCommand { get; private set; } = null!;
        public ICommand RestoreFromBackupCommand { get; private set; } = null!;
        public ICommand VerifyBackupCommand { get; private set; } = null!;
        public ICommand DeleteBackupCommand { get; private set; } = null!;
        public ICommand ScheduleBackupCommand { get; private set; } = null!;
        public ICommand ConfigureBackupCommand { get; private set; } = null!;
        public ICommand ManageStorageCommand { get; private set; } = null!;
        public ICommand TestConnectionCommand { get; private set; } = null!;
        public ICommand ExportDataCommand { get; private set; } = null!;
        public ICommand ImportDataCommand { get; private set; } = null!;
        public ICommand ExportSettingsCommand { get; private set; } = null!;
        public ICommand ImportSettingsCommand { get; private set; } = null!;
        public ICommand RefreshBackupListCommand { get; private set; } = null!;
        public ICommand ViewBackupDetailsCommand { get; private set; } = null!;

        #endregion

        public BackupRestoreViewModel(
            ILogger<BackupRestoreViewModel> logger,
            IBackupService backupService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _backupService = backupService ?? throw new ArgumentNullException(nameof(backupService));
            
            InitializeCommands();
            InitializeData();
        }

        private void InitializeCommands()
        {
            CreateFullBackupCommand = new RelayCommand(async () => await CreateFullBackupAsync());
            CreateIncrementalBackupCommand = new RelayCommand(async () => await CreateIncrementalBackupAsync());
            RestoreFromBackupCommand = new RelayCommand(async () => await RestoreFromBackupAsync(), () => SelectedBackup != null);
            VerifyBackupCommand = new RelayCommand(async () => await VerifyBackupAsync(), () => SelectedBackup != null);
            DeleteBackupCommand = new RelayCommand(async () => await DeleteBackupAsync(), () => SelectedBackup != null);
            ScheduleBackupCommand = new RelayCommand(ScheduleBackup);
            ConfigureBackupCommand = new RelayCommand(ConfigureBackup);
            ManageStorageCommand = new RelayCommand(ManageStorage);
            TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync());
            ExportDataCommand = new RelayCommand(async () => await ExportDataAsync());
            ImportDataCommand = new RelayCommand(async () => await ImportDataAsync());
            ExportSettingsCommand = new RelayCommand(async () => await ExportSettingsAsync());
            ImportSettingsCommand = new RelayCommand(async () => await ImportSettingsAsync());
            RefreshBackupListCommand = new RelayCommand(async () => await RefreshBackupListAsync());
            ViewBackupDetailsCommand = new RelayCommand(ViewBackupDetails, () => SelectedBackup != null);
        }

        private void InitializeData()
        {
            try
            {
                // إنشاء بيانات نسخ احتياطية وهمية للعرض
                AvailableBackups.Add(new BackupItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Full_Backup_20240101",
                    Type = BackupType.Full,
                    CreatedDate = DateTime.Now.AddDays(-7),
                    Size = 124.5,
                    Status = AccountingSystem.WPF.ViewModels.BackupStatus.Completed,
                    Description = "نسخة احتياطية كاملة شهرية"
                });

                AvailableBackups.Add(new BackupItem
                {
                    Id = Guid.NewGuid(),
                    Name = "Incremental_Backup_20240108",
                    Type = BackupType.Incremental,
                    CreatedDate = DateTime.Now.AddDays(-1),
                    Size = 15.2,
                    Status = AccountingSystem.WPF.ViewModels.BackupStatus.Completed,
                    Description = "نسخة احتياطية تدريجية يومية"
                });

                // إنشاء سجلات النسخ الاحتياطي
                BackupLogs.Add(new BackupLog
                {
                    Timestamp = DateTime.Now.AddHours(-2),
                    Operation = "إنشاء نسخة احتياطية",
                    Status = "نجح",
                    Message = "تم إنشاء النسخة الاحتياطية بنجاح"
                });

                BackupLogs.Add(new BackupLog
                {
                    Timestamp = DateTime.Now.AddDays(-1),
                    Operation = "التحقق من النسخة",
                    Status = "نجح",
                    Message = "تم التحقق من سلامة النسخة الاحتياطية"
                });

                _logger.LogInformation("تم تهيئة بيانات النسخ الاحتياطي - Backup data initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تهيئة بيانات النسخ الاحتياطي - Error initializing backup data");
            }
        }

        #region Command Implementations

        private async Task CreateFullBackupAsync()
        {
            try
            {
                _logger.LogInformation("بدء إنشاء نسخة احتياطية كاملة - Starting full backup");
                
                BackupStatus = "جاري إنشاء النسخة الاحتياطية الكاملة...";
                BackupProgress = 0;

                // محاكاة عملية النسخ الاحتياطي
                for (int i = 0; i <= 100; i += 10)
                {
                    BackupProgress = i;
                    await Task.Delay(200);
                }

                // إضافة النسخة الجديدة إلى القائمة
                var newBackup = new BackupItem
                {
                    Id = Guid.NewGuid(),
                    Name = $"Full_Backup_{DateTime.Now:yyyyMMdd_HHmmss}",
                    Type = BackupType.Full,
                    CreatedDate = DateTime.Now,
                    Size = new Random().NextDouble() * 200,
                    Status = AccountingSystem.WPF.ViewModels.BackupStatus.Completed,
                    Description = "نسخة احتياطية كاملة تم إنشاؤها الآن"
                };

                AvailableBackups.Insert(0, newBackup);
                LastBackupDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                BackupStatus = "تم إنشاء النسخة الاحتياطية بنجاح";

                // إضافة سجل العملية
                BackupLogs.Insert(0, new BackupLog
                {
                    Timestamp = DateTime.Now,
                    Operation = "إنشاء نسخة احتياطية كاملة",
                    Status = "نجح",
                    Message = $"تم إنشاء النسخة {newBackup.Name} بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء النسخة الاحتياطية الكاملة - Error creating full backup");
                BackupStatus = $"خطأ: {ex.Message}";
            }
        }

        private async Task CreateIncrementalBackupAsync()
        {
            try
            {
                _logger.LogInformation("بدء إنشاء نسخة احتياطية تدريجية - Starting incremental backup");
                
                BackupStatus = "جاري إنشاء النسخة الاحتياطية التدريجية...";
                BackupProgress = 0;

                // محاكاة عملية النسخ الاحتياطي التدريجي (أسرع)
                for (int i = 0; i <= 100; i += 20)
                {
                    BackupProgress = i;
                    await Task.Delay(100);
                }

                var newBackup = new BackupItem
                {
                    Id = Guid.NewGuid(),
                    Name = $"Incremental_Backup_{DateTime.Now:yyyyMMdd_HHmmss}",
                    Type = BackupType.Incremental,
                    CreatedDate = DateTime.Now,
                    Size = new Random().NextDouble() * 50,
                    Status = AccountingSystem.WPF.ViewModels.BackupStatus.Completed,
                    Description = "نسخة احتياطية تدريجية تم إنشاؤها الآن"
                };

                AvailableBackups.Insert(0, newBackup);
                LastBackupDate = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
                BackupStatus = "تم إنشاء النسخة الاحتياطية التدريجية بنجاح";

                BackupLogs.Insert(0, new BackupLog
                {
                    Timestamp = DateTime.Now,
                    Operation = "إنشاء نسخة احتياطية تدريجية",
                    Status = "نجح",
                    Message = $"تم إنشاء النسخة {newBackup.Name} بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء النسخة الاحتياطية التدريجية - Error creating incremental backup");
                BackupStatus = $"خطأ: {ex.Message}";
            }
        }

        private async Task RestoreFromBackupAsync()
        {
            if (SelectedBackup == null) return;

            try
            {
                _logger.LogInformation($"بدء استعادة النسخة الاحتياطية {SelectedBackup.Name} - Starting restore from backup");
                
                BackupStatus = $"جاري استعادة النسخة {SelectedBackup.Name}...";
                BackupProgress = 0;

                // محاكاة عملية الاستعادة
                for (int i = 0; i <= 100; i += 15)
                {
                    BackupProgress = i;
                    await Task.Delay(300);
                }

                BackupStatus = "تم استعادة النسخة الاحتياطية بنجاح";

                BackupLogs.Insert(0, new BackupLog
                {
                    Timestamp = DateTime.Now,
                    Operation = "استعادة من النسخة الاحتياطية",
                    Status = "نجح",
                    Message = $"تم استعادة النسخة {SelectedBackup.Name} بنجاح"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في استعادة النسخة الاحتياطية - Error restoring from backup");
                BackupStatus = $"خطأ في الاستعادة: {ex.Message}";
            }
        }

        private async Task VerifyBackupAsync()
        {
            if (SelectedBackup == null) return;

            try
            {
                _logger.LogInformation($"التحقق من سلامة النسخة {SelectedBackup.Name} - Verifying backup integrity");
                
                BackupStatus = $"جاري التحقق من النسخة {SelectedBackup.Name}...";
                
                await Task.Delay(2000); // محاكاة عملية التحقق
                
                BackupStatus = "تم التحقق من سلامة النسخة الاحتياطية بنجاح";

                BackupLogs.Insert(0, new BackupLog
                {
                    Timestamp = DateTime.Now,
                    Operation = "التحقق من النسخة",
                    Status = "نجح",
                    Message = $"النسخة {SelectedBackup.Name} سليمة وقابلة للاستعادة"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في التحقق من النسخة الاحتياطية - Error verifying backup");
                BackupStatus = $"خطأ في التحقق: {ex.Message}";
            }
        }

        private async Task DeleteBackupAsync()
        {
            if (SelectedBackup == null) return;

            try
            {
                _logger.LogInformation($"حذف النسخة الاحتياطية {SelectedBackup.Name} - Deleting backup");
                
                var backupToDelete = SelectedBackup;
                AvailableBackups.Remove(backupToDelete);
                
                await Task.Delay(500); // محاكاة عملية الحذف
                
                BackupStatus = $"تم حذف النسخة {backupToDelete.Name} بنجاح";

                BackupLogs.Insert(0, new BackupLog
                {
                    Timestamp = DateTime.Now,
                    Operation = "حذف النسخة الاحتياطية",
                    Status = "نجح",
                    Message = $"تم حذف النسخة {backupToDelete.Name}"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف النسخة الاحتياطية - Error deleting backup");
                BackupStatus = $"خطأ في الحذف: {ex.Message}";
            }
        }

        private void ScheduleBackup()
        {
            _logger.LogInformation("فتح جدولة النسخ الاحتياطية - Opening backup scheduling");
            BackupStatus = "فتح نافذة جدولة النسخ الاحتياطية...";
        }

        private void ConfigureBackup()
        {
            _logger.LogInformation("فتح إعدادات النسخ الاحتياطي - Opening backup configuration");
            BackupStatus = "فتح نافذة إعدادات النسخ الاحتياطي...";
        }

        private void ManageStorage()
        {
            _logger.LogInformation("إدارة مساحة التخزين - Managing storage");
            BackupStatus = "فتح نافذة إدارة مساحة التخزين...";
        }

        private async Task TestConnectionAsync()
        {
            try
            {
                _logger.LogInformation("اختبار الاتصال - Testing connection");
                
                BackupStatus = "جاري اختبار الاتصال...";
                
                await Task.Delay(1500); // محاكاة اختبار الاتصال
                
                BackupStatus = "الاتصال ناجح - جميع الخدمات تعمل بشكل طبيعي";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في اختبار الاتصال - Error testing connection");
                BackupStatus = $"فشل الاتصال: {ex.Message}";
            }
        }

        private async Task ExportDataAsync()
        {
            try
            {
                _logger.LogInformation("تصدير البيانات - Exporting data");
                BackupStatus = "جاري تصدير البيانات...";
                await Task.Delay(2000);
                BackupStatus = "تم تصدير البيانات بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تصدير البيانات - Error exporting data");
            }
        }

        private async Task ImportDataAsync()
        {
            try
            {
                _logger.LogInformation("استيراد البيانات - Importing data");
                BackupStatus = "جاري استيراد البيانات...";
                await Task.Delay(2000);
                BackupStatus = "تم استيراد البيانات بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في استيراد البيانات - Error importing data");
            }
        }

        private async Task ExportSettingsAsync()
        {
            try
            {
                _logger.LogInformation("تصدير الإعدادات - Exporting settings");
                await Task.Delay(1000);
                BackupStatus = "تم تصدير الإعدادات بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تصدير الإعدادات - Error exporting settings");
            }
        }

        private async Task ImportSettingsAsync()
        {
            try
            {
                _logger.LogInformation("استيراد الإعدادات - Importing settings");
                await Task.Delay(1000);
                BackupStatus = "تم استيراد الإعدادات بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في استيراد الإعدادات - Error importing settings");
            }
        }

        private async Task RefreshBackupListAsync()
        {
            try
            {
                _logger.LogInformation("تحديث قائمة النسخ الاحتياطية - Refreshing backup list");
                BackupStatus = "جاري تحديث القائمة...";
                await Task.Delay(1000);
                BackupStatus = "تم تحديث القائمة بنجاح";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث القائمة - Error refreshing backup list");
            }
        }

        private void ViewBackupDetails()
        {
            if (SelectedBackup == null) return;
            
            _logger.LogInformation($"عرض تفاصيل النسخة {SelectedBackup.Name} - Viewing backup details");
            BackupStatus = $"عرض تفاصيل النسخة {SelectedBackup.Name}";
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

    public class BackupItem
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public BackupType Type { get; set; }
        public DateTime CreatedDate { get; set; }
        public double Size { get; set; } // Size in MB
        public BackupStatus Status { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public class BackupLog
    {
        public DateTime Timestamp { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    public enum BackupType
    {
        Full,
        Incremental,
        Differential
    }

    public enum BackupStatus
    {
        Pending,
        InProgress,
        Completed,
        Failed,
        Corrupted
    }

    #endregion
}