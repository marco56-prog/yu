# تعليمات الدخول للنظام المحاسبي

## المشكلة التي تم حلها ✅

كانت المشكلة أن المستخدم كان يحاول الدخول بكلمة المرور الخاطئة. 

## البيانات الصحيحة للدخول:

### المدير الرئيسي:
- **اسم المستخدم**: `admin`
- **كلمة المرور**: `Admin@123`

### المحاسب:
- **اسم المستخدم**: `accountant`
- **كلمة المرور**: `Account@123`

## ملاحظات مهمة:

1. كلمة المرور حساسة لحالة الأحرف (Case Sensitive)
2. يجب كتابة `Admin@123` بالضبط وليس `admin` أو `ADMIN@123`
3. نافذة تسجيل الدخول الآن تظهر البيانات الصحيحة تلقائياً
4. في حالة كتابة كلمة مرور خاطئة 5 مرات متتالية، سيتم قفل الحساب لمدة 15 دقيقة

## كيفية تشغيل البرنامج:

1. افتح نافذة PowerShell أو Command Prompt
2. انتقل إلى مجلد البرنامج:
   ```powershell
   cd c:\yu
   ```
3. شغل البرنامج:
   ```powershell
   dotnet run --project AccountingSystem.WPF
   ```

## إذا استمر الخطأ:

1. تأكد من وجود قاعدة البيانات بتشغيل:
   ```powershell
   dotnet ef database update --project AccountingSystem.Data
   ```

2. في حالة وجود مشاكل مع قاعدة البيانات، احذفها وأعد إنشاؤها:
   ```powershell
   dotnet ef database drop --project AccountingSystem.Data --force
   dotnet ef database update --project AccountingSystem.Data
   ```

تم حل المشكلة بنجاح! 🎉