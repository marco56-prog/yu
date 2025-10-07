using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AccountingSystem.WPF.Constants;

namespace AccountingSystem.WPF.Helpers;

/// <summary>
/// Helper class for window management operations
/// </summary>
public class WindowHelper
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WindowHelper> _logger;
    private static readonly Dictionary<string, Type> _cachedTypes = new();

    public WindowHelper(IServiceProvider serviceProvider, ILogger<WindowHelper> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Opens a window with dependency injection and proper resource management
    /// </summary>
    /// <typeparam name="T">Type of window to open</typeparam>
    /// <param name="isDialog">Whether to show as dialog</param>
    /// <returns>Dialog result if shown as dialog, otherwise null</returns>
    public bool? OpenWindow<T>(bool isDialog = false) where T : System.Windows.Window
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var window = scope.ServiceProvider.GetRequiredService<T>();
            
            return ShowWindow(window, isDialog, scope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening window of type {WindowType}", typeof(T).Name);
            System.Windows.MessageBox.Show(
                $"خطأ في فتح النافذة: {ex.Message}",
                ApplicationConstants.ErrorTitleArabic,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return null;
        }
    }

    /// <summary>
    /// Opens a window by name with caching for better performance
    /// </summary>
    /// <param name="windowName">Name of the window type</param>
    /// <param name="isDialog">Whether to show as dialog</param>
    /// <returns>Dialog result if shown as dialog, otherwise null</returns>
    public bool? OpenWindowByName(string windowName, bool isDialog = false)
    {
        try
        {
            var windowType = GetWindowType(windowName);
            if (windowType == null)
            {
                _logger.LogWarning("Window type not found: {WindowName}", windowName);
                return null;
            }

            using var scope = _serviceProvider.CreateScope();
            var window = (System.Windows.Window)scope.ServiceProvider.GetRequiredService(windowType);
            
            return ShowWindow(window, isDialog, scope);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening window by name: {WindowName}", windowName);
            System.Windows.MessageBox.Show(
                $"خطأ في فتح النافذة {windowName}: {ex.Message}",
                ApplicationConstants.ErrorTitleArabic,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
            return null;
        }
    }

    /// <summary>
    /// Gets window type with caching for better performance
    /// </summary>
    /// <param name="windowName">Name of the window type</param>
    /// <returns>Type of the window or null if not found</returns>
    private Type? GetWindowType(string windowName)
    {
        if (_cachedTypes.TryGetValue(windowName, out var cachedType))
        {
            return cachedType;
        }

        var type = GetTypeFromLoadedAssemblies(windowName);
        if (type != null)
        {
            _cachedTypes[windowName] = type;
        }

        return type;
    }

    /// <summary>
    /// Finds type from loaded assemblies with improved performance
    /// </summary>
    /// <param name="typeName">Name of the type to find</param>
    /// <returns>Type if found, otherwise null</returns>
    private static Type? GetTypeFromLoadedAssemblies(string typeName)
    {
        try
        {
            // First, try the current assembly (most likely location)
            var currentAssembly = Assembly.GetExecutingAssembly();
            var type = currentAssembly.GetTypes()
                .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
            
            if (type != null)
                return type;

            // Then search other loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location));

            foreach (var assembly in assemblies)
            {
                try
                {
                    type = assembly.GetTypes()
                        .FirstOrDefault(t => t.Name.Equals(typeName, StringComparison.OrdinalIgnoreCase));
                    
                    if (type != null)
                        return type;
                }
                catch (ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be loaded
                    continue;
                }
            }

            return null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Shows window with proper resource management
    /// </summary>
    /// <param name="window">Window to show</param>
    /// <param name="isDialog">Whether to show as dialog</param>
    /// <param name="scope">Service scope to dispose when window closes</param>
    /// <returns>Dialog result if shown as dialog, otherwise null</returns>
    private bool? ShowWindow(System.Windows.Window window, bool isDialog, IServiceScope scope)
    {
        // Handle scope disposal when window closes
        window.Closed += (_, _) => DisposeScope(scope);

        if (isDialog)
        {
            return window.ShowDialog();
        }
        else
        {
            window.Show();
            return null;
        }
    }

    /// <summary>
    /// Safely disposes service scope
    /// </summary>
    /// <param name="scope">Scope to dispose</param>
    private void DisposeScope(IServiceScope scope)
    {
        try
        {
            scope?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing service scope");
        }
    }
}