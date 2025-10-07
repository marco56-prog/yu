using System;
using System.Threading;

namespace AccountingSystem.Business.Common
{
    /// <summary>
    /// عمليات آمنة للخيوط المتعددة - Thread-safe operations
    /// </summary>
    public static class ThreadSafeOperations
    {
        /// <summary>
        /// قفل آمن للخيوط لضمان تنفيذ العملية مرة واحدة فقط
        /// Thread-safe lock to ensure operation executes only once
        /// </summary>
        public static class Once
        {
            /// <summary>
            /// تنفيذ العملية مرة واحدة فقط بطريقة آمنة للخيوط
            /// </summary>
            public static void Execute(ref int flag, Action action)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));

                if (Interlocked.CompareExchange(ref flag, 1, 0) == 0)
                {
                    try
                    {
                        action();
                    }
                    catch
                    {
                        // إعادة تعيين العلم في حالة الفشل للسماح بإعادة المحاولة
                        Interlocked.Exchange(ref flag, 0);
                        throw;
                    }
                }
            }

            /// <summary>
            /// تنفيذ العملية مرة واحدة فقط مع قيمة إرجاع
            /// </summary>
            public static T Execute<T>(ref int flag, Func<T> func)
            {
                if (func == null)
                    throw new ArgumentNullException(nameof(func));

                if (Interlocked.CompareExchange(ref flag, 1, 0) == 0)
                {
                    try
                    {
                        return func();
                    }
                    catch
                    {
                        // إعادة تعيين العلم في حالة الفشل للسماح بإعادة المحاولة
                        Interlocked.Exchange(ref flag, 0);
                        throw;
                    }
                }

                throw new InvalidOperationException("العملية تم تنفيذها من قبل");
            }
        }

        /// <summary>
        /// عداد آمن للخيوط
        /// Thread-safe counter
        /// </summary>
        public class AtomicCounter
        {
            private int _value;

            public int Value => _value;

            public AtomicCounter(int initialValue = 0)
            {
                _value = initialValue;
            }

            /// <summary>
            /// زيادة القيمة بمقدار 1 وإرجاع القيمة الجديدة
            /// </summary>
            public int Increment()
            {
                return Interlocked.Increment(ref _value);
            }

            /// <summary>
            /// تقليل القيمة بمقدار 1 وإرجاع القيمة الجديدة
            /// </summary>
            public int Decrement()
            {
                return Interlocked.Decrement(ref _value);
            }

            /// <summary>
            /// إضافة قيمة معينة وإرجاع القيمة الجديدة
            /// </summary>
            public int Add(int value)
            {
                return Interlocked.Add(ref _value, value);
            }

            /// <summary>
            /// تعيين قيمة جديدة وإرجاع القيمة القديمة
            /// </summary>
            public int Exchange(int newValue)
            {
                return Interlocked.Exchange(ref _value, newValue);
            }

            /// <summary>
            /// تعيين قيمة جديدة فقط إذا كانت القيمة الحالية مساوية لقيمة معينة
            /// </summary>
            public bool CompareAndExchange(int comparand, int newValue)
            {
                return Interlocked.CompareExchange(ref _value, newValue, comparand) == comparand;
            }
        }

        /// <summary>
        /// قفل بسيط آمن للخيوط
        /// Simple thread-safe lock
        /// </summary>
        public class SimpleLock
        {
            private readonly object _syncRoot = new object();

            /// <summary>
            /// تنفيذ عملية داخل قفل آمن
            /// </summary>
            public void Execute(Action action)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));

                lock (_syncRoot)
                {
                    action();
                }
            }

            /// <summary>
            /// تنفيذ عملية داخل قفل آمن مع قيمة إرجاع
            /// </summary>
            public T Execute<T>(Func<T> func)
            {
                if (func == null)
                    throw new ArgumentNullException(nameof(func));

                lock (_syncRoot)
                {
                    return func();
                }
            }

            /// <summary>
            /// محاولة الحصول على القفل مع timeout
            /// </summary>
            public bool TryExecute(Action action, TimeSpan timeout)
            {
                if (action == null)
                    throw new ArgumentNullException(nameof(action));

                if (Monitor.TryEnter(_syncRoot, timeout))
                {
                    try
                    {
                        action();
                        return true;
                    }
                    finally
                    {
                        Monitor.Exit(_syncRoot);
                    }
                }

                return false;
            }
        }
    }
}
