namespace AccountingSystem.WPF.Constants;

/// <summary>
/// Constants used throughout the application
/// </summary>
public static class ApplicationConstants
{
    // Date and Time Formats
    public const string StandardDateTimeFormat = "yyyy-MM-dd HH:mm:ss";
    public const string ShortDateFormat = "yyyy-MM-dd";
    public const string TimeFormat = "HH:mm:ss";
    
    // Message Box Titles
    public const string ErrorTitleArabic = "خطأ";
    public const string ErrorTitleEnglish = "Error";
    public const string WarningTitleArabic = "تحذير";
    public const string WarningTitleEnglish = "Warning";
    public const string InfoTitleArabic = "معلومات";
    public const string InfoTitleEnglish = "Information";
    public const string SuccessTitleArabic = "نجح";
    public const string SuccessTitleEnglish = "Success";
    
    // Common Messages
    public const string DevelopmentMessageArabic = "قيد التطوير";
    public const string DevelopmentMessageEnglish = "Under Development";
    public const string LoadingMessageArabic = "جاري التحميل...";
    public const string LoadingMessageEnglish = "Loading...";
    
    // Permissions
    public const string AdminPermission = "Admin";
    public const string ManagerPermission = "Manager";
    public const string CashierPermission = "Cashier";
    public const string AccountantPermission = "Accountant";
    
    // Window Names
    public const string SalesInvoiceWindow = "SalesInvoiceWindow";
    public const string PurchaseInvoiceWindow = "PurchaseInvoiceWindow";
    public const string ProductsWindow = "ProductsWindow";
    public const string CustomersWindow = "CustomersWindow";
    public const string SuppliersWindow = "SuppliersWindow";
    public const string ReportsWindow = "ReportsWindow";
    public const string SettingsWindow = "SettingsWindow";
    
    // File Extensions
    public const string ExcelFileExtension = ".xlsx";
    public const string PdfFileExtension = ".pdf";
    public const string CsvFileExtension = ".csv";
    
    // Cache Keys
    public const string UserPermissionsCacheKey = "UserPermissions";
    public const string SystemSettingsCacheKey = "SystemSettings";
    
    // Regular Expressions
    public const string EmailRegexPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public const string PhoneNumberRegexPattern = @"^[\d\s\-\+\(\)]+$";
    
    // Database
    public const int DefaultCommandTimeout = 30;
    public const int MaxRetryAttempts = 3;
    
    // UI
    public const int DefaultAnimationDuration = 300;
    public const int ToastNotificationDuration = 3000;
    public const double DefaultOpacity = 1.0;
    public const double DisabledOpacity = 0.5;
}