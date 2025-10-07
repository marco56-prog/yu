# 🔧 دليل استكشاف الأخطاء وإصلاحها
# Troubleshooting Guide - النظام المحاسبي الشامل

## 📋 جدول المحتويات

1. [مشاكل قاعدة البيانات](#database-issues)
2. [مشاكل الاتصال](#connection-issues)
3. [مشاكل Migrations](#migration-issues)
4. [مشاكل معالجة الأخطاء](#error-handling-issues)
5. [مشاكل الأداء](#performance-issues)
6. [مشاكل UI/XAML](#ui-issues)
7. [مشاكل الأمان](#security-issues)
8. [الأدوات والأوامر المفيدة](#useful-tools)

---

## <a name="database-issues"></a>1. 🗃️ مشاكل قاعدة البيانات

### المشكلة: قاعدة البيانات غير موجودة
**الأعراض:**
```
Cannot open database "AccountingSystemDB" requested by the login
```

**الحل:**
```bash
# 1. تطبيق Migrations
dotnet ef database update --project AccountingSystem.Data --startup-project AccountingSystem.WPF

# 2. إنشاء قاعدة البيانات من الصفر
dotnet ef database drop --project AccountingSystem.Data --startup-project AccountingSystem.WPF --force
dotnet ef database update --project AccountingSystem.Data --startup-project AccountingSystem.WPF
```

**الحل البديل:**
- تأكد من تشغيل SQL Server LocalDB
- افتح SQL Server Management Studio
- أنشئ قاعدة بيانات جديدة باسم `AccountingSystemDB`

### المشكلة: Foreign Key Constraints
**الأعراض:**
```
The ALTER TABLE statement conflicted with the FOREIGN KEY constraint
```

**الحل:**
```sql
-- 1. التحقق من Foreign Keys المعطلة
SELECT 
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName
FROM sys.foreign_keys fk
WHERE fk.is_disabled = 1;

-- 2. تفعيل Foreign Keys
ALTER TABLE [TableName] CHECK CONSTRAINT [ConstraintName];

-- 3. إعادة بناء الفهارس
ALTER INDEX ALL ON [TableName] REBUILD;
```

### المشكلة: بطء الاستعلامات
**الأعراض:**
- العمليات تأخذ وقتاً طويلاً
- Timeout exceptions

**الحل:**
```sql
-- 1. التحقق من الفهارس المفقودة
SELECT 
    OBJECT_NAME(s.object_id) AS TableName,
    i.name AS IndexName,
    s.avg_fragmentation_in_percent
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') s
INNER JOIN sys.indexes i ON s.object_id = i.object_id AND s.index_id = i.index_id
WHERE s.avg_fragmentation_in_percent > 30
ORDER BY s.avg_fragmentation_in_percent DESC;

-- 2. إعادة بناء الفهارس المجزأة
ALTER INDEX ALL ON [TableName] REBUILD WITH (ONLINE = OFF);

-- 3. تحديث الإحصائيات
UPDATE STATISTICS [TableName];
```

---

## <a name="connection-issues"></a>2. 🔌 مشاكل الاتصال

### المشكلة: فشل الاتصال بقاعدة البيانات
**الأعراض:**
```
A network-related or instance-specific error occurred while establishing a connection to SQL Server
```

**التشخيص:**
```csharp
// استخدم خدمة فحص الصحة
var healthService = serviceProvider.GetService<IDatabaseHealthService>();
var health = await healthService.CheckDatabaseHealthAsync();

Console.WriteLine($"Can Connect: {health.CanConnect}");
Console.WriteLine($"Database Exists: {health.DatabaseExists}");
Console.WriteLine($"Issues: {string.Join(", ", health.Issues)}");
```

**الحل:**
```bash
# 1. التحقق من تشغيل SQL Server LocalDB
sqllocaldb info
sqllocaldb start MSSQLLocalDB

# 2. اختبار Connection String
sqlcmd -S "(localdb)\MSSQLLocalDB" -Q "SELECT @@VERSION"

# 3. التحقق من الصلاحيات
# تأكد أن المستخدم الحالي لديه صلاحيات على قاعدة البيانات
```

**تعديل Connection String:**
```json
// في appsettings.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=AccountingSystemDB;Trusted_Connection=true;MultipleActiveResultSets=true;Connection Timeout=30;TrustServerCertificate=true"
  }
}
```

### المشكلة: Timeout خلال العمليات
**الأعراض:**
```
Execution Timeout Expired. The timeout period elapsed prior to completion of the operation
```

**الحل:**
```csharp
// استخدم خدمة المرونة
var resilienceService = serviceProvider.GetService<IDatabaseConnectionResilienceService>();

// تنفيذ مع retry تلقائي
var result = await resilienceService.ExecuteWithRetryAsync(async () =>
{
    return await dbContext.Products.ToListAsync();
}, maxRetries: 5, delayMs: 2000);
```

**تعديل الإعدادات:**
```json
{
  "Database": {
    "CommandTimeout": 120,  // زيادة المهلة
    "MaxRetryCount": 5,
    "MaxRetryDelay": 10
  }
}
```

---

## <a name="migration-issues"></a>3. 📦 مشاكل Migrations

### المشكلة: Migration معلق
**التشخيص:**
```bash
# عرض حالة Migrations
dotnet ef migrations list --project AccountingSystem.Data --startup-project AccountingSystem.WPF
```

**الحل:**
```csharp
// استخدم DatabaseHealthService
var healthService = serviceProvider.GetService<IDatabaseHealthService>();
var migrationStatus = await healthService.CheckMigrationsAsync();

if (migrationStatus.PendingMigrations.Any())
{
    Console.WriteLine($"Pending: {string.Join(", ", migrationStatus.PendingMigrations)}");
    
    // تطبيق Migrations
    await healthService.ApplyPendingMigrationsAsync();
}
```

**الحل اليدوي:**
```bash
# تطبيق جميع Migrations
dotnet ef database update --project AccountingSystem.Data --startup-project AccountingSystem.WPF

# الرجوع إلى migration معين
dotnet ef database update [MigrationName] --project AccountingSystem.Data --startup-project AccountingSystem.WPF

# حذف آخر migration (قبل التطبيق فقط)
dotnet ef migrations remove --project AccountingSystem.Data --startup-project AccountingSystem.WPF
```

### المشكلة: Migration فاشل
**الأعراض:**
```
Could not apply migration 'XXXX_MigrationName'
```

**الحل:**
```bash
# 1. التحقق من السكريبت
dotnet ef migrations script --project AccountingSystem.Data --startup-project AccountingSystem.WPF

# 2. إنشاء migration جديد لإصلاح المشكلة
dotnet ef migrations add FixMigrationIssue --project AccountingSystem.Data --startup-project AccountingSystem.WPF

# 3. إذا لم ينجح، العودة لنقطة سابقة
dotnet ef database update [PreviousMigrationName] --project AccountingSystem.Data --startup-project AccountingSystem.WPF
```

---

## <a name="error-handling-issues"></a>4. ⚠️ مشاكل معالجة الأخطاء

### استخدام GlobalExceptionHandler

**مثال في الكود:**
```csharp
var exceptionHandler = serviceProvider.GetService<IGlobalExceptionHandler>();

// تنفيذ مع معالجة الأخطاء
var result = await exceptionHandler.ExecuteWithErrorHandlingAsync(async () =>
{
    return await someService.DoSomethingAsync();
}, context: "DoSomething Operation");

// أو باستخدام Try-Execute
var result = await exceptionHandler.TryExecuteAsync(
    async () => await someService.DoSomethingAsync(),
    defaultValue: null,
    context: "DoSomething Operation"
);
```

### عرض الأخطاء للمستخدم

**مثال في ViewModel:**
```csharp
try
{
    await _service.SaveDataAsync();
}
catch (Exception ex)
{
    var errorHandler = App.ServiceProvider.GetService<IGlobalExceptionHandler>();
    var result = await errorHandler.HandleExceptionAsync(ex, "Save Data");
    
    // عرض الرسالة للمستخدم
    MessageBox.Show(
        result.UserMessage,
        "خطأ",
        MessageBoxButton.OK,
        MessageBoxImage.Error
    );
    
    // عرض الحل المقترح
    if (!string.IsNullOrEmpty(result.SuggestedAction))
    {
        MessageBox.Show(
            result.SuggestedAction,
            "الحل المقترح",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }
}
```

### فحص سجلات الأخطاء

**استعلامات مفيدة:**
```csharp
var errorLoggingService = serviceProvider.GetService<IErrorLoggingService>();

// الأخطاء الحرجة غير المحلولة
var criticalErrors = await errorLoggingService.GetCriticalErrorsAsync();

// إحصائيات الأخطاء
var stats = await errorLoggingService.GetErrorStatisticsAsync(
    fromDate: DateTime.Now.AddDays(-7),
    toDate: DateTime.Now
);

Console.WriteLine($"Total Errors: {stats.TotalErrors}");
Console.WriteLine($"Critical Errors: {stats.CriticalErrors}");
Console.WriteLine($"Resolution Rate: {stats.ResolutionRate:F1}%");

// البحث عن أخطاء معينة
var searchResult = await errorLoggingService.SearchErrorsAsync(new ErrorSearchRequest
{
    ErrorType = ErrorType.DatabaseError,
    Severity = ErrorSeverity.Critical,
    Status = ErrorStatus.New,
    FromDate = DateTime.Now.AddDays(-1)
});
```

---

## <a name="performance-issues"></a>5. ⚡ مشاكل الأداء

### تشخيص الأداء

**تفعيل Logging للاستعلامات:**
```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore.Database.Command": "Information"
    }
  }
}
```

**مراقبة الأداء:**
```csharp
var stopwatch = System.Diagnostics.Stopwatch.StartNew();

// العملية
var result = await _service.GetDataAsync();

stopwatch.Stop();
_logger.LogInformation("Operation took {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);

if (stopwatch.ElapsedMilliseconds > 1000)
{
    _logger.LogWarning("Slow operation detected: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
}
```

### تحسينات الأداء

**1. استخدام AsNoTracking:**
```csharp
// للقراءة فقط
var products = await _dbContext.Products
    .AsNoTracking()
    .Where(p => p.IsActive)
    .ToListAsync();
```

**2. Select فقط الحقول المطلوبة:**
```csharp
// بدلاً من تحميل الكيان كاملاً
var productNames = await _dbContext.Products
    .Select(p => new { p.ProductId, p.ProductName })
    .ToListAsync();
```

**3. Pagination:**
```csharp
var pageSize = 50;
var pageNumber = 1;

var products = await _dbContext.Products
    .Skip((pageNumber - 1) * pageSize)
    .Take(pageSize)
    .ToListAsync();
```

**4. Include للعلاقات:**
```csharp
// تحميل العلاقات بكفاءة
var invoices = await _dbContext.SalesInvoices
    .Include(i => i.Customer)
    .Include(i => i.Items)
        .ThenInclude(item => item.Product)
    .ToListAsync();
```

---

## <a name="ui-issues"></a>6. 🖥️ مشاكل UI/XAML

### Binding Errors

**التشخيص:**
- افتح Output window في Visual Studio
- ابحث عن `System.Windows.Data Error`

**الحلول الشائعة:**
```xml
<!-- 1. التحقق من اسم الخاصية -->
<TextBlock Text="{Binding CustomerName}" />

<!-- 2. استخدام FallbackValue -->
<TextBlock Text="{Binding CustomerName, FallbackValue='غير متوفر'}" />

<!-- 3. استخدام TargetNullValue -->
<TextBlock Text="{Binding CustomerName, TargetNullValue='لا يوجد'}" />

<!-- 4. Mode صحيح -->
<TextBox Text="{Binding CustomerName, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
```

### Null Reference في UI

**الحل:**
```csharp
// في ViewModel
private string? _customerName;
public string? CustomerName
{
    get => _customerName;
    set
    {
        if (_customerName != value)
        {
            _customerName = value;
            OnPropertyChanged(nameof(CustomerName));
        }
    }
}

// مع Null-conditional operator
public string DisplayName => CustomerName ?? "غير معروف";
```

---

## <a name="security-issues"></a>7. 🔐 مشاكل الأمان

### Login Issues

**التحقق من بيانات المستخدم:**
```sql
-- عرض المستخدمين في قاعدة البيانات
SELECT UserId, UserName, Email, IsActive, Role 
FROM Users;

-- إعادة تعيين كلمة مرور (Development فقط)
UPDATE Users 
SET PasswordHash = [NewHash], 
    LastPasswordChangeDate = GETDATE()
WHERE UserId = 1;
```

**تخطي Login في Development:**
```json
{
  "App": {
    "SkipLogin": true
  }
}
```

---

## <a name="useful-tools"></a>8. 🛠️ الأدوات والأوامر المفيدة

### EF Core Commands

```bash
# عرض قائمة Migrations
dotnet ef migrations list --project AccountingSystem.Data

# إنشاء Migration جديد
dotnet ef migrations add MigrationName --project AccountingSystem.Data

# تطبيق Migrations
dotnet ef database update --project AccountingSystem.Data

# إنشاء SQL Script
dotnet ef migrations script --project AccountingSystem.Data --output migration.sql

# حذف قاعدة البيانات
dotnet ef database drop --project AccountingSystem.Data --force
```

### SQL Server LocalDB Commands

```bash
# عرض instances
sqllocaldb info

# بدء instance
sqllocaldb start MSSQLLocalDB

# إيقاف instance
sqllocaldb stop MSSQLLocalDB

# حذف instance
sqllocaldb delete MSSQLLocalDB

# إنشاء instance جديد
sqllocaldb create MSSQLLocalDB
```

### Logging Commands

```bash
# عرض Logs
tail -f logs/application-*.log

# عرض Errors فقط
tail -f logs/errors-*.log

# البحث في Logs
grep "ERROR" logs/application-*.log

# عد الأخطاء
grep -c "ERROR" logs/application-*.log
```

### Database Statistics Queries

```sql
-- حجم قاعدة البيانات
SELECT 
    DB_NAME() AS DatabaseName,
    SUM(size * 8 / 1024) AS SizeMB
FROM sys.database_files;

-- عدد السجلات في كل جدول
SELECT 
    OBJECT_NAME(object_id) AS TableName,
    SUM(row_count) AS RowCount
FROM sys.dm_db_partition_stats
WHERE index_id IN (0, 1)
GROUP BY object_id
ORDER BY RowCount DESC;

-- الجداول الأكبر حجماً
SELECT 
    t.NAME AS TableName,
    SUM(p.rows) AS RowCounts,
    SUM(a.total_pages) * 8 AS TotalSpaceKB
FROM sys.tables t
INNER JOIN sys.indexes i ON t.OBJECT_ID = i.object_id
INNER JOIN sys.partitions p ON i.object_id = p.OBJECT_ID AND i.index_id = p.index_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
GROUP BY t.Name
ORDER BY TotalSpaceKB DESC;
```

---

## 📞 الحصول على المساعدة

إذا لم تجد حلاً لمشكلتك:

1. **تحقق من Logs:**
   - `logs/application-*.log` - سجلات التطبيق
   - `logs/errors-*.log` - سجلات الأخطاء فقط
   - `logs/startup.log` - سجلات بدء التشغيل

2. **استخدم Database Health Check:**
   ```csharp
   var health = await healthService.CheckDatabaseHealthAsync();
   Console.WriteLine(health.GetSummary());
   ```

3. **فحص Error Logs:**
   ```csharp
   var recentErrors = await errorLoggingService.GetRecentErrorsAsync(20);
   foreach (var error in recentErrors)
   {
       Console.WriteLine($"{error.ErrorId}: {error.Message}");
   }
   ```

4. **اتصل بالدعم الفني** مع:
   - نسخة من Logs
   - خطوات إعادة إنتاج المشكلة
   - Error ID من نظام تسجيل الأخطاء

---

**تاريخ آخر تحديث**: 2025-01-08  
**النسخة**: 2.1.0
