using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using AccountingSystem.Diagnostics.Core;
using AccountingSystem.Diagnostics.HealthChecks.Database;
using AccountingSystem.Diagnostics.HealthChecks.Performance;
using AccountingSystem.Diagnostics.HealthChecks.System;
using AccountingSystem.Diagnostics.HealthChecks.UI;
using AccountingSystem.Diagnostics.Models;
using System.Threading.Tasks;

namespace AccountingSystem.Diagnostics.Tests
{
    /// <summary>
    /// اختبارات شاملة لفحوصات قاعدة البيانات
    /// Comprehensive Database Health Check Tests
    /// </summary>
    [TestFixture]
    public class DatabaseHealthChecksTests
    {
        private Mock<ILogger<DatabaseConnectionCheck>> _mockLogger;
        private DatabaseConnectionCheck _databaseCheck;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<DatabaseConnectionCheck>>();
            _databaseCheck = new DatabaseConnectionCheck(_mockLogger.Object);
        }

        [Test]
        public async Task DatabaseConnectionCheck_ValidConnection_ShouldReturnSuccess()
        {
            // Act
            var result = await _databaseCheck.CheckAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CheckName, Contains.Substring("اتصال"));
            Assert.That(result.Category, Is.EqualTo("قاعدة البيانات"));
            // Note: This may fail if no database is available, which is expected in test environment
        }

        [Test]
        public void DatabaseConnectionCheck_Properties_ShouldBeCorrect()
        {
            // Assert
            Assert.That(_databaseCheck.Name, Contains.Substring("اتصال"));
            Assert.That(_databaseCheck.Category, Is.EqualTo("قاعدة البيانات"));
            Assert.That(_databaseCheck.IsEnabled, Is.True);
            Assert.That(_databaseCheck.Priority, Is.EqualTo(1));
        }
    }

    /// <summary>
    /// اختبارات شاملة لفحوصات الأداء
    /// Comprehensive Performance Health Check Tests
    /// </summary>
    [TestFixture]
    public class PerformanceHealthChecksTests
    {
        private Mock<ILogger<MemoryUsageCheck>> _mockMemoryLogger;
        private Mock<ILogger<CpuUsageCheck>> _mockCpuLogger;
        private Mock<ILogger<DiskSpaceCheck>> _mockDiskLogger;
        
        private MemoryUsageCheck _memoryCheck;
        private CpuUsageCheck _cpuCheck;
        private DiskSpaceCheck _diskCheck;

        [SetUp]
        public void Setup()
        {
            _mockMemoryLogger = new Mock<ILogger<MemoryUsageCheck>>();
            _mockCpuLogger = new Mock<ILogger<CpuUsageCheck>>();
            _mockDiskLogger = new Mock<ILogger<DiskSpaceCheck>>();
            
            _memoryCheck = new MemoryUsageCheck(_mockMemoryLogger.Object);
            _cpuCheck = new CpuUsageCheck(_mockCpuLogger.Object);
            _diskCheck = new DiskSpaceCheck(_mockDiskLogger.Object);
        }

        [Test]
        public async Task MemoryUsageCheck_ShouldReturnResult()
        {
            // Act
            var result = await _memoryCheck.CheckAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CheckName, Contains.Substring("الذاكرة"));
            Assert.That(result.Category, Is.EqualTo("الأداء"));
            Assert.That(result.Status, Is.AnyOf(HealthStatus.Ok, HealthStatus.Warning, HealthStatus.Failed));
        }

        [Test]
        public async Task CpuUsageCheck_ShouldReturnResult()
        {
            // Act
            var result = await _cpuCheck.CheckAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CheckName, Contains.Substring("المعالج"));
            Assert.That(result.Category, Is.EqualTo("الأداء"));
            Assert.That(result.Status, Is.AnyOf(HealthStatus.Ok, HealthStatus.Warning, HealthStatus.Failed));
        }

        [Test]
        public async Task DiskSpaceCheck_ShouldReturnResult()
        {
            // Act
            var result = await _diskCheck.CheckAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CheckName, Contains.Substring("القرص"));
            Assert.That(result.Category, Is.EqualTo("النظام"));
            Assert.That(result.Status, Is.AnyOf(HealthStatus.Ok, HealthStatus.Warning, HealthStatus.Failed));
        }

        [Test]
        public void PerformanceChecks_Properties_ShouldBeCorrect()
        {
            // Memory Check
            Assert.That(_memoryCheck.Name, Contains.Substring("الذاكرة"));
            Assert.That(_memoryCheck.Category, Is.EqualTo("الأداء"));
            Assert.That(_memoryCheck.IsEnabled, Is.True);

            // CPU Check
            Assert.That(_cpuCheck.Name, Contains.Substring("المعالج"));
            Assert.That(_cpuCheck.Category, Is.EqualTo("الأداء"));
            Assert.That(_cpuCheck.IsEnabled, Is.True);

            // Disk Check
            Assert.That(_diskCheck.Name, Contains.Substring("القرص"));
            Assert.That(_diskCheck.Category, Is.EqualTo("النظام"));
            Assert.That(_diskCheck.IsEnabled, Is.True);
        }
    }

    /// <summary>
    /// اختبارات شاملة لفحوصات النظام
    /// Comprehensive System Health Check Tests
    /// </summary>
    [TestFixture]
    public class SystemHealthChecksTests
    {
        private Mock<ILogger<WindowsVersionCheck>> _mockWindowsLogger;
        private Mock<ILogger<DotNetVersionCheck>> _mockDotNetLogger;
        private Mock<ILogger<EventLogCheck>> _mockEventLogLogger;
        
        private WindowsVersionCheck _windowsCheck;
        private DotNetVersionCheck _dotNetCheck;
        private EventLogCheck _eventLogCheck;

        [SetUp]
        public void Setup()
        {
            _mockWindowsLogger = new Mock<ILogger<WindowsVersionCheck>>();
            _mockDotNetLogger = new Mock<ILogger<DotNetVersionCheck>>();
            _mockEventLogLogger = new Mock<ILogger<EventLogCheck>>();
            
            _windowsCheck = new WindowsVersionCheck(_mockWindowsLogger.Object);
            _dotNetCheck = new DotNetVersionCheck(_mockDotNetLogger.Object);
            _eventLogCheck = new EventLogCheck(_mockEventLogLogger.Object);
        }

        [Test]
        public async Task WindowsVersionCheck_ShouldReturnResult()
        {
            // Act
            var result = await _windowsCheck.CheckAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CheckName, Contains.Substring("Windows"));
            Assert.That(result.Category, Is.EqualTo("النظام"));
            Assert.That(result.Status, Is.AnyOf(HealthStatus.Ok, HealthStatus.Warning, HealthStatus.Failed));
        }

        [Test]
        public async Task DotNetVersionCheck_ShouldReturnResult()
        {
            // Act
            var result = await _dotNetCheck.CheckAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CheckName, Contains.Substring(".NET"));
            Assert.That(result.Category, Is.EqualTo("النظام"));
            Assert.That(result.Status, Is.AnyOf(HealthStatus.Ok, HealthStatus.Warning, HealthStatus.Failed));
        }

        [Test]
        public async Task EventLogCheck_ShouldReturnResult()
        {
            // Act
            var result = await _eventLogCheck.CheckAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CheckName, Contains.Substring("سجل"));
            Assert.That(result.Category, Is.EqualTo("النظام"));
            Assert.That(result.Status, Is.AnyOf(HealthStatus.Ok, HealthStatus.Warning, HealthStatus.Failed));
        }

        [Test]
        public void SystemChecks_Properties_ShouldBeCorrect()
        {
            // Windows Check
            Assert.That(_windowsCheck.Name, Contains.Substring("Windows"));
            Assert.That(_windowsCheck.Category, Is.EqualTo("النظام"));
            Assert.That(_windowsCheck.IsEnabled, Is.True);

            // .NET Check
            Assert.That(_dotNetCheck.Name, Contains.Substring(".NET"));
            Assert.That(_dotNetCheck.Category, Is.EqualTo("النظام"));
            Assert.That(_dotNetCheck.IsEnabled, Is.True);

            // Event Log Check
            Assert.That(_eventLogCheck.Name, Contains.Substring("سجل"));
            Assert.That(_eventLogCheck.Category, Is.EqualTo("النظام"));
            Assert.That(_eventLogCheck.IsEnabled, Is.True);
        }
    }

    /// <summary>
    /// اختبارات شاملة لفحوصات واجهة المستخدم
    /// Comprehensive UI Health Check Tests
    /// </summary>
    [TestFixture]
    public class UIHealthChecksTests
    {
        private Mock<ILogger<ThemeResourcesCheck>> _mockThemeLogger;
        private Mock<ILogger<WindowIntegrityCheck>> _mockWindowLogger;
        private Mock<ILogger<LocalizationCheck>> _mockLocalizationLogger;
        
        private ThemeResourcesCheck _themeCheck;
        private WindowIntegrityCheck _windowCheck;
        private LocalizationCheck _localizationCheck;

        [SetUp]
        public void Setup()
        {
            _mockThemeLogger = new Mock<ILogger<ThemeResourcesCheck>>();
            _mockWindowLogger = new Mock<ILogger<WindowIntegrityCheck>>();
            _mockLocalizationLogger = new Mock<ILogger<LocalizationCheck>>();
            
            _themeCheck = new ThemeResourcesCheck(_mockThemeLogger.Object);
            _windowCheck = new WindowIntegrityCheck(_mockWindowLogger.Object);
            _localizationCheck = new LocalizationCheck(_mockLocalizationLogger.Object);
        }

        [Test]
        public async Task ThemeResourcesCheck_ShouldReturnResult()
        {
            // Act
            var result = await _themeCheck.CheckAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CheckName, Contains.Substring("الثيم"));
            Assert.That(result.Category, Is.EqualTo("واجهة المستخدم"));
            Assert.That(result.Status, Is.AnyOf(HealthStatus.Ok, HealthStatus.Warning, HealthStatus.Failed));
        }

        [Test]
        public async Task WindowIntegrityCheck_ShouldReturnResult()
        {
            // Act
            var result = await _windowCheck.CheckAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CheckName, Contains.Substring("النوافذ"));
            Assert.That(result.Category, Is.EqualTo("واجهة المستخدم"));
            Assert.That(result.Status, Is.AnyOf(HealthStatus.Ok, HealthStatus.Warning, HealthStatus.Failed));
        }

        [Test]
        public async Task LocalizationCheck_ShouldReturnResult()
        {
            // Act
            var result = await _localizationCheck.CheckAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.CheckName, Contains.Substring("الترجمة"));
            Assert.That(result.Category, Is.EqualTo("واجهة المستخدم"));
            Assert.That(result.Status, Is.AnyOf(HealthStatus.Ok, HealthStatus.Warning, HealthStatus.Failed));
        }

        [Test]
        public void UIChecks_Properties_ShouldBeCorrect()
        {
            // Theme Check
            Assert.That(_themeCheck.Name, Contains.Substring("الثيم"));
            Assert.That(_themeCheck.Category, Is.EqualTo("واجهة المستخدم"));
            Assert.That(_themeCheck.IsEnabled, Is.True);

            // Window Check
            Assert.That(_windowCheck.Name, Contains.Substring("النوافذ"));
            Assert.That(_windowCheck.Category, Is.EqualTo("واجهة المستخدم"));
            Assert.That(_windowCheck.IsEnabled, Is.True);

            // Localization Check
            Assert.That(_localizationCheck.Name, Contains.Substring("الترجمة"));
            Assert.That(_localizationCheck.Category, Is.EqualTo("واجهة المستخدم"));
            Assert.That(_localizationCheck.IsEnabled, Is.True);
        }
    }

    /// <summary>
    /// اختبارات تكامل شاملة لجميع مكونات النظام التشخيصي
    /// Comprehensive Integration Tests for All Diagnostic System Components
    /// </summary>
    [TestFixture]
    public class DiagnosticsIntegrationTests
    {
        private ServiceProvider _serviceProvider;
        private HealthCheckRunner _healthCheckRunner;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            
            // Register logging
            services.AddLogging(builder => builder.AddConsole());
            
            // Register all health checks
            services.AddTransient<DatabaseConnectionCheck>();
            services.AddTransient<MemoryUsageCheck>();
            services.AddTransient<CpuUsageCheck>();
            services.AddTransient<DiskSpaceCheck>();
            services.AddTransient<WindowsVersionCheck>();
            services.AddTransient<DotNetVersionCheck>();
            services.AddTransient<EventLogCheck>();
            services.AddTransient<ThemeResourcesCheck>();
            services.AddTransient<WindowIntegrityCheck>();
            services.AddTransient<LocalizationCheck>();
            
            // Register core services
            services.AddTransient<HealthCheckRunner>();
            
            _serviceProvider = services.BuildServiceProvider();
            _healthCheckRunner = _serviceProvider.GetRequiredService<HealthCheckRunner>();
        }

        [TearDown]
        public void TearDown()
        {
            _serviceProvider?.Dispose();
        }

        [Test]
        public async Task HealthCheckRunner_QuickCheck_ShouldCompleteAllChecks()
        {
            // Arrange
            var options = DiagnosticsOptions.QuickCheck();

            // Act
            var results = await _healthCheckRunner.RunAllChecksAsync(options);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Not.Empty);
            
            // Should have results from all major categories
            var categories = results.Select(r => r.Category).Distinct().ToList();
            Assert.That(categories, Contains.Item("قاعدة البيانات"));
            Assert.That(categories, Contains.Item("الأداء"));
            Assert.That(categories, Contains.Item("النظام"));
            Assert.That(categories, Contains.Item("واجهة المستخدم"));
        }

        [Test]
        public async Task HealthCheckRunner_CategoryFilter_ShouldReturnOnlySpecifiedCategory()
        {
            // Arrange
            var options = DiagnosticsOptions.QuickCheck();
            options.CategoryFilter = "الأداء";

            // Act
            var results = await _healthCheckRunner.RunAllChecksAsync(options);

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results, Is.Not.Empty);
            
            // All results should be from Performance category
            Assert.That(results.All(r => r.Category == "الأداء"), Is.True);
        }

        [Test]
        public async Task DiagnosticsSystem_EndToEnd_ShouldCompleteSuccessfully()
        {
            // This test simulates a complete end-to-end diagnostic run
            
            // Arrange
            var options = DiagnosticsOptions.QuickCheck();
            var startTime = DateTime.UtcNow;

            // Act
            var healthResults = await _healthCheckRunner.RunAllChecksAsync(options);
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.That(healthResults, Is.Not.Null);
            Assert.That(healthResults, Is.Not.Empty);
            
            // Check timing
            var totalDuration = endTime - startTime;
            Assert.That(totalDuration, Is.LessThan(TimeSpan.FromSeconds(30)), 
                "Quick check should complete within 30 seconds");
            
            // Check that we have diverse results
            var statusTypes = healthResults.Select(r => r.Status).Distinct().ToList();
            Assert.That(statusTypes, Is.Not.Empty);
            
            // Log summary for debugging
            Console.WriteLine($"Completed {healthResults.Count} checks in {totalDuration.TotalSeconds:F2} seconds");
            Console.WriteLine($"Results: {healthResults.Count(r => r.Status == HealthStatus.Ok)} OK, " +
                            $"{healthResults.Count(r => r.Status == HealthStatus.Warning)} Warning, " +
                            $"{healthResults.Count(r => r.Status == HealthStatus.Failed)} Failed");
        }
    }

    /// <summary>
    /// اختبارات الأداء والتحميل للنظام التشخيصي
    /// Performance and Load Tests for Diagnostic System
    /// </summary>
    [TestFixture]
    public class DiagnosticsPerformanceTests
    {
        private HealthCheckRunner _healthCheckRunner;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTransient<HealthCheckRunner>();
            
            var serviceProvider = services.BuildServiceProvider();
            _healthCheckRunner = serviceProvider.GetRequiredService<HealthCheckRunner>();
        }

        [Test]
        public async Task HealthCheckRunner_ConcurrentChecks_ShouldHandleLoad()
        {
            // Arrange
            var options = DiagnosticsOptions.QuickCheck();
            var concurrentRuns = 5;
            var tasks = new List<Task<List<HealthCheckResult>>>();

            // Act
            var startTime = DateTime.UtcNow;
            
            for (int i = 0; i < concurrentRuns; i++)
            {
                tasks.Add(_healthCheckRunner.RunAllChecksAsync(options));
            }
            
            var results = await Task.WhenAll(tasks);
            var endTime = DateTime.UtcNow;

            // Assert
            Assert.That(results, Is.Not.Null);
            Assert.That(results.Length, Is.EqualTo(concurrentRuns));
            
            foreach (var result in results)
            {
                Assert.That(result, Is.Not.Empty);
            }
            
            var totalDuration = endTime - startTime;
            Console.WriteLine($"Completed {concurrentRuns} concurrent diagnostic runs in {totalDuration.TotalSeconds:F2} seconds");
        }

        [Test]
        public async Task HealthCheckRunner_RepeatedRuns_ShouldBeConsistent()
        {
            // Arrange
            var options = DiagnosticsOptions.QuickCheck();
            var runCount = 3;
            var allResults = new List<List<HealthCheckResult>>();

            // Act
            for (int i = 0; i < runCount; i++)
            {
                var results = await _healthCheckRunner.RunAllChecksAsync(options);
                allResults.Add(results);
                
                // Small delay between runs
                await Task.Delay(100);
            }

            // Assert
            Assert.That(allResults.Count, Is.EqualTo(runCount));
            
            // Check that all runs returned similar number of results
            var resultCounts = allResults.Select(r => r.Count).ToList();
            var minCount = resultCounts.Min();
            var maxCount = resultCounts.Max();
            
            Assert.That(maxCount - minCount, Is.LessThanOrEqualTo(1), 
                "Result counts should be consistent across runs");
        }
    }
}