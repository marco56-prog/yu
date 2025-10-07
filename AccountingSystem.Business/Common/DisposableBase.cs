using System;

namespace AccountingSystem.Business.Common
{
    /// <summary>
    /// قاعدة لإدارة الموارد بشكل صحيح - تطبيق IDisposable pattern
    /// Base class for proper resource management implementing IDisposable pattern
    /// </summary>
    public abstract class DisposableBase : IDisposable
    {
        private bool _disposed = false;

        /// <summary>
        /// التخلص من الموارد المُدارة وغير المُدارة
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// التخلص من الموارد - يمكن تجاوزها في الفئات المشتقة
        /// </summary>
        /// <param name="disposing">true إذا تم الاستدعاء من Dispose، false إذا من finalizer</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                // تحرير الموارد المُدارة (managed resources)
                DisposeManagedResources();
            }

            // تحرير الموارد غير المُدارة (unmanaged resources)
            DisposeUnmanagedResources();

            _disposed = true;
        }

        /// <summary>
        /// تحرير الموارد المُدارة - يجب تجاوزها في الفئات المشتقة
        /// </summary>
        protected virtual void DisposeManagedResources()
        {
            // Override في الفئات المشتقة لتحرير الموارد المُدارة
        }

        /// <summary>
        /// تحرير الموارد غير المُدارة - يمكن تجاوزها إذا لزم الأمر
        /// </summary>
        protected virtual void DisposeUnmanagedResources()
        {
            // Override في الفئات المشتقة لتحرير الموارد غير المُدارة
        }

        /// <summary>
        /// التحقق من أن الكائن لم يتم التخلص منه بعد
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(GetType().FullName, "لا يمكن استخدام الكائن بعد التخلص منه");
            }
        }

        /// <summary>
        /// المُدمر - يتم استدعاؤه فقط إذا لم يتم استدعاء Dispose
        /// </summary>
        ~DisposableBase()
        {
            Dispose(false);
        }
    }
}
