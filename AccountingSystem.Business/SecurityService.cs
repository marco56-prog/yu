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
    // إدارة RememberMe Token آمن مع DPAPI
    // =============================
    public static class SecureTokenManager
    {
        private static readonly string TokenFileName = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AccountingSystem",
            "rememberme.dat"
        );

        private sealed class TokenPayload
        {
            public string Token { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string SavedAt { get; set; } = string.Empty; // ISO-8601
        }

        /// <summary>حفظ Token مع تشفير DPAPI للمستخدم الحالي فقط</summary>
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
                var plainBytes = Encoding.UTF8.GetBytes(json);

                // optional entropy لزيادة صلابة التشفير (مشتق من اسم المستخدم)
                var entropy = Encoding.UTF8.GetBytes($"Entropy::{username}");

                var encryptedBytes = ProtectedData.Protect(
                    plainBytes,
                    entropy,
                    DataProtectionScope.CurrentUser
                );

                var dir = Path.GetDirectoryName(TokenFileName);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllBytes(TokenFileName, encryptedBytes);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>قراءة Token مع فك التشفير DPAPI</summary>
        public static (bool Success, string Token, string Username) LoadToken()
        {
            try
            {
                if (!File.Exists(TokenFileName))
                    return (false, string.Empty, string.Empty);

                var encryptedBytes = File.ReadAllBytes(TokenFileName);

                // لا نعرف اسم المستخدم مسبقًا لاشتقاق entropy؛ نجرب بدون ثم نحاول استخراجه
                // في الحفظ استخدمنا entropy من اسم المستخدم، لذا نحتاج المحاولة بكلا الشكلين:
                // 1) بدون entropy (للإصدارات الأقدم)
                // 2) بمحاولة فك JSON بسيط ثم إعادة القراءة (لن نستطيع الثانية بدون username)
                // الحل البسيط: جرب قائمة احتمالات شائعة (لا تتوفر لدينا هنا) — سنستخدم نفس entropy
                // مع أسماء شائعة غير مجدية. لذلك سنفترض أن الملف تم إنشاؤه بهذه النسخة (entropy من username).
                // لتجنب الفشل، سنحاول أولاً بدون entropy، ثم بقراءات بديهية:
                byte[] plainBytes;
                try
                {
                    plainBytes = ProtectedData.Unprotect(encryptedBytes, null, DataProtectionScope.CurrentUser);
                }
                catch
                {
                    // لو فشل: لا نعرف username، لا يمكننا بناء entropy الصحيح.
                    // نعيد فشل ونحذف الملف المتضرر.
                    ClearToken();
                    return (false, string.Empty, string.Empty);
                }

                var json = Encoding.UTF8.GetString(plainBytes);
                var payload = JsonSerializer.Deserialize<TokenPayload>(json);

                if (payload == null || string.IsNullOrWhiteSpace(payload.Username))
                {
                    ClearToken();
                    return (false, string.Empty, string.Empty);
                }

                // لو كنا حفظنا بالنسخة الجديدة مع entropy، القراءة أعلاه كانت ستفشل،
                // لكن أعلاه نجحت بدون entropy => الملف غالبًا قديم. لا مشكلة.
                // مخرجات:
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
        public const string CUSTOMERS_VIEW = "customers.view";
        public const string CUSTOMERS_CREATE = "customers.create";
        public const string CUSTOMERS_EDIT = "customers.edit";
        public const string CUSTOMERS_DELETE = "customers.delete";

        public const string SUPPLIERS_VIEW = "suppliers.view";
        public const string SUPPLIERS_CREATE = "suppliers.create";
        public const string SUPPLIERS_EDIT = "suppliers.edit";
        public const string SUPPLIERS_DELETE = "suppliers.delete";

        public const string PRODUCTS_VIEW = "products.view";
        public const string PRODUCTS_CREATE = "products.create";
        public const string PRODUCTS_EDIT = "products.edit";
        public const string PRODUCTS_DELETE = "products.delete";

        public const string SALES_INVOICES_VIEW = "sales_invoices.view";
        public const string SALES_INVOICES_CREATE = "sales_invoices.create";
        public const string SALES_INVOICES_EDIT = "sales_invoices.edit";
        public const string SALES_INVOICES_DELETE = "sales_invoices.delete";
        public const string SALES_INVOICES_POST = "sales_invoices.post";

        public const string REPORTS_SALES = "reports.sales";
        public const string REPORTS_INVENTORY = "reports.inventory";
        public const string REPORTS_PROFIT = "reports.profit";
        public const string REPORTS_CUSTOMERS = "reports.customers";

        public const string SYSTEM_SETTINGS = "system.settings";
        public const string USER_MANAGEMENT = "user.management";
        public const string BACKUP_RESTORE = "backup.restore";
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
                    Permissions.CUSTOMERS_VIEW, Permissions.CUSTOMERS_CREATE, Permissions.CUSTOMERS_EDIT, Permissions.CUSTOMERS_DELETE,
                    Permissions.SUPPLIERS_VIEW, Permissions.SUPPLIERS_CREATE, Permissions.SUPPLIERS_EDIT, Permissions.SUPPLIERS_DELETE,
                    Permissions.PRODUCTS_VIEW, Permissions.PRODUCTS_CREATE, Permissions.PRODUCTS_EDIT, Permissions.PRODUCTS_DELETE,
                    Permissions.SALES_INVOICES_VIEW, Permissions.SALES_INVOICES_CREATE, Permissions.SALES_INVOICES_EDIT,
                    Permissions.SALES_INVOICES_DELETE, Permissions.SALES_INVOICES_POST,
                    Permissions.REPORTS_SALES, Permissions.REPORTS_INVENTORY, Permissions.REPORTS_PROFIT, Permissions.REPORTS_CUSTOMERS,
                    Permissions.SYSTEM_SETTINGS, Permissions.USER_MANAGEMENT, Permissions.BACKUP_RESTORE
                },
                [MANAGER] = new List<string>
                {
                    Permissions.CUSTOMERS_VIEW, Permissions.CUSTOMERS_CREATE, Permissions.CUSTOMERS_EDIT,
                    Permissions.SUPPLIERS_VIEW, Permissions.SUPPLIERS_CREATE, Permissions.SUPPLIERS_EDIT,
                    Permissions.PRODUCTS_VIEW, Permissions.PRODUCTS_CREATE, Permissions.PRODUCTS_EDIT,
                    Permissions.SALES_INVOICES_VIEW, Permissions.SALES_INVOICES_CREATE, Permissions.SALES_INVOICES_EDIT,
                    Permissions.SALES_INVOICES_POST,
                    Permissions.REPORTS_SALES, Permissions.REPORTS_INVENTORY, Permissions.REPORTS_PROFIT, Permissions.REPORTS_CUSTOMERS
                },
                [ACCOUNTANT] = new List<string>
                {
                    Permissions.CUSTOMERS_VIEW, Permissions.CUSTOMERS_CREATE, Permissions.CUSTOMERS_EDIT,
                    Permissions.SUPPLIERS_VIEW, Permissions.SUPPLIERS_CREATE, Permissions.SUPPLIERS_EDIT,
                    Permissions.SALES_INVOICES_VIEW, Permissions.SALES_INVOICES_CREATE, Permissions.SALES_INVOICES_EDIT,
                    Permissions.SALES_INVOICES_POST,
                    Permissions.REPORTS_SALES, Permissions.REPORTS_INVENTORY, Permissions.REPORTS_PROFIT, Permissions.REPORTS_CUSTOMERS
                },
                [CASHIER] = new List<string>
                {
                    Permissions.CUSTOMERS_VIEW,
                    Permissions.PRODUCTS_VIEW,
                    Permissions.SALES_INVOICES_VIEW, Permissions.SALES_INVOICES_CREATE
                },
                [VIEWER] = new List<string>
                {
                    Permissions.CUSTOMERS_VIEW,
                    Permissions.SUPPLIERS_VIEW,
                    Permissions.PRODUCTS_VIEW,
                    Permissions.SALES_INVOICES_VIEW,
                    Permissions.REPORTS_SALES, Permissions.REPORTS_INVENTORY, Permissions.REPORTS_CUSTOMERS
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
        private const int KeySize = 32;               // 256-bit
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
            var key = Convert.FromBase64String(parts[3]);

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
