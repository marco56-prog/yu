using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AccountingSystem.WPF.Helpers
{
    /// <summary>
    /// مساعد للعمليات المتكررة في واجهة المستخدم
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// تنفيذ عملية على UI Thread بطريقة آمنة
        /// </summary>
        /// <param name="action">العملية المراد تنفيذها</param>
        public static void InvokeOnUIThread(Action action)
        {
            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                action();
            }
            else
            {
                Application.Current?.Dispatcher.Invoke(action);
            }
        }

        /// <summary>
        /// تنفيذ عملية غير متزامنة على UI Thread بطريقة آمنة
        /// </summary>
        /// <param name="action">العملية المراد تنفيذها</param>
        public static async Task InvokeOnUIThreadAsync(Action action)
        {
            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                action();
            }
            else
            {
                await Application.Current.Dispatcher.InvokeAsync(action);
            }
        }

        /// <summary>
        /// تنفيذ عملية غير متزامنة على UI Thread بطريقة آمنة مع إرجاع قيمة
        /// </summary>
        /// <typeparam name="T">نوع القيمة المرجعة</typeparam>
        /// <param name="func">الوظيفة المراد تنفيذها</param>
        /// <returns>القيمة المرجعة</returns>
        public static async Task<T> InvokeOnUIThreadAsync<T>(Func<T> func)
        {
            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                return func();
            }
            else
            {
                return await Application.Current.Dispatcher.InvokeAsync(func);
            }
        }

        /// <summary>
        /// التحقق من أن العملية تتم على UI Thread
        /// </summary>
        /// <returns>true إذا كانت على UI Thread</returns>
        public static bool IsOnUIThread()
        {
            return Application.Current?.Dispatcher.CheckAccess() == true;
        }
    }
}