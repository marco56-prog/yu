using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Text.Json;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AccountingSystem.Data;
using AccountingSystem.Models;

namespace AccountingSystem.Business
{
    // =============================
    // إدارة RememberMe Token آمن باستخدام AES (متوافق مع مختلف المنصات)
    // =============================
    public static class SecureTokenManager
    {
        private static readonly string TokenFileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AccountingSystem",
            "rememberme.dat"
        );

        // IMPORTANT: In a real-world application, this key should be stored securely
        // (e.g., using Azure Key Vault, AWS KMS, or other secret management tools)
        // and not hardcoded. For this exercise, we use a hardcoded key to ensure
        // cross-platform compatibility without external dependencies.
        private static readonly byte[] MasterKey = Encoding.UTF8.GetBytes("DefaultMasterKeyForAesEncryption!");
        private static readonly byte[] Salt = Encoding.UTF8.GetBytes("SaltyBytesGoHere");
        private const int KeySize = 256;
        private const int Iterations = 100_000;

        private sealed class TokenPayload
        {
            public string Token { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string SavedAt { get; set; } = string.Empty; // ISO-8601
        }

        private static string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            using var keyDerivation = new Rfc2898DeriveBytes(MasterKey, Salt, Iterations, HashAlgorithmName.SHA256);

            aes.Key = keyDerivation.GetBytes(KeySize / 8);
            aes.GenerateIV(); // Generate a new IV for each encryption

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();

            // Prepend IV to the stream
            ms.Write(aes.IV, 0, aes.IV.Length);

            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                using var sw = new StreamWriter(cs);
                sw.Write(plainText);
            }

            return Convert.ToBase64String(ms.ToArray());
        }

        private static string Decrypt(string cipherText)
        {
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            var iv = new byte[aes.BlockSize / 8];
            var cipher = new byte[fullCipher.Length - iv.Length];

            // Extract IV from the beginning of the cipher text
            Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
            Buffer.BlockCopy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            using var keyDerivation = new Rfc2898DeriveBytes(MasterKey, Salt, Iterations, HashAlgorithmName.SHA256);
            aes.Key = keyDerivation.GetBytes(KeySize / 8);
            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(cipher);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs);

            return sr.ReadToEnd();
        }

        /// <summary>حفظ Token مع تشفير AES-256 المتوافق مع مختلف المنصات</summary>
        public static bool SaveToken(string token, string username)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(username))
                    return false;

                var payload = new TokenPayload
                {
                    Token = token,
                    Username = username,
                    SavedAt = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
                };

                var json = JsonSerializer.Serialize(payload);
                var encryptedJson = Encrypt(json);

                var dir = Path.GetDirectoryName(TokenFileName);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(TokenFileName, encryptedJson, Encoding.UTF8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>قراءة Token مع فك تشفير AES-256</summary>
        public static (bool Success, string Token, string Username) LoadToken()
        {
            try
            {
                if (!File.Exists(TokenFileName))
                    return (false, string.Empty, string.Empty);

                var encryptedJson = File.ReadAllText(TokenFileName, Encoding.UTF8);
                if(string.IsNullOrWhiteSpace(encryptedJson))
                    return (false, string.Empty, string.Empty);

                var json = Decrypt(encryptedJson);
                var payload = JsonSerializer.Deserialize<TokenPayload>(json);

                if (payload == null || string.IsNullOrWhiteSpace(payload.Username))
                {
                    ClearToken();
                    return (false, string.Empty, string.Empty);
                }

                return (true, payload.Token ?? string.Empty, payload.Username ?? string.Empty);
            }
            catch
            {
                ClearToken();
                return (false, string.Empty, string.Empty);
            }
        }

        /// <summary>حذف Token المحفوظ</summary>
        public static bool ClearToken()
        {
            try
            {
                if (File.Exists(TokenFileName))
                    File.Delete(TokenFileName);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    // =============================
    // نماذج نظام الأمان
    // =============================
    public class LoginRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool RememberMe { get; set; }
    }

    public class LoginResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;
        public User? User { get; set; }
        public string? Token { get; set; }
    }

    public class ChangePasswordRequest
    {
        public int UserId { get; set; }
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    // =============================
    // مستويات الصلاحيات والأدوار
    // =============================
    public static class Permissions
    {
        public const string CustomersView = "customers.view";
        public const string CustomersCreate = "customers.create";
        public const string CustomersEdit = "customers.edit";
        public const string CustomersDelete = "customers.delete";

        public const string SuppliersView = "suppliers.view";
        public const string SuppliersCreate = "suppliers.create";
        public const string SuppliersEdit = "suppliers.edit";
        public const string SuppliersDelete = "suppliers.delete";

        public const string ProductsView = "products.view";
        public const string ProductsCreate = "products.create";
        public const string ProductsEdit = "products.edit";
        public const string ProductsDelete = "products.delete";

        public const string SalesInvoicesView = "sales.invoices.view";
        public const string SalesInvoicesCreate = "sales.invoices.create";
        public const string SalesInvoicesEdit = "sales.invoices.edit";
        public const string SalesInvoicesDelete = "sales.invoices.delete";
        public const string SalesInvoicesPost = "sales.invoices.post";

        public const string ReportsSales = "reports.sales";
        public const string ReportsInventory = "reports.inventory";
        public const string ReportsProfit = "reports.profit";
        public const string ReportsCustomers = "reports.customers";

        public const string SystemSettings = "system.settings";
        public const string UserManagement = "user.management";
        public const string BackupRestore = "backup.restore";
    }

    public static class Roles
    {
        public const string ADMIN = "admin";
        public const string MANAGER = "manager";
        public const string ACCOUNTANT = "accountant";
        public const string CASHIER = "cashier";
        public const string VIEWER = "viewer";

        public static readonly Dictionary<string, List<string>> RolePermissions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                [ADMIN] = new List<string>
                {
                    Permissions.CustomersView, Permissions.CustomersCreate, Permissions.CustomersEdit, Permissions.CustomersDelete,
                    Permissions.SuppliersView, Permissions.SuppliersCreate, Permissions.SuppliersEdit, Permissions.SuppliersDelete,
                    Permissions.ProductsView, Permissions.ProductsCreate, Permissions.ProductsEdit, Permissions.ProductsDelete,
                    Permissions.SalesInvoicesView, Permissions.SalesInvoicesCreate, Permissions.SalesInvoicesEdit,
                    Permissions.SalesInvoicesDelete, Permissions.SalesInvoicesPost,
                    Permissions.ReportsSales, Permissions.ReportsInventory, Permissions.ReportsProfit, Permissions.ReportsCustomers,
                    Permissions.SystemSettings, Permissions.UserManagement, Permissions.BackupRestore
                },
                [MANAGER] = new List<string>
                {
                    Permissions.CustomersView, Permissions.CustomersCreate, Permissions.CustomersEdit,
                    Permissions.SuppliersView, Permissions.SuppliersCreate, Permissions.SuppliersEdit,
                    Permissions.ProductsView, Permissions.ProductsCreate, Permissions.ProductsEdit,
                    Permissions.SalesInvoicesView, Permissions.SalesInvoicesCreate, Permissions.SalesInvoicesEdit,
                    Permissions.SalesInvoicesPost,
                    Permissions.ReportsSales, Permissions.ReportsInventory, Permissions.ReportsProfit, Permissions.ReportsCustomers
                },
                [ACCOUNTANT] = new List<string>
                {
                    Permissions.CustomersView, Permissions.CustomersCreate, Permissions.CustomersEdit,
                    Permissions.SuppliersView, Permissions.SuppliersCreate, Permissions.SuppliersEdit,
                    Permissions.SalesInvoicesView, Permissions.SalesInvoicesCreate, Permissions.SalesInvoicesEdit,
                    Permissions.SalesInvoicesPost,
                    Permissions.ReportsSales, Permissions.ReportsInventory, Permissions.ReportsProfit, Permissions.ReportsCustomers
                },
                [CASHIER] = new List<string>
                {
                    Permissions.CustomersView,
                    Permissions.ProductsView,
                    Permissions.SalesInvoicesView, Permissions.SalesInvoicesCreate
                },
                [VIEWER] = new List<string>
                {
                    Permissions.CustomersView,
                    Permissions.SuppliersView,
                    Permissions.ProductsView,
                    Permissions.SalesInvoicesView,
                    Permissions.ReportsSales, Permissions.ReportsInventory, Permissions.ReportsCustomers
                }
            };
    }

    // =============================
    // واجهات الخدمات
    // =============================
    public interface ISecurityService
    {
        Task<LoginResult> LoginAsync(LoginRequest request);
        Task<bool> LogoutAsync(int userId);
        Task<bool> ChangePasswordAsync(ChangePasswordRequest request);
        Task<User?> GetCurrentUserAsync();
        Task<bool> HasPermissionAsync(string permission);
        Task<bool> HasPermissionAsync(int userId, string permission);
        Task<List<string>> GetUserPermissionsAsync(int userId);

        string HashPassword(string password);
        bool VerifyPassword(string password, string hashedPassword);

        // RememberMe
        Task<LoginResult> LoginWithTokenAsync();
        Task ClearSavedLoginAsync();
    }

    public interface IUserService
    {
        Task<IEnumerable<User>> GetAllUsersAsync();
        Task<User?> GetUserByIdAsync(int id);
        Task<User?> GetUserByUsernameAsync(string username);
        Task<User> CreateUserAsync(User user, string password);
        Task<User> UpdateUserAsync(User user);
        Task<bool> DeleteUserAsync(int id);
        Task<bool> ActivateUserAsync(int id);
        Task<bool> DeactivateUserAsync(int id);
        Task<bool> AssignRoleAsync(int userId, string role);
        Task<(bool IsSuccess, User? User)> ValidateLoginAsync(string username, string password);
    }

    // =============================
    // Legacy hasher (SHA256 + Salt123) للكشف/الدعم المؤقت
    // =============================
    internal static class LegacyPasswordHasher
    {
        public static bool IsLegacyHash(string hashedPassword)
            => !string.IsNullOrEmpty(hashedPassword) &&
               !hashedPassword.StartsWith("PBKDF2:", StringComparison.Ordinal);

        public static bool VerifyLegacyPassword(string password, string legacyHash)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(legacyHash))
                return false;

            try
            {
                var salted = password + "Salt123";
                var hash = SHA256.HashData(Encoding.UTF8.GetBytes(salted));
                var base64 = Convert.ToBase64String(hash);
                return string.Equals(base64, legacyHash, StringComparison.Ordinal);
            }
            catch
            {
                return false;
            }
        }
    }

    internal static class PasswordHasherPBKDF2
    {
        private const int SaltSize = 16;               // 128-bit
        private const int KeySize  = 32;               // 256-bit
        private const int DefaultIterations = 120_000; // عدّل حسب الأداء
        private const string Label = "PBKDF2";

        public static string Hash(string password, int? iterations = null)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password must not be empty.", nameof(password));

            int iters = iterations ?? DefaultIterations;

            using var rng = RandomNumberGenerator.Create();
            var salt = new byte[SaltSize];
            rng.GetBytes(salt);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iters, HashAlgorithmName.SHA256);
            var key = pbkdf2.GetBytes(KeySize);

            return $"{Label}:{iters}:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(key)}";
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrWhiteSpace(stored)) return false;

            var parts = stored.Split(':');
            if (parts.Length != 4 || !string.Equals(parts[0], Label, StringComparison.Ordinal))
                return false;

            if (!int.TryParse(parts[1], out int iters) || iters <= 0) return false;

            var salt = Convert.FromBase64String(parts[2]);
            var key  = Convert.FromBase64String(parts[3]);

            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iters, HashAlgorithmName.SHA256);
            var check = pbkdf2.GetBytes(key.Length);

            return CryptographicOperations.FixedTimeEquals(key, check);
        }
    }

    // =============================
    // تنفيذ خدمة الأمان
    // =============================
    public class SecurityService : ISecurityService
    {
        private readonly AccountingDbContext _context;
        private User? _currentUser;

        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutWindow = TimeSpan.FromMinutes(15);

        public SecurityService(AccountingDbContext context)
        {
            _context = context;
        }

        public async Task<LoginResult> LoginAsync(LoginRequest request)
        {
            try
            {
                var username = request.UserName?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(username))
                    return new LoginResult { IsSuccess = false, Message = "يرجى إدخال اسم المستخدم" };

                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username && u.IsActive);
                if (user == null)
                    return new LoginResult { IsSuccess = false, Message = "اسم المستخدم غير موجود أو غير مفعل" };

                // قفل مؤقت
                if (user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTime.UtcNow)
                {
                    var mins = (int)Math.Ceiling((user.LockoutEnd.Value - DateTime.UtcNow).TotalMinutes);
                    return new LoginResult { IsSuccess = false, Message = $"تم إيقاف الحساب مؤقتًا. حاول بعد {mins} دقيقة." };
                }

                bool isPasswordCorrect;
                bool needsUpgrade = false;

                if (LegacyPasswordHasher.IsLegacyHash(user.PasswordHash))
                {
                    isPasswordCorrect = LegacyPasswordHasher.VerifyLegacyPassword(request.Password, user.PasswordHash);
                    needsUpgrade = isPasswordCorrect;
                }
                else
                {
                    isPasswordCorrect = VerifyPassword(request.Password, user.PasswordHash);
                }

                if (!isPasswordCorrect)
                {
                    await RegisterFailAsync(user);
                    return new LoginResult { IsSuccess = false, Message = "كلمة المرور غير صحيحة" };
                }

                // نجاح
                if (user.FailedAccessCount > 0)
                {
                    user.FailedAccessCount = 0;
                    user.LockoutEnd = null;
                }

                if (needsUpgrade)
                    user.PasswordHash = HashPassword(request.Password);

                user.LastLoginDate = DateTime.Now;
                await _context.SaveChangesAsync();

                _currentUser = user;

                string? token = null;
                if (request.RememberMe)
                {
                    token = GenerateSessionToken();
                    SecureTokenManager.SaveToken(token, username);
                }

                return new LoginResult
                {
                    IsSuccess = true,
                    Message = "تم تسجيل الدخول بنجاح",
                    User = user,
                    Token = token
                };
            }
            catch (Exception ex)
            {
                return new LoginResult { IsSuccess = false, Message = $"حدث خطأ أثناء تسجيل الدخول: {ex.Message}" };
            }
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            _currentUser = null;
            SecureTokenManager.ClearToken();
            return await Task.FromResult(true);
        }

        public async Task<LoginResult> LoginWithTokenAsync()
        {
            try
            {
                var (success, token, username) = SecureTokenManager.LoadToken();
                if (!success || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(username))
                    return new LoginResult { IsSuccess = false, Message = "لا توجد بيانات محفوظة" };

                var user = await _context.Users.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.UserName == username && u.IsActive);

                if (user == null)
                {
                    SecureTokenManager.ClearToken();
                    return new LoginResult { IsSuccess = false, Message = "المستخدم غير موجود أو غير مفعل" };
                }

                _currentUser = user;
                return new LoginResult
                {
                    IsSuccess = true,
                    Message = "تم تسجيل الدخول تلقائياً",
                    User = user,
                    Token = token
                };
            }
            catch (Exception ex)
            {
                return new LoginResult { IsSuccess = false, Message = $"خطأ في التسجيل التلقائي: {ex.Message}" };
            }
        }

        public async Task ClearSavedLoginAsync()
        {
            SecureTokenManager.ClearToken();
            await Task.CompletedTask;
        }

        public async Task<bool> ChangePasswordAsync(ChangePasswordRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
                return false;
            if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
                return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == request.UserId);
            if (user == null) return false;

            // تحقق باستخدام PBKDF2 أو legacy تلقائيًا
            var ok = LegacyPasswordHasher.IsLegacyHash(user.PasswordHash)
                ? LegacyPasswordHasher.VerifyLegacyPassword(request.CurrentPassword, user.PasswordHash)
                : VerifyPassword(request.CurrentPassword, user.PasswordHash);

            if (!ok) return false;

            user.PasswordHash = HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<User?> GetCurrentUserAsync() => await Task.FromResult(_currentUser);

        public async Task<bool> HasPermissionAsync(string permission)
        {
            if (_currentUser == null) return false;
            return await HasPermissionAsync(_currentUser.UserId, permission);
        }

        public async Task<bool> HasPermissionAsync(int userId, string permission)
        {
            if (string.IsNullOrWhiteSpace(permission)) return false;

            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || !user.IsActive) return false;

            // امتياز كامل لو كان الدور Admin أو اسم المستخدم admin
            if (string.Equals(user.Role, Roles.ADMIN, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(user.UserName, Roles.ADMIN, StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrEmpty(user.Role) &&
                Roles.RolePermissions.TryGetValue(user.Role, out var perms))
            {
                return perms.Any(p => string.Equals(p, permission, StringComparison.OrdinalIgnoreCase));
            }

            return false;
        }

        public async Task<List<string>> GetUserPermissionsAsync(int userId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null || !user.IsActive) return new List<string>();

            if (!string.IsNullOrEmpty(user.Role) &&
                Roles.RolePermissions.TryGetValue(user.Role, out var perms))
            {
                return perms.ToList();
            }

            return new List<string>();
        }

        public string HashPassword(string password) => PasswordHasherPBKDF2.Hash(password);
        public bool VerifyPassword(string password, string hashedPassword) => PasswordHasherPBKDF2.Verify(password, hashedPassword);

        private static string GenerateSessionToken()
        {
            Span<byte> buf = stackalloc byte[32];
            RandomNumberGenerator.Fill(buf);
            return Convert.ToBase64String(buf);
        }

        private async Task RegisterFailAsync(User user)
        {
            user.FailedAccessCount++;
            if (user.FailedAccessCount >= MaxFailedAttempts)
            {
                user.LockoutEnd = DateTime.UtcNow.Add(LockoutWindow);
                user.FailedAccessCount = 0;
            }
            await _context.SaveChangesAsync();
        }
    }

    // =============================
    // تنفيذ خدمة المستخدمين
    // =============================
    public class UserService : IUserService
    {
        private readonly AccountingDbContext _context;
        private readonly ISecurityService _securityService;

        public UserService(AccountingDbContext context, ISecurityService securityService)
        {
            _context = context;
            _securityService = securityService;
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .OrderBy(u => u.FullName)
                .ToListAsync();
        }

        public async Task<User?> GetUserByIdAsync(int id)
            => await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);

        public async Task<User?> GetUserByUsernameAsync(string username)
            => await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserName == username);

        public async Task<User> CreateUserAsync(User user, string password)
        {
            if (string.IsNullOrWhiteSpace(user.UserName))
                throw new ArgumentException("اسم المستخدم مطلوب.", nameof(user));

            bool exists = await _context.Users.AnyAsync(u => u.UserName == user.UserName);
            if (exists)
                throw new InvalidOperationException("اسم المستخدم موجود بالفعل.");

            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                throw new ArgumentException("كلمة المرور يجب أن تكون 6 أحرف على الأقل.");

            user.PasswordHash = _securityService.HashPassword(password);
            user.CreatedDate = DateTime.Now;
            user.IsActive = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<User> UpdateUserAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return false;

            if (string.Equals(user.UserName, Roles.ADMIN, StringComparison.OrdinalIgnoreCase))
                return false;

            user.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ActivateUserAsync(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return false;

            user.IsActive = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateUserAsync(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id);
            if (user == null) return false;

            if (string.Equals(user.UserName, Roles.ADMIN, StringComparison.OrdinalIgnoreCase))
                return false;

            user.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AssignRoleAsync(int userId, string role)
        {
            if (string.IsNullOrWhiteSpace(role)) return false;
            if (!Roles.RolePermissions.ContainsKey(role)) return false;

            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) return false;

            user.Role = role;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>التحقق من صحة تسجيل الدخول للاختبارات</summary>
        public async Task<(bool IsSuccess, User? User)> ValidateLoginAsync(string username, string password)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.UserName == username && u.IsActive);
                if (user == null) return (false, null);

                bool ok = LegacyPasswordHasher.IsLegacyHash(user.PasswordHash)
                    ? LegacyPasswordHasher.VerifyLegacyPassword(password, user.PasswordHash)
                    : PasswordHasherPBKDF2.Verify(password, user.PasswordHash); // ← إصلاح مهم

                return (ok, ok ? user : null);
            }
            catch
            {
                return (false, null);
            }
        }
    }
}
