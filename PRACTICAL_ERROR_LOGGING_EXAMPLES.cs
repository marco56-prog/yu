// مثال عملي: كيف النظام يتعامل مع جميع أنواع الأخطاء

// ========================================
// 1. خطأ النقر على زر (تلقائي)
// ========================================
private void SaveInvoice_Click(object sender, RoutedEventArgs e)
{
    // أي خطأ هنا سيتم التقاطه تلقائياً بواسطة GlobalExceptionHandler
    try 
    {
        // كود حفظ الفاتورة
        var invoice = CreateInvoice();
        SaveToDatabase(invoice);
    }
    catch (Exception ex)
    {
        // سيتم تسجيله تلقائياً + يمكن تسجيل تفاصيل إضافية
        var errorId = await _errorLoggingService.LogFinancialErrorAsync(ex, userId, username);
        MessageBox.Show($"فشل في حفظ الفاتورة. رقم الخطأ: {errorId}");
    }
}

// ========================================
// 2. خطأ في العمليات الحسابية (تلقائي)
// ========================================
public decimal CalculateTotal(List<InvoiceItem> items)
{
    try 
    {
        decimal total = 0;
        foreach (var item in items)
        {
            // إذا حدث خطأ في الحسابات (مثل القسمة على صفر)
            total += item.Quantity * item.Price - (item.Quantity * item.Price * item.DiscountRate / 100);
        }
        return total;
    }
    catch (Exception ex)
    {
        // سيتم تسجيله تلقائياً في GlobalExceptionHandler
        // + تسجيل إضافي مخصص للعمليات المالية
        await _errorLoggingService.LogFinancialErrorAsync(ex, userId, username);
        throw; // إعادة رمي الخطأ بعد التسجيل
    }
}

// ========================================
// 3. خطأ في حفظ البيانات (تلقائي)
// ========================================
public async Task<bool> SaveCustomerAsync(Customer customer)
{
    try 
    {
        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();
        return true;
    }
    catch (DbUpdateException ex)
    {
        // خطأ قاعدة بيانات - سيتم تسجيله تلقائياً + تسجيل مخصص
        await _errorLoggingService.LogDatabaseErrorAsync(ex, userId, username);
        return false;
    }
    catch (Exception ex)
    {
        // أي خطأ آخر
        await _errorLoggingService.LogBusinessLogicErrorAsync(ex, userId, username);
        return false;
    }
}

// ========================================
// 4. خطأ في جلب البيانات (تلقائي)
// ========================================
public async Task<List<Product>> GetProductsAsync()
{
    try 
    {
        var products = await _context.Products.ToListAsync();
        return products;
    }
    catch (SqlException ex)
    {
        // خطأ SQL - سيتم تسجيله تلقائياً
        await _errorLoggingService.LogDatabaseErrorAsync(ex, userId, username);
        return new List<Product>(); // قائمة فارغة كحل احتياطي
    }
    catch (TimeoutException ex)
    {
        // انتهت مهلة الاستعلام
        await _errorLoggingService.LogNetworkErrorAsync(ex, userId, username);
        throw new ApplicationException("انتهت مهلة الاتصال بقاعدة البيانات");
    }
}

// ========================================
// 5. خطأ في التحقق من البيانات (يدوي)
// ========================================
public async Task<ValidationResult> ValidateInvoiceAsync(SalesInvoice invoice)
{
    var errors = new List<string>();
    
    if (invoice.CustomerId <= 0)
    {
        errors.Add("يجب اختيار عميل");
        await _errorLoggingService.LogValidationErrorAsync(
            "فاتورة بدون عميل محدد", userId, username);
    }
    
    if (!invoice.Items.Any())
    {
        errors.Add("يجب إضافة صنف واحد على الأقل");
        await _errorLoggingService.LogValidationErrorAsync(
            "فاتورة بدون أصناف", userId, username);
    }
    
    if (invoice.Items.Any(i => i.Quantity <= 0))
    {
        errors.Add("جميع الكميات يجب أن تكون أكبر من صفر");
        await _errorLoggingService.LogValidationErrorAsync(
            "كميات خاطئة في الفاتورة", userId, username);
    }
    
    return new ValidationResult { IsValid = !errors.Any(), Errors = errors };
}

// ========================================
// 6. ما يحدث تلقائياً في النظام:
// ========================================

// أ) جميع Unhandled Exceptions تُلتقط تلقائياً
// ب) تُسجل في 3 أماكن فوراً:
//    - ملف logs/errors-2025-09-23.log
//    - جدول ErrorLogs في قاعدة البيانات  
//    - Console (للمطورين)

// ج) تُصنف حسب النوع:
//    - UI Error (أخطاء واجهة المستخدم)
//    - Database Error (أخطاء قاعدة البيانات)
//    - Business Logic Error (أخطاء منطق الأعمال)
//    - وغيرها...

// د) تُحدد الخطورة:
//    - Info, Warning, Error, Critical, Fatal

// هـ) تُحفظ مع تفاصيل كاملة:
//    - Stack Trace
//    - اسم المستخدم
//    - وقت الحدوث  
//    - تفاصيل الجلسة
//    - رقم السطر والملف (إذا أمكن)

// ========================================
// 7. مثال على ما يُحفظ في log file:
// ========================================
/*
[2025-09-23 22:30:15.123 ERR] خطأ: ABC123 - فشل في حفظ الفاتورة {"ErrorId":"ABC123","ErrorType":"FinancialError","Severity":"Critical","UserId":1,"Username":"admin","Activity":"حفظ فاتورة"}
System.InvalidOperationException: لا يمكن حفظ فاتورة بدون أصناف
   at AccountingSystem.Business.SalesInvoiceService.SaveInvoiceAsync() line 125
   at AccountingSystem.WPF.Views.SalesInvoiceWindow.SaveButton_Click() line 89
*/

// ========================================
// 8. مثال على ما يُحفظ في قاعدة البيانات:
// ========================================
/*
ErrorLog Table:
Id: 1
ErrorId: "ABC123"
ErrorType: FinancialError  
Severity: Critical
Status: New
Title: "فشل في حفظ الفاتورة"
Message: "لا يمكن حفظ فاتورة بدون أصناف"
Details: "Full exception details..."
StackTrace: "Full stack trace..."
MethodName: "SaveInvoiceAsync" 
FileName: "SalesInvoiceService.cs"
LineNumber: 125
UserId: 1
Username: "admin"
Activity: "حفظ فاتورة"
CreatedAt: 2025-09-23 22:30:15
OccurrenceCount: 1
*/