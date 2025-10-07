# 💼 النظام المحاسبي الشامل | Complete Arabic Accounting System

<div align="center">

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet)
![C#](https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white)
![WPF](https://img.shields.io/badge/WPF-0078D4?style=for-the-badge&logo=windows&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL_Server-CC2927?style=for-the-badge&logo=microsoft-sql-server&logoColor=white)
![Arabic](https://img.shields.io/badge/Arabic_RTL-00A86B?style=for-the-badge&logo=language&logoColor=white)
![Version](https://img.shields.io/badge/Version-2.1.0-blue?style=for-the-badge)
![Status](https://img.shields.io/badge/Status-Production_Ready-success?style=for-the-badge)

**نظام محاسبة شامل وكامل الميزات مصمم خصيصاً للسوق العربي**  
**Complete, Feature-Rich Accounting System Designed for Arabic Markets**

[المميزات](#-features) • [التثبيت](#-installation) • [الاستخدام](#-usage) • [التوثيق](#-documentation) • [الدعم](#-support)

</div>

---

## 🎯 **نظرة عامة | Overview**

نظام محاسبي متكامل مبني على أحدث تقنيات Microsoft (.NET 8, WPF, Entity Framework Core) مع دعم كامل للغة العربية ونظام RTL، مصمم خصيصاً لتلبية احتياجات الشركات والمؤسسات في الدول العربية.

A comprehensive accounting system built with the latest Microsoft technologies (.NET 8, WPF, Entity Framework Core) with full Arabic language and RTL support, specifically designed to meet the needs of companies and institutions in Arabic countries.

### ✨ **ما الجديد في الإصدار 2.1.0**

- ✅ **Database Health Monitoring** - مراقبة صحة قاعدة البيانات
- ✅ **Connection Resilience** - مرونة الاتصال مع إعادة محاولة تلقائية
- ✅ **Global Exception Handler** - معالج عام للاستثناءات مع رسائل واضحة
- ✅ **Enhanced Error Logging** - تسجيل محسّن للأخطاء مع Deduplication
- ✅ **Performance Improvements** - تحسينات الأداء والتكوين
- 📝 [CHANGELOG.md](CHANGELOG.md) - سجل التغييرات الكامل
- 🔧 [TROUBLESHOOTING.md](TROUBLESHOOTING.md) - دليل استكشاف الأخطاء

---

**نظام محاسبة متكامل باللغة العربية مع دعم كامل للنصوص من اليمين لليسار**

**A comprehensive Arabic accounting system with full RTL support and modern business management capabilities**

</div>

---

## 🚀 **أحدث التطويرات - Latest Updates (أكتوبر 2024)**

### ✅ **الميزات الجديدة - New Features**
- **🧭 MVVM Navigation Service**: نظام تنقل متقدم مع NavigationService
- **🎯 Arabic RTL Sidebar**: قائمة جانبية عربية متكاملة مع البحث السريع
- **⚡ Dependency Injection**: حقن التبعيات مع Microsoft.Extensions.DI
- **🎨 Modern UI Components**: مكونات واجهة مستخدم عصرية ومتجاوبة
- **🔐 Advanced Security**: نظام أمان متطور مع تشفير البيانات

### ✅ **التحسينات التقنية - Technical Improvements**  
- **📋 Repository Pattern**: نمط Repository مع Unit of Work
- **🔄 Entity Framework Core**: ORM متقدم مع SQL Server LocalDB
- **🎯 MVVM Architecture**: هيكل MVVM كامل مع ViewModels
- **📊 Advanced Reporting**: نظام تقارير متطور مع تصدير متعدد الصيغ
- **🌐 Complete Localization**: ترجمة كاملة مع دعم RTL محسن

## 🏗️ **هيكل المشروع | Project Architecture**

```
AccountingSystem/
├── 📊 AccountingSystem.Models/        # نماذج البيانات والكائنات
├── 🗃️ AccountingSystem.Data/          # طبقة البيانات - Entity Framework Core
├── 🧮 AccountingSystem.Business/      # منطق الأعمال والخدمات
├── 🖥️ AccountingSystem.WPF/           # واجهة المستخدم - WPF مع MVVM
├── 🧪 AccountingSystem.Tests/         # الاختبارات والتحقق
└── 📋 AccountingSystem.sln            # ملف الحل الرئيسي
```

## ✨ **المميزات الأساسية | Core Features**

### 📈 **إدارة المبيعات والمشتريات | Sales & Purchase Management**
- 🧾 **فواتير المبيعات الاحترافية** - إنشاء وإدارة فواتير مبيعات مع حسابات دقيقة
- 🛒 **إدارة المشتريات الشاملة** - فواتير شراء متكاملة مع تتبع الموردين
- 💰 **نقطة البيع السريعة** - واجهة POS عصرية وسهلة الاستخدام
- 📊 **عروض الأسعار والكوتيشن** - إنشاء عروض أسعار احترافية
- ↩️ **إدارة المرتجعات** - مرتجعات المبيعات والمشتريات مع التحكم الكامل
- 🏷️ **الخصومات والعروض** - نظام خصومات متقدم وإدارة العروض الترويجية

### 👥 **إدارة العلاقات | Customer Relationship Management**
- 👤 **إدارة العملاء المتقدمة** - قاعدة بيانات شاملة مع تاريخ المعاملات
- 🏢 **إدارة الموردين** - معلومات تفصيلية وتقييم الأداء
- ⭐ **برنامج الولاء والمكافآت** - نقاط الولاء والمكافآت للعملاء المميزين
- 📞 **CRM متكامل** - إدارة علاقات العملاء مع التواصل والمتابعة
- 🔍 **البحث الذكي والمتقدم** - بحث سريع ودقيق في قواعد البيانات

### 📦 **إدارة المخزون | Advanced Inventory Management**
- 🏷️ **كتالوج المنتجات الشامل** - إدارة المنتجات مع الصور والمواصفات
- 📊 **التصنيفات المرنة** - نظام تصنيف هرمي للمنتجات والخدمات
- 📏 **وحدات القياس المتعددة** - دعم وحدات مختلفة مع التحويل التلقائي
- 📱 **نظام الباركود المتطور** - قراءة وطباعة الباركود مع QR Code
- 🔄 **تتبع الحركات الفوري** - مراقبة حية لحركات المخزون
- ⚠️ **التنبيهات الذكية** - تنبيهات للمخزون المنخفض والتواريخ المنتهية الصلاحية
- 📊 **الجرد الدوري والمستمر** - أدوات جرد متقدمة مع التسوية التلقائية

### 💰 **الإدارة المالية المتقدمة | Financial Management**
- 🏦 **إدارة الخزائن المتعددة** - خزائن متعددة مع التحكم في الصلاحيات
- 💸 **تتبع التدفقات النقدية** - مراقبة شاملة للمدفوعات والمقبوضات
- 📥 **سندات القبض والصرف** - إنشاء وإدارة السندات المالية
- 💳 **إدارة الشيكات والتحويلات** - تتبع الشيكات المؤجلة والتحويلات البنكية
- 🏛️ **الحسابات البنكية** - ربط وإدارة الحسابات البنكية المختلفة
- 💼 **إدارة المصروفات المفصلة** - تصنيف وتحليل جميع أنواع المصروفات
- 📊 **التحليل المالي العميق** - تحليلات مالية متقدمة وتوقعات

### 📊 **التقارير والتحليلات | Advanced Reports & Analytics**
- 📈 **لوحة تحكم تفاعلية** - مؤشرات أداء فورية وتفاعلية
- 📊 **التقارير المالية الشاملة** - جميع التقارير المحاسبية المطلوبة
- 📉 **الرسوم البيانية التفاعلية** - تمثيل بصري للبيانات مع التفاعل
- 🎯 **تقارير مخصصة قابلة للتعديل** - إنشاء تقارير حسب الحاجة
- 📱 **تصدير متعدد الصيغ** - PDF، Excel، Word، CSV
- 📊 **التحليل التنبؤي** - تحليلات مستقبلية وتوقعات الأداء

### 🔧 **إدارة النظام | System Administration**
- 👤 **إدارة المستخدمين المتقدمة** - نظام مستخدمين مع أدوار وصلاحيات دقيقة
- 🔐 **الأمان والحماية المتطورة** - تشفير متعدد المستويات وحماية البيانات
- 💾 **النسخ الاحتياطي الذكي** - نسخ احتياطي تلقائي مع الاستعادة الآمنة
- 🎨 **تخصيص الواجهة المرن** - ثيمات متعددة وإعدادات مرئية شخصية
- 📊 **مراقبة النظام الشاملة** - مراقبة الأداء والصحة العامة للنظام
- 🔄 **التحديثات التلقائية** - نظام تحديث آمن وتلقائي
- 📝 **سجل العمليات المفصل** - تتبع كامل لجميع العمليات والتغييرات

## 🚀 **التقنيات المستخدمة | Technology Stack**

### **Backend Technologies**
- **🏗️ .NET 8.0** - أحدث إصدار من إطار العمل .NET
- **💾 Entity Framework Core** - ORM متقدم مع Code-First approach
- **🗄️ SQL Server LocalDB** - قاعدة بيانات محلية موثوقة وسريعة
- **🔧 Repository Pattern** - نمط تصميم للوصول للبيانات
- **🔄 Unit of Work** - إدارة المعاملات والتزامن
- **💉 Dependency Injection** - حقن التبعيات مع Microsoft.Extensions.DI
- **📊 LINQ & Lambda** - استعلامات متقدمة وبرمجة وظيفية

### **Frontend Technologies**  
- **🖥️ WPF (Windows Presentation Foundation)** - واجهة مستخدم غرافيكية متطورة
- **🏛️ MVVM Pattern** - نمط Model-View-ViewModel للفصل الواضح
- **🧭 NavigationService** - نظام تنقل متقدم ومرن
- **🎨 Material Design** - تصميم عصري ومتجاوب
- **📱 RTL Support** - دعم كامل ومحسن للغة العربية
- **⚡ Data Binding** - ربط البيانات الثنائي الاتجاه
- **🎯 Command Pattern** - نمط الأوامر للتفاعل

### **Tools & Libraries**
- **📊 LiveCharts2** - رسوم بيانية تفاعلية وحديثة
- **🖨️ Advanced Reporting** - نظام تقارير متطور مع القوالب
- **🔐 BCrypt Security** - تشفير كلمات المرور المتقدم
- **📝 Serilog** - نظام سجلات شامل ومرن
- **✅ FluentValidation** - التحقق من صحة البيانات
- **🎨 HandyControl** - مكونات واجهة مستخدم محسنة
- **📊 Prism Framework** - إطار عمل MVVM متقدم

## 📋 **متطلبات التشغيل | System Requirements**

### **الحد الأدنى | Minimum Requirements**
- **💻 نظام التشغيل:** Windows 10 (64-bit) أو أحدث
- **🧮 المعالج:** Intel Core i3 أو AMD Ryzen 3 أو ما يعادلهما
- **🧠 الذاكرة:** 4 GB RAM
- **💾 مساحة القرص:** 3 GB مساحة فارغة
- **🖥️ دقة الشاشة:** 1366x768 كحد أدنى
- **🌐 .NET Runtime:** .NET 8.0 Desktop Runtime

### **المستوى الموصى به | Recommended Requirements**
- **💻 نظام التشغيل:** Windows 11 (64-bit)
- **🧮 المعالج:** Intel Core i5 أو AMD Ryzen 5 أو أفضل
- **🧠 الذاكرة:** 8 GB RAM أو أكثر
- **💾 مساحة القرص:** 10 GB مساحة فارغة
- **🖥️ دقة الشاشة:** 1920x1080 Full HD أو أعلى
- **🗄️ قاعدة البيانات:** SQL Server LocalDB أو SQL Server Express

### **المتطلبات الإضافية | Additional Requirements**
- **🖨️ للطباعة:** طابعة متوافقة مع Windows (اختيارية)
- **📱 للباركود:** كاميرا ويب أو قارئ باركود (اختيارية)
- **🌐 للتحديثات:** اتصال إنترنت (للتحديثات فقط)
- **🔊 للتنبيهات:** بطاقة صوت (اختيارية)

## 🛠️ **التثبيت والإعداد | Installation & Setup**

### **الطريقة الأولى: من الكود المصدري | From Source Code**

```bash
# 1️⃣ استنساخ المستودع
git clone https://github.com/marco56-prog/yu.git
cd yu

# 2️⃣ استعادة حزم NuGet
dotnet restore AccountingSystem.sln

# 3️⃣ بناء المشروع
dotnet build AccountingSystem.sln --configuration Release

# 4️⃣ تشغيل التطبيق
dotnet run --project AccountingSystem.WPF
```

### **الطريقة الثانية: باستخدام Visual Studio | Using Visual Studio**

1. **📥 تحميل Visual Studio**
   - احصل على Visual Studio 2022 Community (مجاني)
   - تأكد من تثبيت workload ".NET Desktop Development"

2. **📂 فتح المشروع**
   - افتح الملف `AccountingSystem.sln`
   - انتظر تحميل المشروع والحزم

3. **📦 استعادة الحزم**
   - Right-click على Solution
   - اختر "Restore NuGet Packages"

4. **🔨 بناء المشروع**
   - اختر `Build > Build Solution` (Ctrl+Shift+B)
   - تأكد من عدم وجود أخطاء في البناء

5. **▶️ تشغيل التطبيق**
   - اضغط F5 أو اختر `Debug > Start Debugging`

### **إعداد قاعدة البيانات | Database Setup**

قاعدة البيانات ستُنشأ تلقائياً عند التشغيل الأول:

```bash
# في حالة وجود مشاكل، يمكنك تحديث قاعدة البيانات يدوياً
cd AccountingSystem.Data
dotnet ef database update

# لإعادة إنشاء قاعدة البيانات من البداية
dotnet ef database drop
dotnet ef database update
```

### **بيانات الدخول الافتراضية | Default Login Credentials**

```
👤 المدير العام | Administrator:
   المستخدم: admin
   كلمة المرور: admin

👨‍💼 المحاسب | Accountant:
   المستخدم: accountant  
   كلمة المرور: admin

💡 يمكنك تغيير كلمات المرور من إعدادات النظام بعد الدخول
```

## 🎯 **دليل الاستخدام السريع | Quick Start Guide**

### **البدء الأول | First Launch**

1. **🚀 تشغيل التطبيق**
   - افتح `AccountingSystem.WPF.exe`
   - انتظر تحميل النظام وإنشاء قاعدة البيانات

2. **🔐 تسجيل الدخول**
   - استخدم: `admin` / `admin`
   - اضغط "دخول" أو Enter

3. **🏠 لوحة التحكم**
   - ستظهر لوحة التحكم الرئيسية
   - تصفح الإحصائيات والمؤشرات

4. **📚 استكشاف النظام**
   - استخدم القائمة الجانبية للتنقل
   - جرب البحث السريع في الشريط العلوي

### **الإعداد الأولي | Initial Setup**

#### **1️⃣ إعداد البيانات الأساسية | Master Data Setup**

```
🏷️ فئات المنتجات:
   المنتجات → إدارة الفئات → إضافة فئة جديدة
   
📏 وحدات القياس:
   المنتجات → وحدات القياس → إضافة وحدة
   
🏦 إعداد الخزائن:
   المالية → إدارة الخزائن → خزينة جديدة
```

#### **2️⃣ إدخال البيانات الأساسية | Basic Data Entry**

```
👥 إضافة العملاء:
   العملاء والموردين → إدارة العملاء → عميل جديد
   
🏢 إضافة الموردين:
   العملاء والموردين → إدارة الموردين → مورد جديد
   
📦 إضافة المنتجات:
   المنتجات → إدارة المنتجات → منتج جديد
```

#### **3️⃣ بدء العمليات | Start Operations**

```
🛒 إنشاء فاتورة مبيعات:
   المبيعات → فاتورة مبيعات جديدة (F2)
   
📥 إنشاء فاتورة مشتريات:
   المشتريات → فاتورة مشتريات جديدة (F3)
   
💰 نقطة البيع:
   المبيعات → نقطة البيع (F1)
```

## 🎨 **واجهة المستخدم والتنقل | UI & Navigation**

### **الميزات المرئية | Visual Features**
- **🌙 دعم الثيمات المتعددة:** فاتح، غامق، تلقائي، مخصص
- **🔄 RTL كامل ومحسن:** دعم شامل للعربية من اليمين لليسار
- **📱 تصميم متجاوب:** يتكيف مع أحجام وأنواع الشاشات المختلفة
- **🎯 تنقل ذكي ومرن:** قائمة جانبية مع بحث فوري ومرشحات
- **⚡ اختصارات شاملة:** اختصارات لوحة مفاتيح لجميع العمليات
- **🎨 تخصيص مرن:** إمكانية تخصيص الألوان والخطوط والتخطيط

### **اختصارات لوحة المفاتيح | Keyboard Shortcuts**

```
🔥 العمليات السريعة:
F1    💰 نقطة البيع السريعة
F2    🛒 فاتورة مبيعات جديدة
F3    📥 فاتورة مشتريات جديدة
F5    👥 إدارة العملاء
F6    🏢 إدارة الموردين
F7    📦 إدارة المنتجات
F8    📊 تقرير المخزون
F9    📈 التقارير العامة
F11   ⚙️ إعدادات النظام

⚡ العمليات العامة:
Ctrl+N    ➕ عنصر جديد
Ctrl+S    💾 حفظ
Ctrl+P    🖨️ طباعة
Ctrl+F    🔍 بحث
Ctrl+R    🔄 تحديث
Ctrl+Z    ↩️ تراجع
Escape    ❌ إلغاء/إغلاق
Enter     ✅ تأكيد

🧭 التنقل:
Tab           الانتقال للحقل التالي
Shift+Tab     الانتقال للحقل السابق
Ctrl+Tab      الانتقال بين النوافذ
Alt+F4        إغلاق النافذة النشطة
```

### **البحث والتصفية | Search & Filtering**
- **🔍 البحث الفوري:** بحث سريع في جميع البيانات
- **🎯 المرشحات المتقدمة:** تصفية حسب التاريخ، النوع، الحالة
- **📊 الفرز الذكي:** ترتيب النتائج حسب أي عمود
- **💾 حفظ المرشحات:** حفظ إعدادات البحث المفضلة

## 🔧 **الإعدادات المتقدمة | Advanced Configuration**

### **إعدادات قاعدة البيانات | Database Configuration**

تحرير ملف `appsettings.json` في مجلد `AccountingSystem.WPF`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AccountingSystemDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  },
  "AppSettings": {
    "CompanyName": "اسم شركتك هنا",
    "CompanyAddress": "عنوان الشركة التفصيلي",
    "CompanyPhone": "+20 xxx xxx xxxx",
    "CompanyEmail": "info@company.com",
    "Currency": "ج.م",
    "Language": "ar-EG",
    "Theme": "Auto",
    "DateFormat": "dd/MM/yyyy",
    "NumberFormat": "N2"
  },
  "BusinessSettings": {
    "DefaultTaxRate": 14.0,
    "AllowNegativeStock": false,
    "AutoGenerateInvoiceNumbers": true,
    "RequireCustomerInSales": true,
    "AutoPostInvoices": false,
    "ShowPricesInPOS": true
  }
}
```

### **إعدادات الطباعة | Printing Configuration**

```json
{
  "PrintSettings": {
    "DefaultPrinter": "",
    "InvoiceTemplate": "Arabic_Modern",
    "AutoPrint": false,
    "ShowPrintDialog": true,
    "ThermalPrinting": false,
    "PaperSize": "A4",
    "PrintMargins": {
      "Top": 20,
      "Bottom": 20,
      "Left": 20,
      "Right": 20
    },
    "WatermarkText": "",
    "ShowCompanyLogo": true
  }
}
```

### **إعدادات الأمان | Security Configuration**

```json
{
  "SecuritySettings": {
    "PasswordPolicy": {
      "MinLength": 6,
      "RequireUppercase": false,
      "RequireLowercase": false,
      "RequireNumbers": false,
      "RequireSpecialChars": false
    },
    "SessionSettings": {
      "SessionTimeout": 480,
      "AutoLock": false,
      "LockAfterMinutes": 15
    },
    "AuditSettings": {
      "LogAllOperations": true,
      "LogLoginAttempts": true,
      "RetainLogsForDays": 90
    }
  }
}
```

## 📊 **أمثلة العمليات | Operation Examples**

### **إنشاء فاتورة مبيعات | Creating Sales Invoice**

```csharp
// مثال برمجي لإنشاء فاتورة مبيعات
var invoice = new SalesInvoice
{
    CustomerId = selectedCustomer.Id,
    InvoiceDate = DateTime.Now,
    DueDate = DateTime.Now.AddDays(30),
    Items = new List<SalesInvoiceItem>
    {
        new SalesInvoiceItem
        {
            ProductId = product.Id,
            Quantity = 10,
            UnitPrice = 50.00m,
            Discount = 5.00m
        }
    }
};

await salesInvoiceService.CreateInvoiceAsync(invoice);
```

### **البحث في العملاء | Customer Search**

```csharp
// البحث المتقدم في العملاء
var searchCriteria = new CustomerSearchCriteria
{
    Name = "أحمد",
    City = "القاهرة",
    ActiveOnly = true,
    HasBalance = true
};

var customers = await customerService.SearchAsync(searchCriteria);
```

### **تقرير المبيعات | Sales Report**

```csharp
// إنشاء تقرير مبيعات للفترة المحددة
var reportCriteria = new SalesReportCriteria
{
    FromDate = DateTime.Now.AddMonths(-1),
    ToDate = DateTime.Now,
    CustomerId = null, // جميع العملاء
    GroupBy = ReportGrouping.Customer
};

var report = await reportService.GenerateSalesReportAsync(reportCriteria);
```

## 🤝 **المساهمة والتطوير | Contributing & Development**

### **كيفية المساهمة | How to Contribute**

1. **🍴 Fork المشروع**
   ```bash
   # Fork the repository on GitHub
   # Then clone your fork
   git clone https://github.com/YOUR_USERNAME/yu.git
   ```

2. **🌿 إنشاء فرع للميزة الجديدة**
   ```bash
   git checkout -b feature/amazing-new-feature
   ```

3. **💻 تطوير الميزة**
   - اتبع معايير الكود المحددة
   - أضف التوثيق اللازم
   - اكتب الاختبارات المطلوبة

4. **📝 Commit التغييرات**
   ```bash
   git commit -m "Add: Amazing new feature for inventory management"
   ```

5. **📤 Push ورفع Pull Request**
   ```bash
   git push origin feature/amazing-new-feature
   # ثم قم بإنشاء Pull Request على GitHub
   ```

### **معايير التطوير | Development Standards**

#### **معايير الكود | Code Standards**
- 📝 **التوثيق:** جميع الكلاسات والدوال موثقة بالعربية
- 🧪 **الاختبارات:** كود جديد يتطلب اختبارات وحدة شاملة
- 🎯 **SOLID Principles:** اتباع مبادئ SOLID في جميع التصاميم
- 🔄 **MVVM Pattern:** استخدام نمط MVVM في جميع الواجهات
- 🌐 **Localization:** دعم كامل للترجمة والعولمة
- ⚡ **Performance:** تحسين الأداء وإدارة الذاكرة
- 🔒 **Security:** اتباع أفضل ممارسات الأمان

#### **هيكل المساهمة | Contribution Structure**
```
📝 Documentation (Arabic preferred)
🧪 Unit Tests (Required for new features)  
🔧 Code Reviews (Mandatory)
📊 Performance Tests (For critical features)
🌐 Localization Support (Arabic/English)
🎨 UI/UX Guidelines (RTL-first design)
```

### **بيئة التطوير | Development Environment**

```bash
# إعداد بيئة التطوير
git clone https://github.com/marco56-prog/yu.git
cd yu

# تثبيت الأدوات المطلوبة
dotnet tool install --global dotnet-ef
dotnet tool install --global dotnet-reportgenerator-globaltool

# إعداد قاعدة بيانات التطوير
cd AccountingSystem.Data
dotnet ef database update

# تشغيل الاختبارات
cd ..
dotnet test
```

## 📞 **الدعم والمساعدة | Support & Help**

### **قنوات الدعم | Support Channels**

- **📖 الوثائق الشاملة:** راجع ملف `README_ADVANCED.md` للتفاصيل التقنية
- **🐛 الإبلاغ عن الأخطاء:** [GitHub Issues](https://github.com/marco56-prog/yu/issues)
- **💡 الاقتراحات والمناقشات:** [GitHub Discussions](https://github.com/marco56-prog/yu/discussions)
- **📧 الاتصال المباشر:** developer@accountingsystem.com
- **📱 الدعم السريع:** [Telegram Support](https://t.me/AccountingSystemSupport)

### **الأسئلة الشائعة | FAQ**

#### **❓ لا يمكنني تسجيل الدخول**
```
🔍 تحقق من:
✅ اسم المستخدم: admin
✅ كلمة المرور: admin  
✅ تشغيل قاعدة البيانات
✅ وجود جدول Users في قاعدة البيانات

🔧 الحل:
dotnet ef database update
```

#### **❓ مشكلة في قاعدة البيانات**
```bash
# حذف وإعادة إنشاء قاعدة البيانات
dotnet ef database drop
dotnet ef database update
```

#### **❓ مشكلة في الحزم والمكتبات**
```bash
# إعادة تثبيت جميع الحزم
dotnet restore --force
dotnet clean
dotnet build
```

#### **❓ بطء في الأداء**
```
🔍 تحقق من:
✅ مساحة القرص المتوفرة (5GB+)
✅ الذاكرة المتاحة (4GB+)
✅ إغلاق البرامج غير المستخدمة
✅ تحديث Windows

🔧 تحسين الأداء:
- إعادة تشغيل التطبيق
- تحسين إعدادات قاعدة البيانات
- تنظيف البيانات المؤقتة
```

### **التقارير والأخطاء | Bug Reports**

عند الإبلاغ عن خطأ، يرجى تضمين:

```markdown
## وصف المشكلة
وصف واضح ومفصل للمشكلة

## خطوات إعادة الإنتاج
1. اذهب إلى '...'
2. اضغط على '...'  
3. شاهد الخطأ

## السلوك المتوقع
ما الذي كان متوقعاً أن يحدث

## لقطات الشاشة
إن أمكن، أضف لقطات شاشة

## معلومات البيئة
- نظام التشغيل: [مثال Windows 11]
- إصدار .NET: [مثال 8.0]
- إصدار التطبيق: [مثال 1.2.0]
```

## 📄 **الترخيص والاستخدام | License & Usage**

هذا المشروع مرخص تحت **رخصة MIT** - راجع ملف [LICENSE](LICENSE) للتفاصيل الكاملة.

### **ما يمكنك فعله | What You Can Do**
- ✅ **الاستخدام التجاري** - استخدم المشروع في أعمالك التجارية
- ✅ **التعديل** - عدّل الكود حسب احتياجاتك
- ✅ **التوزيع** - وزع المشروع أو نسخ معدلة منه
- ✅ **الاستخدام الخاص** - استخدم المشروع لأغراضك الشخصية
- ✅ **إنشاء مشاريع مشتقة** - ابني عليه مشاريع جديدة

### **الشروط | Conditions**
- 📄 **تضمين الترخيص** - يجب تضمين نص الترخيص في التوزيعات
- 👤 **ذكر المصدر** - ذكر المؤلفين الأصليين مُستحسن

### **القيود | Limitations**
- ❌ **لا ضمانات** - المشروع مقدم "كما هو" بدون ضمانات
- ❌ **لا مسؤولية قانونية** - المطورون غير مسؤولين عن أي أضرار

```
MIT License

Copyright (c) 2024 AccountingSystem Contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
```

## 🙏 **شكر وتقدير | Acknowledgments**

### **المساهمون والداعمون | Contributors & Supporters**

- **🏢 Microsoft** - لإطار عمل .NET الرائع وأدوات التطوير المتقدمة
- **🗄️ Entity Framework Team** - لـ ORM متقدم ومرن يسهل إدارة البيانات
- **🖥️ WPF Community** - للدعم المستمر والموارد القيمة والأمثلة العملية
- **🌍 Arabic Dev Community** - للإلهام والدعم والمراجعات البناءة
- **👨‍💻 جميع المساهمين** - لجهودهم المتواصلة في تطوير وتحسين النظام
- **🏢 الشركات المستخدمة** - للملاحظات والاقتراحات القيمة من الاستخدام الفعلي
- **🧪 مجتمع المختبرين** - للمساعدة في اكتشاف وحل المشاكل

### **المكتبات والأدوات المستخدمة | Libraries & Tools Used**

```
🏗️ Core Framework:
   .NET 8.0, Entity Framework Core, WPF

📊 UI Components:  
   HandyControl, LiveCharts2, Material Design

🔧 Development Tools:
   Visual Studio 2022, Git, NuGet

🧪 Testing Framework:
   xUnit, Moq, FluentAssertions

📝 Documentation:
   Markdown, GitHub Pages, XML Documentation

🔐 Security Libraries:
   BCrypt.NET, System.Security.Cryptography
```

### **إهداء خاص | Special Thanks**

نتقدم بالشكر الخاص إلى:

- **💼 رواد الأعمال العرب** الذين ألهمونا بحاجتهم لأنظمة محاسبية عربية متقدمة
- **👨‍💼 المحاسبين والماليين** الذين قدموا خبراتهم العملية في تطوير النظام
- **🎓 الطلاب والأكاديميين** الذين يستخدمون النظام لأغراض تعليمية
- **🌟 المجتمع مفتوح المصدر العربي** لدعمهم المتواصل للمشاريع العربية

---

<div align="center">

## 💝 **إذا أعجبك المشروع، لا تنس إعطاءه ⭐**

[![GitHub stars](https://img.shields.io/github/stars/marco56-prog/yu?style=for-the-badge&logo=github)](https://github.com/marco56-prog/yu/stargazers)
[![GitHub forks](https://img.shields.io/github/forks/marco56-prog/yu?style=for-the-badge&logo=github)](https://github.com/marco56-prog/yu/network)
[![GitHub issues](https://img.shields.io/github/issues/marco56-prog/yu?style=for-the-badge&logo=github)](https://github.com/marco56-prog/yu/issues)
[![GitHub license](https://img.shields.io/github/license/marco56-prog/yu?style=for-the-badge)](https://github.com/marco56-prog/yu/blob/master/LICENSE)

### **🚀 انضم لمجتمع المطورين العرب | Join Arab Developers Community**

[💬 Telegram](https://t.me/ArabDevelopers) | [🐦 Twitter](https://twitter.com/AccountingSystemAR) | [📧 Email](mailto:developer@accountingsystem.com)

### **صُنع بـ ❤️ للمجتمع العربي**
### **Made with ❤️ for the Arabic Business Community**

---

**© 2024 AccountingSystem Contributors. All rights reserved.**

</div>