using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace AccountingSystem.WPF.Extensions;

/// <summary>
/// Extension methods for improved logging
/// </summary>
public static class LoggerExtensions
{
    /// <summary>
    /// Logs method entry with automatic method name detection
    /// </summary>
    public static void LogMethodEntry(this ILogger logger, 
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            logger.LogDebug("Entering {ClassName}.{MethodName}", className, methodName);
        }
    }

    /// <summary>
    /// Logs method exit with automatic method name detection
    /// </summary>
    public static void LogMethodExit(this ILogger logger,
        [CallerMemberName] string methodName = "",
        [CallerFilePath] string filePath = "")
    {
        if (logger.IsEnabled(LogLevel.Debug))
        {
            var className = System.IO.Path.GetFileNameWithoutExtension(filePath);
            logger.LogDebug("Exiting {ClassName}.{MethodName}", className, methodName);
        }
    }

    /// <summary>
    /// Logs operation result with timing information
    /// </summary>
    public static void LogOperationResult(this ILogger logger, 
        string operation, 
        bool success, 
        TimeSpan duration,
        string? additionalInfo = null)
    {
        var level = success ? LogLevel.Information : LogLevel.Warning;
        var status = success ? "succeeded" : "failed";
        
        if (string.IsNullOrEmpty(additionalInfo))
        {
            logger.Log(level, "Operation {Operation} {Status} in {Duration}ms", 
                operation, status, duration.TotalMilliseconds);
        }
        else
        {
            logger.Log(level, "Operation {Operation} {Status} in {Duration}ms. Additional info: {Info}", 
                operation, status, duration.TotalMilliseconds, additionalInfo);
        }
    }

    /// <summary>
    /// Logs user action for audit purposes
    /// </summary>
    public static void LogUserAction(this ILogger logger, 
        string userName, 
        string action, 
        string? details = null)
    {
        if (string.IsNullOrEmpty(details))
        {
            logger.LogInformation("User {UserName} performed action: {Action}", userName, action);
        }
        else
        {
            logger.LogInformation("User {UserName} performed action: {Action}. Details: {Details}", 
                userName, action, details);
        }
    }
}