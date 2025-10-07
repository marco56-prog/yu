using NUnit.Framework;
using Microsoft.Extensions.Logging;
using Moq;
using AccountingSystem.Diagnostics.Models;
using AccountingSystem.Diagnostics.Core;
using AccountingSystem.Diagnostics.HealthChecks;
using AccountingSystem.Diagnostics.Tests;
using System.Threading.Tasks;

namespace AccountingSystem.Diagnostics.Tests
{
    [TestFixture]
    public class DiagnosticsFrameworkTests
    {
        private Mock<ILogger<HealthCheckRunner>> _mockLogger;
        private HealthCheckRunner _healthCheckRunner;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<HealthCheckRunner>>();
            _healthCheckRunner = new HealthCheckRunner(_mockLogger.Object);
        }

        [Test]
        public async Task HealthCheckResult_Success_ShouldCreateValidResult()
        {
            // Arrange
            var checkName = "Test Check";
            var message = "Test message";
            var category = "Test Category";

            // Act
            var result = HealthCheckResult.Success(checkName, message, category);

            // Assert
            Assert.That(result.CheckName, Is.EqualTo(checkName));
            Assert.That(result.Message, Is.EqualTo(message));
            Assert.That(result.Category, Is.EqualTo(category));
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Ok));
            Assert.That(result.StatusIcon, Is.EqualTo("✅"));
        }

        [Test]
        public async Task HealthCheckResult_Warning_ShouldCreateValidResult()
        {
            // Arrange
            var checkName = "Warning Check";
            var message = "Warning message";
            var category = "Test Category";
            var recommendedAction = "Fix this issue";

            // Act
            var result = HealthCheckResult.Warning(checkName, message, category, recommendedAction);

            // Assert
            Assert.That(result.CheckName, Is.EqualTo(checkName));
            Assert.That(result.Message, Is.EqualTo(message));
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Warning));
            Assert.That(result.StatusIcon, Is.EqualTo("⚠️"));
            Assert.That(result.RecommendedAction, Is.EqualTo(recommendedAction));
        }

        [Test]
        public async Task HealthCheckResult_Failure_ShouldCreateValidResult()
        {
            // Arrange
            var checkName = "Failed Check";
            var message = "Failure message";
            var category = "Test Category";
            var exception = new System.Exception("Test exception");
            var recommendedAction = "Contact support";

            // Act
            var result = HealthCheckResult.Failure(checkName, message, category, exception, recommendedAction);

            // Assert
            Assert.That(result.CheckName, Is.EqualTo(checkName));
            Assert.That(result.Message, Is.EqualTo(message));
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Failed));
            Assert.That(result.StatusIcon, Is.EqualTo("❌"));
            Assert.That(result.Exception, Is.EqualTo(exception));
            Assert.That(result.RecommendedAction, Is.EqualTo(recommendedAction));
        }

        [Test]
        public async Task FixResult_Success_ShouldCreateValidResult()
        {
            // Arrange
            var actionName = "Test Fix";
            var message = "Fix successful";

            // Act
            var result = FixResult.Success(actionName, message);

            // Assert
            Assert.That(result.ActionName, Is.EqualTo(actionName));
            Assert.That(result.Message, Is.EqualTo(message));
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Exception, Is.Null);
        }

        [Test]
        public async Task FixResult_Failure_ShouldCreateValidResult()
        {
            // Arrange
            var actionName = "Test Fix";
            var message = "Fix failed";
            var exception = new System.Exception("Test exception");

            // Act
            var result = FixResult.Failure(actionName, message, exception);

            // Assert
            Assert.That(result.ActionName, Is.EqualTo(actionName));
            Assert.That(result.Message, Is.EqualTo(message));
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.Exception, Is.EqualTo(exception));
        }

        [Test]
        public void DiagnosticsOptions_QuickCheck_ShouldHaveCorrectSettings()
        {
            // Act
            var options = DiagnosticsOptions.QuickCheck();

            // Assert
            Assert.That(options.QuickCheckOnly, Is.True);
            Assert.That(options.AutoFix, Is.False);
            Assert.That(options.PerformanceTest, Is.False);
            Assert.That(options.VisualRegressionTest, Is.False);
            Assert.That(options.TimeoutMs, Is.EqualTo(10000));
        }

        [Test]
        public void DiagnosticsOptions_Comprehensive_ShouldHaveCorrectSettings()
        {
            // Act
            var options = DiagnosticsOptions.Comprehensive();

            // Assert
            Assert.That(options.ComprehensiveCheck, Is.True);
            Assert.That(options.AutoFix, Is.True);
            Assert.That(options.SafeFixOnly, Is.True);
            Assert.That(options.PerformanceTest, Is.True);
            Assert.That(options.VisualRegressionTest, Is.True);
            Assert.That(options.TimeoutMs, Is.EqualTo(300000));
        }

        [Test]
        public void HealthCheckResult_DurationText_ShouldFormatCorrectly()
        {
            // Arrange
            var result1 = new HealthCheckResult { Duration = TimeSpan.FromMilliseconds(500) };
            var result2 = new HealthCheckResult { Duration = TimeSpan.FromSeconds(2.5) };

            // Act & Assert
            Assert.That(result1.DurationText, Is.EqualTo("500ms"));
            Assert.That(result2.DurationText, Is.EqualTo("2.5s"));
        }

        [Test]
        public void HealthStatus_Values_ShouldBeCorrect()
        {
            // Assert
            Assert.That((int)HealthStatus.Ok, Is.EqualTo(0));
            Assert.That((int)HealthStatus.Warning, Is.EqualTo(1));
            Assert.That((int)HealthStatus.Failed, Is.EqualTo(2));
        }
    }

    [TestFixture]
    public class PerformanceTestFrameworkTests
    {
        private Mock<ILogger<PerformanceTestFramework>> _mockLogger;
        private Mock<HealthCheckRunner> _mockHealthCheckRunner;
        private PerformanceTestFramework _performanceFramework;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<PerformanceTestFramework>>();
            _mockHealthCheckRunner = new Mock<HealthCheckRunner>(Mock.Of<ILogger<HealthCheckRunner>>());
            _performanceFramework = new PerformanceTestFramework(_mockLogger.Object, _mockHealthCheckRunner.Object);
        }

        [Test]
        public async Task RunPerformanceTests_ShouldCompleteSuccessfully()
        {
            // Arrange
            _mockHealthCheckRunner.Setup(x => x.RunSingleCheckAsync(It.IsAny<string>()))
                .ReturnsAsync(HealthCheckResult.Success("Mock Check", "Success"));

            // Act
            var result = await _performanceFramework.RunPerformanceTestsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.Tests, Is.Not.Empty);
            Assert.That(result.TotalDuration, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task SavePerformanceResults_ShouldCreateFile()
        {
            // Arrange
            var results = new PerformanceTestResults
            {
                TestStartTime = DateTime.UtcNow,
                TestEndTime = DateTime.UtcNow.AddMinutes(1),
                IsSuccessful = true,
                Tests = new List<PerformanceTest>
                {
                    new PerformanceTest
                    {
                        TestName = "Sample Test",
                        Category = "Database",
                        IsSuccessful = true,
                        Duration = TimeSpan.FromMilliseconds(100)
                    }
                }
            };

            var tempFile = Path.GetTempFileName();

            // Act
            await _performanceFramework.SavePerformanceResultsAsync(results, tempFile);

            // Assert
            Assert.That(File.Exists(tempFile), Is.True);
            var content = await File.ReadAllTextAsync(tempFile);
            Assert.That(content, Contains.Substring("Sample Test"));
            Assert.That(content, Contains.Substring("Database"));

            // Cleanup
            File.Delete(tempFile);
        }
    }

    [TestFixture]
    public class VisualRegressionTestFrameworkTests
    {
        private Mock<ILogger<VisualRegressionTestFramework>> _mockLogger;
        private VisualRegressionTestFramework _visualFramework;
        private string _testBaselinesPath;
        private string _testOutputPath;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<VisualRegressionTestFramework>>();
            _testBaselinesPath = Path.Combine(Path.GetTempPath(), "TestBaselines");
            _testOutputPath = Path.Combine(Path.GetTempPath(), "TestOutput");
            
            _visualFramework = new VisualRegressionTestFramework(
                _mockLogger.Object, 
                _testBaselinesPath, 
                _testOutputPath);
        }

        [TearDown]
        public void TearDown()
        {
            // Cleanup test directories
            if (Directory.Exists(_testBaselinesPath))
                Directory.Delete(_testBaselinesPath, true);
            if (Directory.Exists(_testOutputPath))
                Directory.Delete(_testOutputPath, true);
        }

        [Test]
        public async Task RunVisualRegressionTests_ShouldCompleteSuccessfully()
        {
            // Act
            var result = await _visualFramework.RunVisualRegressionTestsAsync();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Tests, Is.Not.Empty);
            Assert.That(result.TotalDuration, Is.GreaterThan(TimeSpan.Zero));
        }

        [Test]
        public async Task GenerateVisualReport_ShouldCreateHtmlFile()
        {
            // Arrange
            var results = new VisualRegressionResults
            {
                TestStartTime = DateTime.UtcNow,
                TestEndTime = DateTime.UtcNow.AddMinutes(1),
                IsSuccessful = true,
                Tests = new List<VisualTest>
                {
                    new VisualTest
                    {
                        TestName = "MainWindow",
                        Description = "النافذة الرئيسية",
                        IsSuccessful = true,
                        Similarity = 0.95
                    }
                }
            };

            var reportPath = Path.GetTempFileName() + ".html";

            // Act
            await _visualFramework.GenerateVisualReportAsync(results, reportPath);

            // Assert
            Assert.That(File.Exists(reportPath), Is.True);
            var content = await File.ReadAllTextAsync(reportPath);
            Assert.That(content, Contains.Substring("تقرير الاختبارات البصرية"));
            Assert.That(content, Contains.Substring("MainWindow"));
            Assert.That(content, Contains.Substring("النافذة الرئيسية"));

            // Cleanup
            File.Delete(reportPath);
        }
    }

    /// <summary>
    /// Mock Health Check للاختبارات
    /// </summary>
    public class MockHealthCheck : IHealthCheck
    {
        public string Name { get; set; } = "Mock Check";
        public string Category { get; set; } = "Mock";
        public string Description { get; set; } = "Mock health check for testing";
        public int Priority { get; set; } = 1;
        public bool IsEnabled { get; set; } = true;

        public bool ShouldFail { get; set; } = false;
        public bool ShouldWarn { get; set; } = false;
        public TimeSpan SimulatedDuration { get; set; } = TimeSpan.FromMilliseconds(100);

        public async Task<HealthCheckResult> CheckAsync()
        {
            await Task.Delay(SimulatedDuration);

            if (ShouldFail)
            {
                return HealthCheckResult.Failure(Name, "Mock failure", Category);
            }
            
            if (ShouldWarn)
            {
                return HealthCheckResult.Warning(Name, "Mock warning", Category);
            }

            return HealthCheckResult.Success(Name, "Mock success", Category);
        }
    }

    /// <summary>
    /// Mock Fix Action للاختبارات
    /// </summary>
    public class MockFixAction : IFixAction
    {
        public string Name { get; set; } = "Mock Fix";
        public string Description { get; set; } = "Mock fix action for testing";
        public bool IsSafe { get; set; } = true;

        public bool CanFixValue { get; set; } = true;
        public bool ShouldFailFix { get; set; } = false;
        public TimeSpan SimulatedDuration { get; set; } = TimeSpan.FromMilliseconds(200);

        public async Task<bool> CanFixAsync()
        {
            await Task.Delay(10);
            return CanFixValue;
        }

        public async Task<FixResult> FixAsync()
        {
            await Task.Delay(SimulatedDuration);

            if (ShouldFailFix)
            {
                return FixResult.Failure(Name, "Mock fix failure");
            }

            return FixResult.Success(Name, "Mock fix success");
        }
    }

    [TestFixture]
    public class MockComponentsTests
    {
        [Test]
        public async Task MockHealthCheck_Success_ShouldReturnOk()
        {
            // Arrange
            var mockCheck = new MockHealthCheck
            {
                Name = "Test Mock",
                ShouldFail = false,
                ShouldWarn = false
            };

            // Act
            var result = await mockCheck.CheckAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Ok));
            Assert.That(result.CheckName, Is.EqualTo("Test Mock"));
            Assert.That(result.Message, Is.EqualTo("Mock success"));
        }

        [Test]
        public async Task MockHealthCheck_Warning_ShouldReturnWarning()
        {
            // Arrange
            var mockCheck = new MockHealthCheck
            {
                ShouldWarn = true
            };

            // Act
            var result = await mockCheck.CheckAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Warning));
            Assert.That(result.Message, Is.EqualTo("Mock warning"));
        }

        [Test]
        public async Task MockHealthCheck_Failure_ShouldReturnFailed()
        {
            // Arrange
            var mockCheck = new MockHealthCheck
            {
                ShouldFail = true
            };

            // Act
            var result = await mockCheck.CheckAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HealthStatus.Failed));
            Assert.That(result.Message, Is.EqualTo("Mock failure"));
        }

        [Test]
        public async Task MockFixAction_CanFix_ShouldReturnTrue()
        {
            // Arrange
            var mockFix = new MockFixAction
            {
                CanFixValue = true
            };

            // Act
            var canFix = await mockFix.CanFixAsync();

            // Assert
            Assert.That(canFix, Is.True);
        }

        [Test]
        public async Task MockFixAction_Fix_ShouldSucceed()
        {
            // Arrange
            var mockFix = new MockFixAction
            {
                Name = "Test Fix",
                ShouldFailFix = false
            };

            // Act
            var result = await mockFix.FixAsync();

            // Assert
            Assert.That(result.IsSuccessful, Is.True);
            Assert.That(result.ActionName, Is.EqualTo("Test Fix"));
            Assert.That(result.Message, Is.EqualTo("Mock fix success"));
        }

        [Test]
        public async Task MockFixAction_Fix_ShouldFail()
        {
            // Arrange
            var mockFix = new MockFixAction
            {
                Name = "Test Fix",
                ShouldFailFix = true
            };

            // Act
            var result = await mockFix.FixAsync();

            // Assert
            Assert.That(result.IsSuccessful, Is.False);
            Assert.That(result.ActionName, Is.EqualTo("Test Fix"));
            Assert.That(result.Message, Is.EqualTo("Mock fix failure"));
        }
    }
}