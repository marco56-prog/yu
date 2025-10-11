using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;

namespace AccountingSystem.WPF.Helpers
{
    /// <summary>
    /// Utilities for safely operating on CollectionView instances from UI code.
    /// </summary>
    public static class CollectionViewHelper
    {
        /// <summary>
        /// Attempts to call <see cref="ICollectionView.Refresh"/> safely, handling common CollectionView exceptions.
        /// Schedules retry attempts on the UI dispatcher if needed. Swallows exceptions to avoid crashing the UI.
        /// </summary>
        public static void SafeRefresh(ICollectionView? view)
        {
            if (view == null) return;

            try
            {
                view.Refresh();
            }
            catch (InvalidOperationException ex) when (
                ex.Message.Contains("Refresh is being deferred") ||
                ex.Message.Contains("Current position") ||
                ex.Message.Contains("CollectionView"))
            {
                // جدولة إعادة المحاولة على الـ dispatcher مع أولوية منخفضة
                ScheduleRetryRefresh(view);
            }
            catch (Exception ex) when (!(ex is OutOfMemoryException || ex is StackOverflowException))
            {
                // عدم إسقاط التطبيق بسبب أخطاء CollectionView
                // لكن تجنب التعامل مع الأخطاء الحرجة
            }
        }

        private static void ScheduleRetryRefresh(ICollectionView view)
        {
            try
            {
                var dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;

                // محاولة بأولوية Background أولاً
                dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        view.Refresh();
                    }
                    catch
                    {
                        // إذا فشلت، محاولة أخيرة بأولوية أقل
                        dispatcher.BeginInvoke(new Action(() =>
                        {
                            try { view.Refresh(); } catch { /* final attempt - swallow */ }
                        }), DispatcherPriority.ContextIdle);
                    }
                }), DispatcherPriority.Background);
            }
            catch
            {
                // آخر محاولة - تجاهل الخطأ حتى لا يسقط التطبيق
            }
        }
    }
}
