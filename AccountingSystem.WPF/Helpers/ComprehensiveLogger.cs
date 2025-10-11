using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using AccountingSystem.Business;
using AccountingSystem.Models;
using Serilog;

namespace AccountingSystem.WPF.Helpers
{
    /// <summary>
    /// نظام تسجيل شامل لكافة عمليات التطبيق
    /// </summary>
    public static class ComprehensiveLogger
    {
        /// <summary>
        /// تسجيل عملية محاسبية
        /// </summary>
        public static void LogBusinessOperation(string operation, string details,
            int? userId = null, string? username = null, bool isSuccess = true,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                var logContext = Log.ForContext("BusinessOperation", true)
                                   .ForContext("Operation", operation)
                                   .ForContext("UserId", userId)
                                   .ForContext("Username", username)
                                   .ForContext("IsSuccess", isSuccess)
                                   .ForContext("SourceFile", fileName)
                                   .ForContext("SourceMethod", caller)
                                   .ForContext("LineNumber", lineNumber);

                if (isSuccess)
                {
                    logContext.Information("✅ عملية محاسبية: {Operation} | المستخدم: {Username} | {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           operation, username, details, fileName, caller, lineNumber);
                }
                else
                {
                    logContext.Warning("⚠️ فشل عملية محاسبية: {Operation} | المستخدم: {Username} | {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           operation, username, details, fileName, caller, lineNumber);
                }

                // تسجيل في خدمة قاعدة البيانات أيضاً
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var serviceProvider = App.ServiceProvider;
                        using var scope = serviceProvider?.CreateScope();
                        var errorLoggingService = scope?.ServiceProvider.GetService<IErrorLoggingService>();

                        if (errorLoggingService != null && !isSuccess)
                        {
                            await errorLoggingService.LogErrorAsync($"فشل في العملية المحاسبية: {operation}",
                                details, ErrorType.BusinessLogicError, ErrorSeverity.Warning, userId, username);
                        }
                    }
                    catch { /* تجاهل أخطاء التسجيل في قاعدة البيانات */ }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"فشل في تسجيل العملية المحاسبية: {ex.Message}");
            }
        }

        /// <summary>
        /// تسجيل عملية قاعدة بيانات
        /// </summary>
        public static void LogDatabaseOperation(string operation, string tableName,
            string? details = null, int? recordId = null, bool isSuccess = true,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                var logContext = Log.ForContext("DatabaseOperation", true)
                                   .ForContext("Operation", operation)
                                   .ForContext("TableName", tableName)
                                   .ForContext("RecordId", recordId)
                                   .ForContext("IsSuccess", isSuccess)
                                   .ForContext("SourceFile", fileName)
                                   .ForContext("SourceMethod", caller)
                                   .ForContext("LineNumber", lineNumber);

                if (isSuccess)
                {
                    logContext.Information("🗄️ قاعدة بيانات: {Operation} على {TableName} | سجل: {RecordId} | {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           operation, tableName, recordId, details, fileName, caller, lineNumber);
                }
                else
                {
                    logContext.Error("❌ فشل قاعدة بيانات: {Operation} على {TableName} | سجل: {RecordId} | {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           operation, tableName, recordId, details, fileName, caller, lineNumber);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"فشل في تسجيل عملية قاعدة البيانات: {ex.Message}");
            }
        }

        /// <summary>
        /// تسجيل عملية مستخدم
        /// </summary>
        public static void LogUserOperation(string operation, string username,
            int? userId = null, string? details = null, bool isSuccess = true,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                var logContext = Log.ForContext("UserOperation", true)
                                   .ForContext("Operation", operation)
                                   .ForContext("Username", username)
                                   .ForContext("UserId", userId)
                                   .ForContext("IsSuccess", isSuccess)
                                   .ForContext("SourceFile", fileName)
                                   .ForContext("SourceMethod", caller)
                                   .ForContext("LineNumber", lineNumber);

                if (isSuccess)
                {
                    logContext.Information("👤 عملية مستخدم: {Operation} | المستخدم: {Username} | {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           operation, username, details, fileName, caller, lineNumber);
                }
                else
                {
                    logContext.Warning("⚠️ فشل عملية مستخدم: {Operation} | المستخدم: {Username} | {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           operation, username, details, fileName, caller, lineNumber);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"فشل في تسجيل عملية المستخدم: {ex.Message}");
            }
        }

        /// <summary>
        /// تسجيل عملية أمان
        /// </summary>
        public static void LogSecurityOperation(string operation, string? username = null,
            string? ipAddress = null, string? details = null, bool isSuccess = true,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                var logContext = Log.ForContext("SecurityOperation", true)
                                   .ForContext("Operation", operation)
                                   .ForContext("Username", username)
                                   .ForContext("IpAddress", ipAddress)
                                   .ForContext("IsSuccess", isSuccess)
                                   .ForContext("SourceFile", fileName)
                                   .ForContext("SourceMethod", caller)
                                   .ForContext("LineNumber", lineNumber);

                if (isSuccess)
                {
                    logContext.Information("🔒 عملية أمان: {Operation} | المستخدم: {Username} | IP: {IpAddress} | {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           operation, username, ipAddress, details, fileName, caller, lineNumber);
                }
                else
                {
                    logContext.Warning("🚨 محاولة أمان فاشلة: {Operation} | المستخدم: {Username} | IP: {IpAddress} | {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           operation, username, ipAddress, details, fileName, caller, lineNumber);
                }

                // تسجيل أحداث الأمان المهمة في قاعدة البيانات
                if (!isSuccess || operation.Contains("Login") || operation.Contains("Permission"))
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var serviceProvider = App.ServiceProvider;
                            using var scope = serviceProvider?.CreateScope();
                            var errorLoggingService = scope?.ServiceProvider.GetService<IErrorLoggingService>();

                            if (errorLoggingService != null)
                            {
                                await errorLoggingService.LogSecurityErrorAsync(
                                    new SecurityException($"حدث أمان: {operation} - {details}"),
                                    null, username);
                            }
                        }
                        catch { /* تجاهل أخطاء التسجيل في قاعدة البيانات */ }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"فشل في تسجيل عملية الأمان: {ex.Message}");
            }
        }

        /// <summary>
        /// تسجيل عملية نافذة/واجهة مستخدم
        /// </summary>
        public static void LogUIOperation(string operation, string windowName,
            string? details = null, bool isSuccess = true,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                var logContext = Log.ForContext("UIOperation", true)
                                   .ForContext("Operation", operation)
                                   .ForContext("WindowName", windowName)
                                   .ForContext("IsSuccess", isSuccess)
                                   .ForContext("SourceFile", fileName)
                                   .ForContext("SourceMethod", caller)
                                   .ForContext("LineNumber", lineNumber);

                if (isSuccess)
                {
                    logContext.Debug("🖼️ واجهة المستخدم: {Operation} | النافذة: {WindowName} | {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           operation, windowName, details, fileName, caller, lineNumber);
                }
                else
                {
                    logContext.Warning("⚠️ مشكلة في واجهة المستخدم: {Operation} | النافذة: {WindowName} | {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           operation, windowName, details, fileName, caller, lineNumber);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"فشل في تسجيل عملية واجهة المستخدم: {ex.Message}");
            }
        }

        /// <summary>
        /// تسجيل استثناء بشكل شامل
        /// </summary>
        public static void LogException(Exception exception, string? context = null,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                Log.ForContext("SourceFile", fileName)
                   .ForContext("SourceMethod", caller)
                   .ForContext("LineNumber", lineNumber)
                   .Error(exception, "❌ استثناء: {Context} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                          context ?? "غير محدد", fileName, caller, lineNumber);

                // تسجيل في خدمة قاعدة البيانات أيضاً
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var serviceProvider = App.ServiceProvider;
                        using var scope = serviceProvider?.CreateScope();
                        var errorLoggingService = scope?.ServiceProvider.GetService<IErrorLoggingService>();

                        if (errorLoggingService != null)
                        {
                            await errorLoggingService.LogErrorAsync(exception,
                                ErrorType.SystemError, ErrorSeverity.Error);
                        }
                    }
                    catch { /* تجاهل أخطاء التسجيل في قاعدة البيانات */ }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"فشل في تسجيل الاستثناء: {ex.Message}");
            }
        }

        /// <summary>
        /// تسجيل خطأ عام
        /// </summary>
        public static void LogError(string message, Exception? exception = null, string? context = null,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                var logContext = Log.ForContext("SourceFile", fileName)
                                   .ForContext("SourceMethod", caller)
                                   .ForContext("LineNumber", lineNumber)
                                   .ForContext("Context", context ?? "غير محدد");

                if (exception != null)
                {
                    logContext.Error(exception, "❌ خطأ: {Message} | السياق: {Context} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           message, context, fileName, caller, lineNumber);
                }
                else
                {
                    logContext.Error("❌ خطأ: {Message} | السياق: {Context} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           message, context, fileName, caller, lineNumber);
                }

                // تسجيل في خدمة قاعدة البيانات أيضاً
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var serviceProvider = App.ServiceProvider;
                        using var scope = serviceProvider?.CreateScope();
                        var errorLoggingService = scope?.ServiceProvider.GetService<IErrorLoggingService>();

                        if (errorLoggingService != null)
                        {
                            if (exception != null)
                            {
                                await errorLoggingService.LogErrorAsync(exception,
                                    ErrorType.SystemError, ErrorSeverity.Error);
                            }
                            else
                            {
                                await errorLoggingService.LogErrorAsync(message,
                                    context, ErrorType.SystemError, ErrorSeverity.Error);
                            }
                        }
                    }
                    catch { /* تجاهل أخطاء التسجيل في قاعدة البيانات */ }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"فشل في تسجيل الخطأ: {ex.Message}");
            }
        }

        /// <summary>
        /// تسجيل معلومات عامة
        /// </summary>
        public static void LogInfo(string message, string? context = null,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                Log.ForContext("SourceFile", fileName)
                   .ForContext("SourceMethod", caller)
                   .ForContext("LineNumber", lineNumber)
                   .Information("ℹ️ معلومات: {Message} | السياق: {Context} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                          message, context ?? "عام", fileName, caller, lineNumber);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"فشل في تسجيل المعلومات: {ex.Message}");
            }
        }

        /// <summary>
        /// تسجيل عملية أداء
        /// </summary>
        public static void LogPerformanceOperation(string operation, string? context = null,
            object? metrics = null,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                Log.ForContext("PerformanceOperation", true)
                   .ForContext("Operation", operation)
                   .ForContext("Context", context)
                   .ForContext("Metrics", metrics)
                   .ForContext("SourceFile", fileName)
                   .ForContext("SourceMethod", caller)
                   .ForContext("LineNumber", lineNumber)
                   .Information("⚡ أداء: {Operation} | السياق: {Context} | المقاييس: {Metrics} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                          operation, context ?? "عام", metrics, fileName, caller, lineNumber);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"فشل في تسجيل عملية الأداء: {ex.Message}");
            }
        }

        /// <summary>
        /// تسجيل عملية تدقيق
        /// </summary>
        public static void LogAuditOperation(string action, string? context = null,
            string? username = null, string? details = null,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                Log.ForContext("AuditOperation", true)
                   .ForContext("Action", action)
                   .ForContext("Username", username)
                   .ForContext("Context", context)
                   .ForContext("SourceFile", fileName)
                   .ForContext("SourceMethod", caller)
                   .ForContext("LineNumber", lineNumber)
                   .Information("📋 تدقيق: {Action} | المستخدم: {Username} | السياق: {Context} | التفاصيل: {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                          action, username, context, details, fileName, caller, lineNumber);

                // تسجيل في خدمة قاعدة البيانات أيضاً
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var serviceProvider = App.ServiceProvider;
                        using var scope = serviceProvider?.CreateScope();
                        var errorLoggingService = scope?.ServiceProvider.GetService<IErrorLoggingService>();

                        if (errorLoggingService != null)
                        {
                            await errorLoggingService.LogErrorAsync($"عملية تدقيق: {action}",
                                details, ErrorType.AuditLog, ErrorSeverity.Info, null, username);
                        }
                    }
                    catch { /* تجاهل أخطاء التسجيل في قاعدة البيانات */ }
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"فشل في تسجيل عملية التدقيق: {ex.Message}");
            }
        }

        /// <summary>
        /// تسجيل عملية بيانات عامة
        /// </summary>
        public static void LogDataOperation(string operation, string? context = null,
            string? details = null, bool isSuccess = true,
            [CallerMemberName] string? caller = null,
            [CallerFilePath] string? filePath = null,
            [CallerLineNumber] int lineNumber = 0)
        {
            try
            {
                var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);

                var logContext = Log.ForContext("DataOperation", true)
                                   .ForContext("Operation", operation)
                                   .ForContext("Context", context)
                                   .ForContext("IsSuccess", isSuccess)
                                   .ForContext("SourceFile", fileName)
                                   .ForContext("SourceMethod", caller)
                                   .ForContext("LineNumber", lineNumber);

                if (isSuccess)
                {
                    logContext.Information("💾 بيانات: {Operation} | السياق: {Context} | التفاصيل: {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           operation, context, details, fileName, caller, lineNumber);
                }
                else
                {
                    logContext.Warning("⚠️ فشل عملية بيانات: {Operation} | السياق: {Context} | التفاصيل: {Details} | من: {SourceFile}.{SourceMethod}:{LineNumber}",
                           operation, context, details, fileName, caller, lineNumber);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"فشل في تسجيل عملية البيانات: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// استثناء متعلق بالأمان
    /// </summary>
    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, Exception innerException) : base(message, innerException) { }
    }
}