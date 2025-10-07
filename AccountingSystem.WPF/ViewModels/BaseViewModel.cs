// File: BaseViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace AccountingSystem.WPF.ViewModels;

/// <summary>
/// Base class for all ViewModels implementing INotifyPropertyChanged
/// </summary>
public abstract class BaseViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Raises the PropertyChanged event for a property.
    /// Ensures the event is raised on the UI thread when possible.
    /// </summary>
    /// <param name="propertyName">Name of the property that changed</param>
    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
#if DEBUG
        VerifyPropertyName(propertyName);
#endif
        // لو احنا على WPF، ضمن إن الحدث يتنفّذ على UI thread
        var app = Application.Current;
        if (app?.Dispatcher is Dispatcher dispatcher && !dispatcher.CheckAccess())
        {
            dispatcher.Invoke(() => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName)));
        }
        else
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// Raises PropertyChanged for multiple properties at once.
    /// </summary>
    protected void OnPropertiesChanged(params string[] propertyNames)
    {
        if (propertyNames == null || propertyNames.Length == 0) return;
        foreach (var name in propertyNames)
        {
            OnPropertyChanged(name);
        }
    }

    /// <summary>
    /// Sets a property value and raises PropertyChanged if the value changed.
    /// </summary>
    /// <typeparam name="T">Type of the property</typeparam>
    /// <param name="field">Reference to the backing field</param>
    /// <param name="value">New value</param>
    /// <param name="propertyName">Name of the property</param>
    /// <returns>True if the value changed, false otherwise</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        // يحافظ على التوقيع الأصلي تمامًا
        return SetProperty(ref field, value, comparer: null, onChanged: null, propertyName);
    }

    /// <summary>
    /// Sets a property value and raises PropertyChanged if the value changed, then runs onChanged.
    /// </summary>
    protected bool SetProperty<T>(ref T field, T value, Action? onChanged, [CallerMemberName] string? propertyName = null)
    {
        return SetProperty(ref field, value, comparer: null, onChanged, propertyName);
    }

    /// <summary>
    /// Sets a property value with optional custom comparer, runs onChanged, and notifies dependent properties.
    /// </summary>
    protected bool SetProperty<T>(
        ref T field,
        T value,
        IEqualityComparer<T>? comparer,
        Action? onChanged,
        [CallerMemberName] string? propertyName = null,
        params string[] dependentPropertyNames)
    {
        var cmp = comparer ?? EqualityComparer<T>.Default;
        if (cmp.Equals(field, value)) return false;

        field = value;

        // إشعار الخاصية الأساسية
        OnPropertyChanged(propertyName);

        // تنفيذ أكشن اختياري بعد التغيير
        onChanged?.Invoke();

        // إشعار خصائص تابعة (إن وجدت)
        if (dependentPropertyNames is { Length: > 0 })
            OnPropertiesChanged(dependentPropertyNames);

        return true;
    }

#if DEBUG
    /// <summary>
    /// Helps catch typos in property names at debug time.
    /// </summary>
    [Conditional("DEBUG")]
    private void VerifyPropertyName(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName)) return;

        // ابحث عن خاصية عامة بالاسم المحدد
        var exists = GetType().GetProperty(propertyName) != null;
        if (!exists)
        {
            Debug.Fail($"Property '{propertyName}' was not found on '{GetType().Name}'.");
        }
    }
#endif
}
