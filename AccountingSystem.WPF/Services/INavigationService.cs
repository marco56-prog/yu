using System;
using System.Collections.Generic;
using System.Windows;

namespace AccountingSystem.WPF.Services
{
    /// <summary>
    /// خدمة التنقل بين الشاشات
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// تسجيل نافذة جديدة في النظام
        /// </summary>
        void RegisterWindow<T>(string routeName) where T : Window;

        /// <summary>
        /// فتح نافذة حسب المسار المحدد
        /// </summary>
        void NavigateTo(string routeName);

        /// <summary>
        /// فتح نافذة كحوار
        /// </summary>
        bool? ShowDialog(string routeName);

        /// <summary>
        /// إغلاق النافذة الحالية
        /// </summary>
        void CloseWindow(Window window);
    }
}