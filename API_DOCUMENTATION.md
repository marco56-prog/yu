# 📚 API Documentation - خدمات البنية التحتية الجديدة

## نظرة عامة | Overview

هذا التوثيق يغطي الخدمات الجديدة المضافة في الإصدار 2.1.0 لتحسين معالجة الأخطاء، مرونة الاتصال، وصحة قاعدة البيانات.

This documentation covers the new services added in version 2.1.0 for improved error handling, connection resilience, and database health monitoring.

---

## 1. 🏥 DatabaseHealthService

### الوصف | Description
خدمة شاملة لفحص صحة قاعدة البيانات والتحقق من سلامتها.  
Comprehensive service for database health checking and integrity verification.

### التسجيل | Registration
```csharp
services.AddScoped<IDatabaseHealthService, DatabaseHealthService>();
```

### الاستخدام | Usage

#### فحص صحة قاعدة البيانات الشامل
```csharp
var healthService = serviceProvider.GetService<IDatabaseHealthService>();
var healthResult = await healthService.CheckDatabaseHealthAsync();

if (healthResult.IsHealthy)
{
    Console.WriteLine("✅ قاعدة البيانات بحالة جيدة");
}
else
{
    Console.WriteLine($"❌ مشاكل: {string.Join(", ", healthResult.Issues)}");
    Console.WriteLine($"⚠️ تحذيرات: {string.Join(", ", healthResult.Warnings)}");
}
```

#### اختبار الاتصال
```csharp
bool canConnect = await healthService.CanConnectAsync();
if (!canConnect)
{
    Console.WriteLine("فشل الاتصال بقاعدة البيانات");
}
```

#### التحقق من Migrations
```csharp
var migrationStatus = await healthService.CheckMigrationsAsync();

Console.WriteLine($"Applied Migrations: {migrationStatus.AppliedMigrations.Count}");
Console.WriteLine($"Pending Migrations: {migrationStatus.PendingMigrations.Count}");

if (!migrationStatus.IsUpToDate)
{
    Console.WriteLine("يوجد migrations معلقة:");
    foreach (var migration in migrationStatus.PendingMigrations)
    {
        Console.WriteLine($"  - {migration}");
    }
}
```

#### تطبيق Migrations المعلقة
```csharp
if (!migrationStatus.IsUpToDate)
{
    bool success = await healthService.ApplyPendingMigrationsAsync();
    if (success)
    {
        Console.WriteLine("✅ تم تطبيق Migrations بنجاح");
    }
}
```

#### الحصول على إحصائيات قاعدة البيانات
```csharp
var stats = await healthService.GetDatabaseStatisticsAsync();

Console.WriteLine($"العملاء: {stats.CustomersCount}");
Console.WriteLine($"الموردين: {stats.SuppliersCount}");
Console.WriteLine($"المنتجات: {stats.ProductsCount}");
Console.WriteLine($"فواتير البيع: {stats.SalesInvoicesCount}");
Console.WriteLine($"فواتير الشراء: {stats.PurchaseInvoicesCount}");
Console.WriteLine($"حجم قاعدة البيانات: {stats.DatabaseSize}");
```

### النماذج | Models

#### DatabaseHealthResult
```csharp
public class DatabaseHealthResult
{
    public bool IsHealthy { get; set; }
    public DateTime CheckedAt { get; set; }
    public bool CanConnect { get; set; }
    public bool DatabaseExists { get; set; }
    public bool TablesExist { get; set; }
    public bool HasSeedData { get; set; }
    public bool HasPendingMigrations { get; set; }
    public List<string> PendingMigrations { get; set; }
    public List<string> Issues { get; set; }
    public List<string> Warnings { get; set; }
    
    public string GetSummary();
}
```

---

## 2. 🔄 DatabaseConnectionResilienceService

### الوصف | Description
خدمة مرونة الاتصال بقاعدة البيانات مع إعادة محاولة تلقائية.  
Database connection resilience service with automatic retry logic.

### التسجيل | Registration
```csharp
services.AddScoped<IDatabaseConnectionResilienceService, DatabaseConnectionResilienceService>();
```

### الاستخدام | Usage

#### تنفيذ عملية مع إعادة محاولة تلقائية
```csharp
var resilienceService = serviceProvider.GetService<IDatabaseConnectionResilienceService>();

// مع قيمة إرجاع
var products = await resilienceService.ExecuteWithRetryAsync(async () =>
{
    return await dbContext.Products.ToListAsync();
}, maxRetries: 5, delayMs: 2000);

// بدون قيمة إرجاع
await resilienceService.ExecuteWithRetryAsync(async () =>
{
    await dbContext.SaveChangesAsync();
}, maxRetries: 3);
```

#### التأكد من صلاحية الاتصال
```csharp
bool isConnected = await resilienceService.EnsureConnectionAsync();
if (!isConnected)
{
    Console.WriteLine("فشل التأكد من الاتصال");
}
```

#### إعادة إنشاء الاتصال
```csharp
bool recreated = await resilienceService.RecreateConnectionAsync();
if (recreated)
{
    Console.WriteLine("تم إعادة إنشاء الاتصال بنجاح");
}
```

#### استخدام Extension Methods
```csharp
// تنفيذ استعلام مع قيمة افتراضية في حالة الفشل
var result = await resilienceService.ExecuteQueryWithResilienceAsync(
    async () => await dbContext.Products.FirstOrDefaultAsync(p => p.ProductId == 1),
    defaultValue: null,
    maxRetries: 3
);

// تنفيذ أمر مع معالجة boolean للنجاح/الفشل
bool success = await resilienceService.ExecuteCommandWithResilienceAsync(
    async () => await SaveDataAsync(),
    maxRetries: 3
);
```

### الأخطاء المؤقتة المدعومة
الخدمة تكتشف وتعالج الأخطاء المؤقتة التالية:
- Timeout errors (-1, -2)
- Deadlock (1205)
- Lock request timeout (1222)
- Database unavailable (4060, 40613)
- Service busy (40501, 40197)
- Network errors (SocketException)

---

## 3. ⚠️ GlobalExceptionHandler

### الوصف | Description
معالج عام للاستثناءات مع تصنيف تلقائي ورسائل واضحة للمستخدم.  
Global exception handler with automatic classification and user-friendly messages.

### التسجيل | Registration
```csharp
services.AddScoped<IGlobalExceptionHandler, GlobalExceptionHandler>();
```

### الاستخدام | Usage

#### معالجة استثناء محدد
```csharp
var exceptionHandler = serviceProvider.GetService<IGlobalExceptionHandler>();

try
{
    // عملية قد تفشل
    await SomeRiskyOperationAsync();
}
catch (Exception ex)
{
    var result = await exceptionHandler.HandleExceptionAsync(ex, "RiskyOperation");
    
    // عرض رسالة للمستخدم
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
    
    Console.WriteLine($"Error ID: {result.ErrorId}");
    Console.WriteLine($"Type: {result.ErrorType}");
    Console.WriteLine($"Severity: {result.Severity}");
    Console.WriteLine($"Recoverable: {result.IsRecoverable}");
}
```

#### تنفيذ مع معالجة تلقائية (Async with return value)
```csharp
var result = await exceptionHandler.ExecuteWithErrorHandlingAsync(async () =>
{
    return await _service.GetDataAsync();
}, context: "GetData");

// result will be null if exception occurred
if (result != null)
{
    // معالجة النتيجة
}
```

#### تنفيذ مع معالجة تلقائية (Async without return value)
```csharp
await exceptionHandler.ExecuteWithErrorHandlingAsync(async () =>
{
    await _service.SaveDataAsync();
}, context: "SaveData");
```

#### تنفيذ مع معالجة تلقائية (Sync)
```csharp
// مع قيمة إرجاع
var data = exceptionHandler.ExecuteWithErrorHandling(() =>
{
    return _service.GetData();
}, context: "GetData");

// بدون قيمة إرجاع
exceptionHandler.ExecuteWithErrorHandling(() =>
{
    _service.SaveData();
}, context: "SaveData");
```

#### استخدام Extension Methods
```csharp
// تنفيذ مع قيمة افتراضية
var data = await exceptionHandler.TryExecuteAsync(
    async () => await _service.GetDataAsync(),
    defaultValue: new List<Product>(),
    context: "GetProducts"
);

// تنفيذ مع إعادة محاولة
var result = await exceptionHandler.TryExecuteWithRetryAsync(
    async () => await _service.SaveDataAsync(),
    maxRetries: 3,
    delayMs: 1000,
    context: "SaveData"
);
```

### النماذج | Models

#### ErrorHandlingResult
```csharp
public class ErrorHandlingResult
{
    public string ErrorId { get; set; }
    public Exception? Exception { get; set; }
    public ErrorType ErrorType { get; set; }
    public ErrorSeverity Severity { get; set; }
    public string Context { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsRecoverable { get; set; }
    public string UserMessage { get; set; }
    public string SuggestedAction { get; set; }
}
```

### تصنيف الأخطاء
الخدمة تصنف الأخطاء تلقائياً:

| Exception Type | Error Type | Severity |
|---------------|-----------|----------|
| DbUpdateException | DatabaseError | Critical |
| SqlException | DatabaseError | Critical |
| ArgumentNullException | ValidationError | Warning |
| UnauthorizedAccessException | SecurityError | Critical |
| OutOfMemoryException | SystemError | Fatal |

---

## 4. ✅ BusinessLogicValidator

### الوصف | Description
خدمة التحقق من منطق الأعمال مع قواعد شاملة للتحقق.  
Business logic validation service with comprehensive validation rules.

### التسجيل | Registration
```csharp
services.AddScoped<IBusinessLogicValidator, BusinessLogicValidator>();
```

### الاستخدام | Usage

#### التحقق من فاتورة بيع
```csharp
var validator = serviceProvider.GetService<IBusinessLogicValidator>();
var invoice = new SalesInvoice { /* ... */ };

var result = await validator.ValidateSalesInvoiceAsync(invoice);

if (!result.IsValid)
{
    Console.WriteLine("أخطاء:");
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  - {error.PropertyName}: {error.Message}");
    }
}

if (result.Warnings.Any())
{
    Console.WriteLine("تحذيرات:");
    foreach (var warning in result.Warnings)
    {
        Console.WriteLine($"  - {warning.PropertyName}: {warning.Message}");
    }
}
```

#### التحقق من العميل
```csharp
var customer = new Customer { /* ... */ };
var result = await validator.ValidateCustomerAsync(customer);

if (result.IsValid)
{
    // حفظ العميل
    await _customerService.CreateAsync(customer);
}
else
{
    MessageBox.Show(result.GetErrorMessage(), "خطأ في البيانات");
}
```

#### التحقق من المنتج
```csharp
var product = new Product { /* ... */ };
var result = await validator.ValidateProductAsync(product);

if (!result.IsValid)
{
    MessageBox.Show(result.GetErrorMessage());
}
else if (result.Warnings.Any())
{
    var response = MessageBox.Show(
        result.GetWarningMessage() + "\n\nهل تريد المتابعة؟",
        "تحذير",
        MessageBoxButton.YesNo
    );
    
    if (response == MessageBoxResult.Yes)
    {
        await _productService.CreateAsync(product);
    }
}
```

#### التحقق من عملية مخزون
```csharp
var result = await validator.ValidateStockTransactionAsync(
    productId: 123,
    quantity: 50,
    operation: "Sale"
);

if (result.IsValid)
{
    await ProcessStockTransaction();
}
```

### قواعد التحقق

#### فاتورة البيع
- ✅ تحديد العميل (CustomerId > 0)
- ✅ تاريخ صحيح (ليس في المستقبل)
- ✅ بند واحد على الأقل
- ✅ كمية وسعر صحيحين لكل بند
- ✅ نسبة خصم بين 0-100%
- ✅ حسابات البنود صحيحة
- ✅ إجماليات غير سالبة
- ⚠️ تحذير: صافي سالب
- ⚠️ تحذير: مدفوع أكبر من الصافي

#### العميل/المورد
- ✅ الاسم مطلوب وصحيح
- ✅ الهاتف صحيح (إذا موجود)
- ✅ البريد الإلكتروني صحيح (إذا موجود)
- ✅ حد الائتمان غير سالب
- ⚠️ تحذير: رصيد سالب

#### المنتج
- ✅ الاسم والكود مطلوبان
- ✅ أسعار غير سالبة
- ✅ مخزون غير سالب
- ⚠️ تحذير: سعر بيع أقل من شراء
- ⚠️ تحذير: مخزون أقل من الحد الأدنى

---

## 📊 أمثلة متقدمة | Advanced Examples

### مثال 1: فحص النظام الشامل عند البدء
```csharp
public async Task<bool> PerformSystemHealthCheckAsync()
{
    var healthService = App.ServiceProvider.GetService<IDatabaseHealthService>();
    var exceptionHandler = App.ServiceProvider.GetService<IGlobalExceptionHandler>();
    
    try
    {
        // فحص قاعدة البيانات
        var health = await healthService.CheckDatabaseHealthAsync();
        
        if (!health.IsHealthy)
        {
            var message = $"مشاكل في قاعدة البيانات:\n{string.Join("\n", health.Issues)}";
            MessageBox.Show(message, "فشل فحص النظام", MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
        
        // تطبيق Migrations إذا لزم الأمر
        if (health.HasPendingMigrations)
        {
            var result = MessageBox.Show(
                "يوجد تحديثات لقاعدة البيانات. هل تريد تطبيقها؟",
                "تحديثات قاعدة البيانات",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );
            
            if (result == MessageBoxResult.Yes)
            {
                await healthService.ApplyPendingMigrationsAsync();
            }
        }
        
        return true;
    }
    catch (Exception ex)
    {
        await exceptionHandler.HandleExceptionAsync(ex, "SystemHealthCheck");
        return false;
    }
}
```

### مثال 2: حفظ فاتورة مع تحقق شامل
```csharp
public async Task<bool> SaveInvoiceWithValidationAsync(SalesInvoice invoice)
{
    var validator = App.ServiceProvider.GetService<IBusinessLogicValidator>();
    var exceptionHandler = App.ServiceProvider.GetService<IGlobalExceptionHandler>();
    var resilienceService = App.ServiceProvider.GetService<IDatabaseConnectionResilienceService>();
    
    // 1. التحقق من منطق الأعمال
    var validationResult = await validator.ValidateSalesInvoiceAsync(invoice);
    
    if (!validationResult.IsValid)
    {
        MessageBox.Show(
            validationResult.GetErrorMessage(),
            "خطأ في البيانات",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
        return false;
    }
    
    // 2. عرض التحذيرات إذا وجدت
    if (validationResult.Warnings.Any())
    {
        var response = MessageBox.Show(
            validationResult.GetWarningMessage() + "\n\nهل تريد المتابعة؟",
            "تحذير",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning
        );
        
        if (response == MessageBoxResult.No)
            return false;
    }
    
    // 3. حفظ مع معالجة الأخطاء ومرونة الاتصال
    return await exceptionHandler.ExecuteWithErrorHandlingAsync(async () =>
    {
        await resilienceService.ExecuteWithRetryAsync(async () =>
        {
            await _invoiceService.CreateSalesInvoiceAsync(invoice);
        }, maxRetries: 3, delayMs: 1000);
        
        MessageBox.Show("تم حفظ الفاتورة بنجاح", "نجح", MessageBoxButton.OK, MessageBoxImage.Information);
        return true;
    }, context: "SaveInvoice") ?? false;
}
```

### مثال 3: مراقبة الأداء مع السجلات
```csharp
public async Task<List<Product>> GetProductsWithMonitoringAsync()
{
    var logger = App.ServiceProvider.GetService<ILogger<ProductService>>();
    var resilienceService = App.ServiceProvider.GetService<IDatabaseConnectionResilienceService>();
    
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    
    try
    {
        var products = await resilienceService.ExecuteWithRetryAsync(async () =>
        {
            return await _dbContext.Products
                .AsNoTracking()
                .Where(p => p.IsActive)
                .ToListAsync();
        }, maxRetries: 3);
        
        stopwatch.Stop();
        logger.LogInformation(
            "تم تحميل {Count} منتج في {ElapsedMs}ms",
            products.Count,
            stopwatch.ElapsedMilliseconds
        );
        
        if (stopwatch.ElapsedMilliseconds > 1000)
        {
            logger.LogWarning("عملية بطيئة: {ElapsedMs}ms", stopwatch.ElapsedMilliseconds);
        }
        
        return products;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "فشل تحميل المنتجات");
        return new List<Product>();
    }
}
```

---

## 🔍 استكشاف الأخطاء | Troubleshooting

راجع ملف [TROUBLESHOOTING.md](TROUBLESHOOTING.md) للحصول على دليل شامل لاستكشاف الأخطاء وإصلاحها.

See [TROUBLESHOOTING.md](TROUBLESHOOTING.md) for a comprehensive troubleshooting guide.

---

**تاريخ التحديث**: 2025-01-08  
**النسخة**: 2.1.0  
**الحالة**: ✅ موثق بالكامل
