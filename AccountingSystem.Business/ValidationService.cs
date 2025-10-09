using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace AccountingSystem.Business
{
    /// <summary>
    /// خدمة التحقق من صحة البيانات - موحدة لكامل النظام
    /// </summary>
    public static class ValidationService
    {
        #region Email Validation

        private static readonly Regex EmailRegex = new(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// التحقق من صحة البريد الإلكتروني
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                // التحقق من الطول
                if (email.Length > 254)
                    return false;

                return EmailRegex.IsMatch(email.Trim());
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        #endregion

        #region Phone Validation

        /// <summary>
        /// التحقق من صحة رقم الهاتف (يدعم الأرقام المصرية والدولية)
        /// </summary>
        public static bool IsValidPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return false;

            // استخراج الأرقام فقط
            var digits = new string(phone.Where(char.IsDigit).ToArray());

            // التحقق من الطول (7-15 رقم حسب المعايير الدولية)
            if (digits.Length < 7 || digits.Length > 15)
                return false;

            // التحقق من الأرقام المصرية
            if (IsEgyptianPhone(digits))
                return true;

            // التحقق من الأرقام الدولية
            return IsInternationalPhone(digits);
        }

        private static bool IsEgyptianPhone(string digits)
        {
            // أرقام محمولة مصرية: تبدأ بـ 010, 011, 012, 015
            if (digits.Length == 11 && digits.StartsWith("01", StringComparison.Ordinal))
            {
                var secondDigit = digits[2];
                return secondDigit == '0' || secondDigit == '1' || secondDigit == '2' || secondDigit == '5';
            }

            // أرقام أرضية مصرية: تبدأ بكود المحافظة
            if (digits.Length >= 8 && digits.Length <= 10)
            {
                var cityCode = digits[..2];
                return IsValidEgyptianCityCode(cityCode);
            }

            return false;
        }

        private static bool IsValidEgyptianCityCode(string cityCode)
        {
            // أكواد المحافظات المصرية الرئيسية
            string[] validCodes = { "02", "03", "04", "05", "06", "07", "08", "09", "13", "15", "16", "17", "18", "19", "20" };
            return validCodes.Contains(cityCode);
        }

        private static bool IsInternationalPhone(string digits)
        {
            // أرقام دولية عامة
            return digits.Length >= 7 && digits.Length <= 15;
        }

        #endregion

        #region Name Validation

        /// <summary>
        /// التحقق من صحة الاسم (يدعم العربية والإنجليزية)
        /// </summary>
        public static bool IsValidName(string name, int minLength = 2, int maxLength = 100)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            var trimmedName = name.Trim();

            // التحقق من الطول
            if (trimmedName.Length < minLength || trimmedName.Length > maxLength)
                return false;

            // التحقق من الأحرف المسموحة (عربي، إنجليزي، مسافات، شرطة، نقطة)
            return Regex.IsMatch(trimmedName, @"^[\u0600-\u06FFa-zA-Z\s\-\.]+$");
        }

        #endregion

        #region Numeric Validation

        /// <summary>
        /// التحقق من صحة المبلغ المالي
        /// </summary>
        public static bool IsValidAmount(decimal amount, decimal minValue = 0, decimal maxValue = decimal.MaxValue)
        {
            return amount >= minValue && amount <= maxValue && amount != decimal.MinValue;
        }

        /// <summary>
        /// التحقق من صحة الكمية
        /// </summary>
        public static bool IsValidQuantity(decimal quantity, bool allowZero = false)
        {
            if (allowZero)
                return quantity >= 0;
            
            return quantity > 0;
        }

        /// <summary>
        /// التحقق من صحة النسبة المئوية
        /// </summary>
        public static bool IsValidPercentage(decimal percentage, decimal min = 0, decimal max = 100)
        {
            return percentage >= min && percentage <= max;
        }

        #endregion

        #region Date Validation

        /// <summary>
        /// التحقق من صحة التاريخ
        /// </summary>
        public static bool IsValidDate(DateTime date, DateTime? minDate = null, DateTime? maxDate = null)
        {
            if (date == default)
                return false;

            var min = minDate ?? new DateTime(1900, 1, 1);
            var max = maxDate ?? DateTime.Now.AddYears(10);

            return date >= min && date <= max;
        }

        /// <summary>
        /// التحقق من أن تاريخ الانتهاء بعد تاريخ البداية
        /// </summary>
        public static bool IsValidDateRange(DateTime startDate, DateTime endDate)
        {
            return endDate >= startDate;
        }

        #endregion

        #region Business Logic Validation

        /// <summary>
        /// التحقق من كود المنتج
        /// </summary>
        public static bool IsValidProductCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                return false;

            var trimmedCode = code.Trim();
            
            // الطول بين 2-20 حرف
            if (trimmedCode.Length < 2 || trimmedCode.Length > 20)
                return false;

            // يحتوي على أحرف وأرقام فقط
            return Regex.IsMatch(trimmedCode, @"^[a-zA-Z0-9\-_]+$");
        }

        /// <summary>
        /// التحقق من الرقم الضريبي
        /// </summary>
        public static bool IsValidTaxNumber(string taxNumber)
        {
            if (string.IsNullOrWhiteSpace(taxNumber))
                return false;

            // الرقم الضريبي المصري: 9 أرقام
            var digits = new string(taxNumber.Where(char.IsDigit).ToArray());
            return digits.Length == 9;
        }

        /// <summary>
        /// التحقق من الرقم التجاري
        /// </summary>
        public static bool IsValidCommercialNumber(string commercialNumber)
        {
            if (string.IsNullOrWhiteSpace(commercialNumber))
                return false;

            var trimmedNumber = commercialNumber.Trim();
            
            // الطول بين 5-20 حرف/رقم
            if (trimmedNumber.Length < 5 || trimmedNumber.Length > 20)
                return false;

            // يحتوي على أرقام وأحرف فقط
            return Regex.IsMatch(trimmedNumber, @"^[a-zA-Z0-9]+$");
        }

        #endregion

        #region Validation Results

        /// <summary>
        /// نتيجة التحقق من صحة البيانات
        /// </summary>
        public class ValidationResult
        {
            public bool IsValid { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public string PropertyName { get; set; } = string.Empty;

            public static ValidationResult Success() => new() { IsValid = true };
            
            public static ValidationResult Error(string message, string propertyName = "")
                => new() { IsValid = false, ErrorMessage = message, PropertyName = propertyName };
        }

        /// <summary>
        /// التحقق الشامل من بيانات المورد
        /// </summary>
        public static ValidationResult ValidateSupplier(string name, string? email, string? phone, string? taxNumber = null)
        {
            if (!IsValidName(name, 2, 100))
                return ValidationResult.Error("اسم المورد غير صحيح", nameof(name));

            if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
                return ValidationResult.Error("البريد الإلكتروني غير صحيح", nameof(email));

            if (!string.IsNullOrEmpty(phone) && !IsValidPhone(phone))
                return ValidationResult.Error("رقم الهاتف غير صحيح", nameof(phone));

            if (!string.IsNullOrEmpty(taxNumber) && !IsValidTaxNumber(taxNumber))
                return ValidationResult.Error("الرقم الضريبي غير صحيح", nameof(taxNumber));

            return ValidationResult.Success();
        }

        /// <summary>
        /// التحقق الشامل من بيانات العميل
        /// </summary>
        public static ValidationResult ValidateCustomer(string name, string? email, string? phone, decimal creditLimit = 0)
        {
            if (!IsValidName(name, 2, 100))
                return ValidationResult.Error("اسم العميل غير صحيح", nameof(name));

            if (!string.IsNullOrEmpty(email) && !IsValidEmail(email))
                return ValidationResult.Error("البريد الإلكتروني غير صحيح", nameof(email));

            if (!string.IsNullOrEmpty(phone) && !IsValidPhone(phone))
                return ValidationResult.Error("رقم الهاتف غير صحيح", nameof(phone));

            if (!IsValidAmount(creditLimit, 0, 1000000))
                return ValidationResult.Error("حد الائتمان غير صحيح", nameof(creditLimit));

            return ValidationResult.Success();
        }

        /// <summary>
        /// التحقق الشامل من بيانات المنتج
        /// </summary>
        public static ValidationResult ValidateProduct(string name, string code, decimal salePrice, decimal purchasePrice, decimal minStock = 0)
        {
            if (!IsValidName(name, 2, 200))
                return ValidationResult.Error("اسم المنتج غير صحيح", nameof(name));

            if (!IsValidProductCode(code))
                return ValidationResult.Error("كود المنتج غير صحيح", nameof(code));

            if (!IsValidAmount(salePrice, 0.01m, 1000000))
                return ValidationResult.Error("سعر البيع غير صحيح", nameof(salePrice));

            if (!IsValidAmount(purchasePrice, 0.01m, 1000000))
                return ValidationResult.Error("سعر الشراء غير صحيح", nameof(purchasePrice));

            if (salePrice < purchasePrice)
                return ValidationResult.Error("سعر البيع يجب أن يكون أكبر من سعر الشراء", nameof(salePrice));

            if (!IsValidQuantity(minStock, true))
                return ValidationResult.Error("الحد الأدنى للمخزون غير صحيح", nameof(minStock));

            return ValidationResult.Success();
        }

        #endregion
    }
}