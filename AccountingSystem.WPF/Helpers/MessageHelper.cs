using System;
using System.Windows;
using AccountingSystem.WPF.Constants;

namespace AccountingSystem.WPF.Helpers;

/// <summary>
/// Helper class for displaying standardized messages to users
/// </summary>
public static class MessageHelper
{
    /// <summary>
    /// Shows an error message with consistent formatting
    /// </summary>
    /// <param name="message">Error message in Arabic</param>
    /// <param name="title">Optional custom title (defaults to standard error title)</param>
    /// <param name="owner">Owner window for modal display</param>
    public static void ShowError(string message, string? title = null, Window? owner = null)
    {
        var msgBox = MessageBox.Show(
            message,
            title ?? ApplicationConstants.ErrorTitleArabic,
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    /// <summary>
    /// Shows an error message for window opening failures
    /// </summary>
    /// <param name="windowName">Name of the window that failed to open</param>
    /// <param name="exception">The exception that occurred</param>
    /// <param name="owner">Owner window for modal display</param>
    public static void ShowWindowOpenError(string windowName, Exception exception, Window? owner = null)
    {
        ShowError($"خطأ في فتح {windowName}:\n{exception.Message}", owner: owner);
    }

    /// <summary>
    /// Shows a warning message with consistent formatting
    /// </summary>
    /// <param name="message">Warning message in Arabic</param>
    /// <param name="title">Optional custom title (defaults to standard warning title)</param>
    /// <param name="owner">Owner window for modal display</param>
    public static void ShowWarning(string message, string? title = null, Window? owner = null)
    {
        MessageBox.Show(
            message,
            title ?? ApplicationConstants.WarningTitleArabic,
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    /// <summary>
    /// Shows an information message with consistent formatting
    /// </summary>
    /// <param name="message">Information message in Arabic</param>
    /// <param name="title">Optional custom title (defaults to standard info title)</param>
    /// <param name="owner">Owner window for modal display</param>
    public static void ShowInfo(string message, string? title = null, Window? owner = null)
    {
        MessageBox.Show(
            message,
            title ?? ApplicationConstants.InfoTitleArabic,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// Shows a success message with consistent formatting
    /// </summary>
    /// <param name="message">Success message in Arabic</param>
    /// <param name="title">Optional custom title (defaults to standard success title)</param>
    /// <param name="owner">Owner window for modal display</param>
    public static void ShowSuccess(string message, string? title = null, Window? owner = null)
    {
        MessageBox.Show(
            message,
            title ?? ApplicationConstants.SuccessTitleArabic,
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    /// <summary>
    /// Shows a confirmation dialog
    /// </summary>
    /// <param name="message">Confirmation message in Arabic</param>
    /// <param name="title">Optional custom title</param>
    /// <param name="owner">Owner window for modal display</param>
    /// <returns>True if user clicked Yes, false otherwise</returns>
    public static bool ShowConfirmation(string message, string? title = null, Window? owner = null)
    {
        var result = MessageBox.Show(
            message,
            title ?? ApplicationConstants.InfoTitleArabic,
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        return result == MessageBoxResult.Yes;
    }

    /// <summary>
    /// Shows a development message for features under construction
    /// </summary>
    /// <param name="featureName">Name of the feature under development</param>
    /// <param name="owner">Owner window for modal display</param>
    public static void ShowDevelopmentMessage(string featureName, Window? owner = null)
    {
        ShowInfo($"{featureName} - {ApplicationConstants.DevelopmentMessageArabic}", owner: owner);
    }
}