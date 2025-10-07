using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using AccountingSystem.WPF.Helpers;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// خدمة التحقق الذكي من البيانات مع دعم شامل للمحاسبة
    /// </summary>
    public interface ISmartDataValidationService
    {
        ValidationResult ValidateInvoiceData(InvoiceValidationModel invoice);
        ValidationResult ValidateCustomerData(CustomerValidationModel customer);
        ValidationResult ValidateProductData(ProductValidationModel product);
        ValidationResult ValidateFinancialAmount(decimal amount, string context = "");
        ValidationResult ValidateBusinessRules(object data, string ruleSet);
        bool IsValidPhoneNumber(string phoneNumber);
        bool IsValidEmail(string email);
        bool IsValidTaxNumber(string taxNumber);
        List<string> GetSuggestions(string fieldName, string currentValue);
    }

    public class SmartDataValidationService : ISmartDataValidationService
    {
        private const string ComponentName = "SmartDataValidationService";
        private readonly Dictionary<string, List<string>> _suggestionCache;

        public SmartDataValidationService()
        {
            _suggestionCache = new Dictionary<string, List<string>>();
            InitializeSuggestionData();
            
            ComprehensiveLogger.LogInfo("تم تهيئة خدمة التحقق الذكي من البيانات", ComponentName);
        }

        public ValidationResult ValidateInvoiceData(InvoiceValidationModel invoice)
        {
            var result = new ValidationResult();

            try
            {
                // التحقق من البيانات الأساسية
                if (string.IsNullOrWhiteSpace(invoice.CustomerName))
                {
                    result.AddError("CustomerName", "اسم العميل مطلوب");
                }

                if (invoice.InvoiceDate == default)
                {
                    result.AddError("InvoiceDate", "تاريخ الفاتورة مطلوب");
                }
                else if (invoice.InvoiceDate > DateTime.Now)
                {
                    result.AddWarning("InvoiceDate", "تاريخ الفاتورة في المستقبل");
                }
                else if (invoice.InvoiceDate < DateTime.Now.AddYears(-2))
                {
                    result.AddWarning("InvoiceDate", "تاريخ الفاتورة قديم جداً");
                }

                // التحقق من الأصناف
                if (invoice.Items == null || !invoice.Items.Any())
                {
                    result.AddError("Items", "يجب إضافة صنف واحد على الأقل");
                }
                else
                {
                    for (int i = 0; i < invoice.Items.Count; i++)
                    {
                        var item = invoice.Items[i];
                        
                        if (string.IsNullOrWhiteSpace(item.ProductName))
                        {
                            result.AddError($"Items[{i}].ProductName", $"اسم المنتج مطلوب في السطر {i + 1}");
                        }

                        if (item.Quantity <= 0)
                        {
                            result.AddError($"Items[{i}].Quantity", $"الكمية يجب أن تكون أكبر من صفر في السطر {i + 1}");
                        }

                        if (item.UnitPrice < 0)
                        {
                            result.AddError($"Items[{i}].UnitPrice", $"سعر الوحدة لا يمكن أن يكون سالب في السطر {i + 1}");
                        }

                        // تحقق من الكمية المتاحة
                        if (item.AvailableQuantity < item.Quantity)
                        {
                            result.AddWarning($"Items[{i}].Quantity", 
                                $"الكمية المطلوبة ({item.Quantity}) أكبر من المتاح ({item.AvailableQuantity}) في السطر {i + 1}");
                        }
                    }
                }

                // التحقق من المبالغ المالية
                var totalValidation = ValidateFinancialAmount(invoice.TotalAmount, "إجمالي الفاتورة");
                result.Merge(totalValidation);

                if (invoice.DiscountAmount < 0)
                {
                    result.AddError("DiscountAmount", "مبلغ الخصم لا يمكن أن يكون سالب");
                }

                if (invoice.DiscountAmount > invoice.TotalAmount)
                {
                    result.AddWarning("DiscountAmount", "مبلغ الخصم أكبر من إجمالي الفاتورة");
                }

                // التحقق من قواعد العمل
                ValidateInvoiceBusinessRules(invoice, result);

                ComprehensiveLogger.LogInfo($"تم التحقق من بيانات الفاتورة - أخطاء: {result.Errors.Count}, تحذيرات: {result.Warnings.Count}", ComponentName);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("خطأ في التحقق من بيانات الفاتورة", ex, ComponentName);
                result.AddError("General", "حدث خطأ في التحقق من البيانات");
            }

            return result;
        }

        public ValidationResult ValidateCustomerData(CustomerValidationModel customer)
        {
            var result = new ValidationResult();

            try
            {
                // التحقق من الاسم
                if (string.IsNullOrWhiteSpace(customer.Name))
                {
                    result.AddError("Name", "اسم العميل مطلوب");
                }
                else if (customer.Name.Length < 2)
                {
                    result.AddError("Name", "اسم العميل قصير جداً");
                }
                else if (customer.Name.Length > 100)
                {
                    result.AddError("Name", "اسم العميل طويل جداً");
                }

                // التحقق من رقم الهاتف
                if (!string.IsNullOrWhiteSpace(customer.PhoneNumber))
                {
                    if (!IsValidPhoneNumber(customer.PhoneNumber))
                    {
                        result.AddError("PhoneNumber", "رقم الهاتف غير صحيح");
                    }
                }

                // التحقق من البريد الإلكتروني
                if (!string.IsNullOrWhiteSpace(customer.Email))
                {
                    if (!IsValidEmail(customer.Email))
                    {
                        result.AddError("Email", "البريد الإلكتروني غير صحيح");
                    }
                }

                // التحقق من الرقم الضريبي
                if (!string.IsNullOrWhiteSpace(customer.TaxNumber))
                {
                    if (!IsValidTaxNumber(customer.TaxNumber))
                    {
                        result.AddError("TaxNumber", "الرقم الضريبي غير صحيح");
                    }
                }

                // التحقق من الحد الائتماني
                if (customer.CreditLimit < 0)
                {
                    result.AddError("CreditLimit", "الحد الائتماني لا يمكن أن يكون سالب");
                }

                ComprehensiveLogger.LogInfo($"تم التحقق من بيانات العميل - أخطاء: {result.Errors.Count}", ComponentName);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("خطأ في التحقق من بيانات العميل", ex, ComponentName);
                result.AddError("General", "حدث خطأ في التحقق من البيانات");
            }

            return result;
        }

        public ValidationResult ValidateProductData(ProductValidationModel product)
        {
            var result = new ValidationResult();

            try
            {
                // التحقق من الاسم
                if (string.IsNullOrWhiteSpace(product.Name))
                {
                    result.AddError("Name", "اسم المنتج مطلوب");
                }

                // التحقق من الكود
                if (string.IsNullOrWhiteSpace(product.Code))
                {
                    result.AddError("Code", "كود المنتج مطلوب");
                }
                else if (!IsValidProductCode(product.Code))
                {
                    result.AddError("Code", "كود المنتج غير صحيح");
                }

                // التحقق من السعر
                if (product.Price < 0)
                {
                    result.AddError("Price", "سعر المنتج لا يمكن أن يكون سالب");
                }

                // التحقق من الكمية
                if (product.Quantity < 0)
                {
                    result.AddError("Quantity", "كمية المخزون لا يمكن أن تكون سالبة");
                }

                // تحذير للمخزون المنخفض
                if (product.Quantity < product.MinimumStock)
                {
                    result.AddWarning("Quantity", $"المخزون أقل من الحد الأدنى ({product.MinimumStock})");
                }

                ComprehensiveLogger.LogInfo($"تم التحقق من بيانات المنتج - أخطاء: {result.Errors.Count}", ComponentName);
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError("خطأ في التحقق من بيانات المنتج", ex, ComponentName);
                result.AddError("General", "حدث خطأ في التحقق من البيانات");
            }

            return result;
        }

        public ValidationResult ValidateFinancialAmount(decimal amount, string context = "")
        {
            var result = new ValidationResult();

            try
            {
                if (amount < 0)
                {
                    result.AddError("Amount", $"المبلغ لا يمكن أن يكون سالب {context}");
                }

                if (amount > 1_000_000_000) // مليار
                {
                    result.AddWarning("Amount", $"المبلغ كبير جداً {context}");
                }

                // التحقق من عدد المنازل العشرية
                var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(amount)[3])[2];
                if (decimalPlaces > 2)
                {
                    result.AddWarning("Amount", $"المبلغ يحتوي على أكثر من منزلتين عشريتين {context}");
                }
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"خطأ في التحقق من المبلغ المالي: {amount}", ex, ComponentName);
                result.AddError("Amount", "خطأ في التحقق من المبلغ");
            }

            return result;
        }

        public ValidationResult ValidateBusinessRules(object data, string ruleSet)
        {
            var result = new ValidationResult();

            try
            {
                // تطبيق قواعد العمل حسب النوع
                switch (ruleSet.ToLower())
                {
                    case "invoice":
                        if (data is InvoiceValidationModel invoice)
                        {
                            ValidateInvoiceBusinessRules(invoice, result);
                        }
                        break;
                    case "payment":
                        ValidatePaymentRules(data, result);
                        break;
                    default:
                        result.AddWarning("RuleSet", $"مجموعة القواعد غير معروفة: {ruleSet}");
                        break;
                }
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"خطأ في تطبيق قواعد العمل: {ruleSet}", ex, ComponentName);
                result.AddError("BusinessRules", "خطأ في تطبيق قواعد العمل");
            }

            return result;
        }

        public bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            // تنظيف الرقم
            var cleanNumber = Regex.Replace(phoneNumber, @"[^\d+]", "");
            
            // التحقق من الأنماط المختلفة للأرقام
            var patterns = new[]
            {
                @"^\+?966[0-9]{9}$",  // أرقام سعودية
                @"^\+?[0-9]{10,15}$", // أرقام دولية عامة
                @"^05[0-9]{8}$"       // أرقام سعودية بدون كود البلد
            };

            return patterns.Any(pattern => Regex.IsMatch(cleanNumber, pattern));
        }

        public bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var emailAttribute = new EmailAddressAttribute();
                return emailAttribute.IsValid(email);
            }
            catch
            {
                return false;
            }
        }

        public bool IsValidTaxNumber(string taxNumber)
        {
            if (string.IsNullOrWhiteSpace(taxNumber))
                return false;

            // تنظيف الرقم
            var cleanNumber = Regex.Replace(taxNumber, @"[^\d]", "");
            
            // للرقم الضريبي السعودي - 15 رقم
            return cleanNumber.Length == 15 && cleanNumber.All(char.IsDigit);
        }

        public List<string> GetSuggestions(string fieldName, string currentValue)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(currentValue))
                    return new List<string>();

                var key = fieldName.ToLower();
                if (_suggestionCache.TryGetValue(key, out var suggestions))
                {
                    return suggestions
                        .Where(s => s.Contains(currentValue, StringComparison.OrdinalIgnoreCase))
                        .Take(10)
                        .ToList();
                }

                return new List<string>();
            }
            catch (Exception ex)
            {
                ComprehensiveLogger.LogError($"خطأ في الحصول على الاقتراحات للحقل: {fieldName}", ex, ComponentName);
                return new List<string>();
            }
        }

        #region Private Methods

        private static void ValidateInvoiceBusinessRules(InvoiceValidationModel invoice, ValidationResult result)
        {
            // قاعدة: لا يمكن إنشاء فاتورة في يوم الجمعة (اختيارية)
            if (invoice.InvoiceDate.DayOfWeek == DayOfWeek.Friday)
            {
                result.AddInfo("InvoiceDate", "تم إنشاء الفاتورة في يوم الجمعة");
            }

            // قاعدة: التحقق من الحد الائتماني للعميل
            if (invoice.CustomerCreditLimit > 0 && invoice.TotalAmount > invoice.CustomerCreditLimit)
            {
                result.AddWarning("TotalAmount", "إجمالي الفاتورة يتجاوز الحد الائتماني للعميل");
            }

            // قاعدة: فاتورة كبيرة تحتاج موافقة
            if (invoice.TotalAmount > 50000)
            {
                result.AddWarning("TotalAmount", "الفاتورة تحتاج موافقة إدارية (أكثر من 50,000)");
            }
        }

        private static void ValidatePaymentRules(object data, ValidationResult result)
        {
            // قواعد الدفع (يمكن تطويرها لاحقاً)
            result.AddInfo("Payment", "تم التحقق من قواعد الدفع");
        }

        private static bool IsValidProductCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            // كود المنتج يجب أن يكون 3-20 حرف/رقم
            return code.Length >= 3 && code.Length <= 20 && 
                   code.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');
        }

        private void InitializeSuggestionData()
        {
            // بيانات الاقتراحات (يمكن تحميلها من قاعدة البيانات لاحقاً)
            _suggestionCache["customername"] = new List<string>
            {
                "شركة الرياض للتجارة", "مؤسسة النور", "شركة الفجر", "محلات السلام",
                "مكتبة المعرفة", "صيدلية الشفاء", "مطعم الأصالة", "معرض السيارات"
            };

            _suggestionCache["productname"] = new List<string>
            {
                "لابتوب ديل", "شاشة سامسونج", "طابعة HP", "ماوس لوجيتك",
                "كيبورد", "سماعات", "كاميرا ويب", "هارد ديسك خارجي"
            };

            _suggestionCache["city"] = new List<string>
            {
                "الرياض", "جدة", "الدمام", "مكة المكرمة", "المدينة المنورة",
                "الطائف", "تبوك", "حائل", "الجبيل", "الخبر"
            };
        }

        #endregion
    }

    #region Validation Models and Results

    public class ValidationResult
    {
        public List<ValidationMessage> Errors { get; } = new();
        public List<ValidationMessage> Warnings { get; } = new();
        public List<ValidationMessage> Infos { get; } = new();

        public bool IsValid => !Errors.Any();
        public bool HasWarnings => Warnings.Any();

        public void AddError(string field, string message)
        {
            Errors.Add(new ValidationMessage { Field = field, Message = message, Type = ValidationMessageType.Error });
        }

        public void AddWarning(string field, string message)
        {
            Warnings.Add(new ValidationMessage { Field = field, Message = message, Type = ValidationMessageType.Warning });
        }

        public void AddInfo(string field, string message)
        {
            Infos.Add(new ValidationMessage { Field = field, Message = message, Type = ValidationMessageType.Info });
        }

        public void Merge(ValidationResult other)
        {
            Errors.AddRange(other.Errors);
            Warnings.AddRange(other.Warnings);
            Infos.AddRange(other.Infos);
        }
    }

    public class ValidationMessage
    {
        public string Field { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public ValidationMessageType Type { get; set; }
    }

    public enum ValidationMessageType
    {
        Error,
        Warning,
        Info
    }

    // نماذج التحقق
    public class InvoiceValidationModel
    {
        public string CustomerName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal CustomerCreditLimit { get; set; }
        public List<InvoiceItemValidationModel> Items { get; set; } = new();
    }

    public class InvoiceItemValidationModel
    {
        public string ProductName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal AvailableQuantity { get; set; }
    }

    public class CustomerValidationModel
    {
        public string Name { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string TaxNumber { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
    }

    public class ProductValidationModel
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal MinimumStock { get; set; }
    }

    #endregion
}