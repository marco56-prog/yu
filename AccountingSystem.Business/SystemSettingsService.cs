using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using AccountingSystem.Models;
using AccountingSystem.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountingSystem.Business
{
    public interface ISystemSettingsService
    {
        Task<string?> GetSettingValueAsync(string key);
        Task<T?> GetSettingValueAsync<T>(string key) where T : struct;
        Task<decimal> GetTaxRateAsync();
        Task<decimal> GetDiscountRateAsync();
        Task<string> GetCompanyNameAsync();
        Task<string> GetCompanyAddressAsync();
        Task<string> GetCompanyPhoneAsync();
        Task<string> GetCompanyEmailAsync();
        Task<string> GetCompanyWebsiteAsync();
        Task<string> GetCompanyLogoAsync();
        Task<bool> IsReceiptPrintEnabledAsync();
        Task<string> GetDefaultPrinterAsync();
        Task<string> GetThermalPrinterAsync();
        Task<string> GetPaperSizeAsync();
        Task<bool> GetAutoBackupEnabledAsync();
        Task<string> GetBackupPathAsync();
        Task<bool> GetEmailIntegrationEnabledAsync();
        Task<string> GetSMTPServerAsync();
        Task<int> GetSMTPPortAsync();
        Task<bool> GetRequireCustomerSelectionAsync();
        Task<bool> GetAllowNegativeStockAsync();
        Task<int> GetMinStockWarningLevelAsync();
        Task SetSettingAsync(string key, string value, string? description = null);
        Task<Dictionary<string, string>> GetAllSettingsAsync();
    }

    public class SystemSettingsService : ISystemSettingsService
    {
        private readonly IUnitOfWork _unitOfWork;

        // مفاتيح افتراضية أولية
        private readonly Dictionary<string, string> _defaultSettings = new()
        {
            { "TaxRate", "0.14" },
            { "DiscountRate", "0.00" },
            { "CompanyName", "شركة النظام المحاسبي" },
            { "CompanyAddress", "جمهورية مصر العربية" },
            { "CompanyPhone", "+20-000-000-0000" },
            { "ReceiptPrintEnabled", "true" },
            { "Currency", "جنيه مصري" },
            { "CurrencyName", "الجنيه المصري" },
            { "CurrencySymbol", "ج.م" },
            { "CurrencyFormat", "#,##0.00 'ج.م'" },
            { "DateFormat", "dd/MM/yyyy" },
            { "TimeFormat", "HH:mm:ss" }
        };

        public SystemSettingsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<string?> GetSettingValueAsync(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            var setting = (await _unitOfWork.Repository<SystemSettings>()
                .FindAsync(s => s.SettingKey == key)).FirstOrDefault();

            if (setting != null)
                return setting.SettingValue;

            // مش موجود: رجّع الافتراضي وسجّله في القاعدة
            if (_defaultSettings.TryGetValue(key, out var defaultValue))
            {
                await SetSettingAsync(key, defaultValue, $"قيمة افتراضية لـ {key}");
                return defaultValue;
            }

            return null;
        }

        public async Task<T?> GetSettingValueAsync<T>(string key) where T : struct
        {
            var value = await GetSettingValueAsync(key);
            if (string.IsNullOrWhiteSpace(value))
                return null;

            try
            {
                if (typeof(T) == typeof(decimal))
                    return (T)(object)decimal.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(double))
                    return (T)(object)double.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(int))
                    return (T)(object)int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                if (typeof(T) == typeof(bool))
                    return (T)(object)bool.Parse(value);

                // fallback عام
                return (T)Convert.ChangeType(value, typeof(T), CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
        }

        public async Task<decimal> GetTaxRateAsync()
        {
            var rate = await GetSettingValueAsync<decimal>("TaxRate");
            return rate ?? 0.14m;
        }

        public async Task<decimal> GetDiscountRateAsync()
        {
            var rate = await GetSettingValueAsync<decimal>("DiscountRate");
            return rate ?? 0.00m;
        }

        public async Task<string> GetCompanyNameAsync()
        {
            return await GetSettingValueAsync("CompanyName") ?? "شركة النظام المحاسبي";
        }

        public async Task<string> GetCompanyAddressAsync()
        {
            return await GetSettingValueAsync("CompanyAddress") ?? "المملكة العربية السعودية";
        }

        public async Task<string> GetCompanyPhoneAsync()
        {
            return await GetSettingValueAsync("CompanyPhone") ?? "+966123456789";
        }

        public async Task<string> GetCompanyEmailAsync()
        {
            return await GetSettingValueAsync("CompanyEmail") ?? "info@company.com";
        }

        public async Task<string> GetCompanyWebsiteAsync()
        {
            return await GetSettingValueAsync("CompanyWebsite") ?? "www.company.com";
        }

        public async Task<string> GetCompanyLogoAsync()
        {
            return await GetSettingValueAsync("CompanyLogo") ?? "";
        }

        public async Task<bool> IsReceiptPrintEnabledAsync()
        {
            var enabled = await GetSettingValueAsync<bool>("ReceiptPrintEnabled");
            return enabled ?? true;
        }

        public async Task<string> GetDefaultPrinterAsync()
        {
            return await GetSettingValueAsync("DefaultPrinter") ?? "";
        }

        public async Task<string> GetThermalPrinterAsync()
        {
            return await GetSettingValueAsync("ThermalPrinter") ?? "";
        }

        public async Task<string> GetPaperSizeAsync()
        {
            return await GetSettingValueAsync("PaperSize") ?? "A4";
        }

        public async Task<bool> GetAutoBackupEnabledAsync()
        {
            var enabled = await GetSettingValueAsync<bool>("AutoBackupEnabled");
            return enabled ?? false;
        }

        public async Task<string> GetBackupPathAsync()
        {
            return await GetSettingValueAsync("BackupPath") ?? System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments), "AccountingBackups");
        }

        public async Task<bool> GetEmailIntegrationEnabledAsync()
        {
            var enabled = await GetSettingValueAsync<bool>("EmailIntegrationEnabled");
            return enabled ?? false;
        }

        public async Task<string> GetSMTPServerAsync()
        {
            return await GetSettingValueAsync("SMTPServer") ?? "smtp.gmail.com";
        }

        public async Task<int> GetSMTPPortAsync()
        {
            var port = await GetSettingValueAsync<int>("SMTPPort");
            return port ?? 587;
        }

        public async Task<bool> GetRequireCustomerSelectionAsync()
        {
            var required = await GetSettingValueAsync<bool>("RequireCustomerSelection");
            return required ?? false;
        }

        public async Task<bool> GetAllowNegativeStockAsync()
        {
            var allowed = await GetSettingValueAsync<bool>("AllowNegativeStock");
            return allowed ?? false;
        }

        public async Task<int> GetMinStockWarningLevelAsync()
        {
            var level = await GetSettingValueAsync<int>("MinStockWarningLevel");
            return level ?? 10;
        }

        public async Task SetSettingAsync(string key, string value, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("مفتاح الإعداد لا يمكن أن يكون فارغاً", nameof(key));

            var repo = _unitOfWork.Repository<SystemSettings>();
            var existing = (await repo.FindAsync(s => s.SettingKey == key)).FirstOrDefault();

            if (existing != null)
            {
                // تحديث الكائن المتتبع بالفعل - لا نحتاج لاستدعاء Update لأنه متتبع
                existing.SettingValue = value ?? string.Empty;
                existing.Description  = description;
                existing.UpdatedDate  = DateTime.Now;
                // لا نستدعي repo.Update لأن الكائن متتبع بالفعل من FindAsync
            }
            else
            {
                var s = new SystemSettings
                {
                    SettingKey   = key,
                    SettingValue = value ?? string.Empty,
                    Description  = description,
                    UpdatedDate  = DateTime.Now
                };
                await repo.AddAsync(s);
            }

            // مهم: احفظ التغييرات
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<Dictionary<string, string>> GetAllSettingsAsync()
        {
            var repo = _unitOfWork.Repository<SystemSettings>();
            var existing = await repo.GetAllAsync();

            var result = existing.ToDictionary(s => s.SettingKey, s => s.SettingValue);

            // كمّل أي مفاتيح ناقصة من الافتراضي، وسجّلها بعملية حفظ واحدة
            bool hasNew = false;
            foreach (var kv in _defaultSettings)
            {
                if (!result.ContainsKey(kv.Key))
                {
                    result[kv.Key] = kv.Value;
                    await repo.AddAsync(new SystemSettings
                    {
                        SettingKey   = kv.Key,
                        SettingValue = kv.Value,
                        Description  = $"قيمة افتراضية لـ {kv.Key}",
                        UpdatedDate  = DateTime.Now
                    });
                    hasNew = true;
                }
            }

            if (hasNew)
                await _unitOfWork.SaveChangesAsync();

            return result;
        }
    }
}
