using System;
using System.Collections.Generic;
using System.Linq;

namespace AccountingSystem.Business.Helpers
{
    /// <summary>
    /// مساعدات التحقق من صحة البيانات المحسنة
    /// Enhanced validation helpers
    /// </summary>
    public static class ValidationHelpers
    {
        /// <summary>
        /// التحقق من أن القيمة ليست null
        /// </summary>
        public static T EnsureNotNull<T>(T value, string paramName) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(paramName, $"القيمة {paramName} لا يمكن أن تكون null");
            return value;
        }

        /// <summary>
        /// التحقق من أن النص ليس فارغاً
        /// </summary>
        public static string EnsureNotNullOrEmpty(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException($"النص {paramName} لا يمكن أن يكون فارغاً", paramName);
            return value;
        }

        /// <summary>
        /// التحقق من أن النص ليس فارغاً أو مسافات بيضاء فقط
        /// </summary>
        public static string EnsureNotNullOrWhiteSpace(string value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"النص {paramName} لا يمكن أن يكون فارغاً أو مسافات فقط", paramName);
            return value;
        }

        /// <summary>
        /// التحقق من أن القيمة في النطاق المحدد
        /// </summary>
        public static T EnsureInRange<T>(T value, T min, T max, string paramName) where T : IComparable<T>
        {
            if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
                throw new ArgumentOutOfRangeException(paramName, value, 
                    $"القيمة {paramName} يجب أن تكون بين {min} و {max}");
            return value;
        }

        /// <summary>
        /// التحقق من أن القيمة أكبر من صفر
        /// </summary>
        public static decimal EnsurePositive(decimal value, string paramName)
        {
            if (value <= 0)
                throw new ArgumentException($"القيمة {paramName} يجب أن تكون أكبر من صفر", paramName);
            return value;
        }

        /// <summary>
        /// التحقق من أن القيمة أكبر من أو تساوي صفر
        /// </summary>
        public static decimal EnsureNonNegative(decimal value, string paramName)
        {
            if (value < 0)
                throw new ArgumentException($"القيمة {paramName} لا يمكن أن تكون سالبة", paramName);
            return value;
        }

        /// <summary>
        /// التحقق من أن المجموعة ليست null أو فارغة
        /// </summary>
        public static IEnumerable<T> EnsureNotNullOrEmpty<T>(IEnumerable<T> collection, string paramName)
        {
            if (collection == null)
                throw new ArgumentNullException(paramName, $"المجموعة {paramName} لا يمكن أن تكون null");
            
            if (!collection.Any())
                throw new ArgumentException($"المجموعة {paramName} لا يمكن أن تكون فارغة", paramName);
            
            return collection;
        }

        /// <summary>
        /// التحقق من أن التاريخ ليس في المستقبل
        /// </summary>
        public static DateTime EnsureNotFuture(DateTime date, string paramName)
        {
            if (date > DateTime.Now)
                throw new ArgumentException($"التاريخ {paramName} لا يمكن أن يكون في المستقبل", paramName);
            return date;
        }

        /// <summary>
        /// التحقق من أن التاريخ ليس في الماضي
        /// </summary>
        public static DateTime EnsureNotPast(DateTime date, string paramName)
        {
            if (date < DateTime.Now)
                throw new ArgumentException($"التاريخ {paramName} لا يمكن أن يكون في الماضي", paramName);
            return date;
        }

        /// <summary>
        /// التحقق من شرط معين
        /// </summary>
        public static void EnsureCondition(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        /// <summary>
        /// التحقق من صحة البريد الإلكتروني
        /// </summary>
        public static string EnsureValidEmail(string email, string paramName)
        {
            email = EnsureNotNullOrWhiteSpace(email, paramName);
            
            if (!ValidationService.IsValidEmail(email))
                throw new ArgumentException($"البريد الإلكتروني {email} غير صحيح", paramName);
            
            return email;
        }

        /// <summary>
        /// التحقق من صحة رقم الهاتف
        /// </summary>
        public static string EnsureValidPhone(string phone, string paramName)
        {
            phone = EnsureNotNullOrWhiteSpace(phone, paramName);
            
            if (!ValidationService.IsValidPhone(phone))
                throw new ArgumentException($"رقم الهاتف {phone} غير صحيح", paramName);
            
            return phone;
        }

        /// <summary>
        /// التحقق من طول النص
        /// </summary>
        public static string? EnsureMaxLength(string? value, int maxLength, string paramName)
        {
            if (value != null && value.Length > maxLength)
                throw new ArgumentException($"طول {paramName} يجب ألا يتجاوز {maxLength} حرف", paramName);
            
            return value;
        }

        /// <summary>
        /// التحقق من الحد الأدنى لطول النص
        /// </summary>
        public static string? EnsureMinLength(string? value, int minLength, string paramName)
        {
            if (value != null && value.Length < minLength)
                throw new ArgumentException($"طول {paramName} يجب أن يكون على الأقل {minLength} حرف", paramName);
            
            return value;
        }

        /// <summary>
        /// محاولة التحويل إلى نوع معين مع معالجة الأخطاء
        /// </summary>
        public static bool TryConvert<T>(object value, out T result)
        {
            result = default!;
            try
            {
                if (value == null)
                    return false;

                result = (T)Convert.ChangeType(value, typeof(T));
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// التحقق من أن القيمة من نوع معين
        /// </summary>
        public static T EnsureType<T>(object value, string paramName) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(paramName);

            if (!(value is T typedValue))
                throw new ArgumentException($"القيمة {paramName} يجب أن تكون من نوع {typeof(T).Name}", paramName);

            return typedValue;
        }

        /// <summary>
        /// دمج أخطاء متعددة في رسالة واحدة
        /// </summary>
        public static string CombineErrors(IEnumerable<string> errors)
        {
            if (errors == null || !errors.Any())
                return string.Empty;

            return string.Join(Environment.NewLine, errors.Select((e, i) => $"{i + 1}. {e}"));
        }

        /// <summary>
        /// التحقق من صحة معرف الكيان
        /// </summary>
        public static int EnsureValidId(int id, string paramName)
        {
            if (id <= 0)
                throw new ArgumentException($"المعرف {paramName} يجب أن يكون أكبر من صفر", paramName);
            return id;
        }

        /// <summary>
        /// التحقق الآمن من null مع قيمة افتراضية
        /// </summary>
        public static T GetOrDefault<T>(T? value, T defaultValue) where T : class
        {
            return value ?? defaultValue;
        }

        /// <summary>
        /// تنظيف النص وإزالة المسافات الزائدة
        /// </summary>
        public static string? SanitizeString(string? value)
        {
            return value?.Trim();
        }

        /// <summary>
        /// تنظيف النص وضمان عدم null
        /// </summary>
        public static string SanitizeStringNotNull(string? value)
        {
            return value?.Trim() ?? string.Empty;
        }
    }
}
