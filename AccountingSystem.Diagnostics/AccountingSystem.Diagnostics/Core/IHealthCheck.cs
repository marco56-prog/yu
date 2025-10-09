using System.Threading;
using System.Threading.Tasks;
using AccountingSystem.Diagnostics.Models;

namespace AccountingSystem.Diagnostics.Core
{
    /// <summary>
    /// واجهة فحص الصحة الأساسية
    /// </summary>
    public interface IHealthCheck
    {
        /// <summary>
        /// اسم الفحص
        /// </summary>
        string Name { get; }
        
        /// <summary>
        /// فئة الفحص
        /// </summary>
        string Category { get; }
        
        /// <summary>
        /// وصف مفصل للفحص
        /// </summary>
        string Description { get; }
        
        /// <summary>
        /// أولوية الفحص (1 = عالية)
        /// </summary>
        int Priority { get; }
        
        /// <summary>
        /// هل الفحص مفعل؟
        /// </summary>
        bool IsEnabled { get; }
        
        /// <summary>
        /// تنفيذ الفحص
        /// </summary>
        Task<HealthCheckResult> CheckAsync(CancellationToken cancellationToken = default);
    }
}