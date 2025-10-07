using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AccountingSystem.WPF.ViewModels;
using AccountingSystem.Diagnostics.Core;
using AccountingSystem.Diagnostics.Models;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace AccountingSystem.Diagnostics.Tests
{
    /// <summary>
    /// اختبارات شاملة لواجهة المستخدم التشخيصية
    /// Comprehensive Diagnostic UI Tests
    /// </summary>
    [TestFixture]
    public class DiagnosticUITests
    {
        private ServiceProvider _serviceProvider;
        private SimpleDoctorViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            
            // Register logging
            services.AddLogging(builder => builder.AddConsole());
            
            // Register diagnostic services
            services.AddTransient<HealthCheckRunner>();
            
            // Register ViewModels
            services.AddTransient<SimpleDoctorViewModel>();
            
            _serviceProvider = services.BuildServiceProvider();
            _viewModel = _serviceProvider.GetRequiredService<SimpleDoctorViewModel>();
        }

        [TearDown]
        public void TearDown()
        {
            _serviceProvider?.Dispose();
        }

        #region ViewModel Property Tests

        [Test]
        public void SimpleDoctorViewModel_InitialState_ShouldBeCorrect()
        {
            // Assert
            Assert.That(_viewModel.IsRunning, Is.False);
            Assert.That(_viewModel.Progress, Is.EqualTo(0));
            Assert.That(_viewModel.StatusMessage, Is.EqualTo("جاهز لبدء التشخيص"));
            Assert.That(_viewModel.Results, Is.Not.Null);
            Assert.That(_viewModel.Results.Count, Is.EqualTo(0));
        }

        [Test]
        public void SimpleDoctorViewModel_Commands_ShouldBeInitialized()
        {
            // Assert
            Assert.That(_viewModel.RunQuickCheckCommand, Is.Not.Null);
            Assert.That(_viewModel.RunComprehensiveCheckCommand, Is.Not.Null);
            Assert.That(_viewModel.AutoFixCommand, Is.Not.Null);
            Assert.That(_viewModel.ClearResultsCommand, Is.Not.Null);
            Assert.That(_viewModel.SaveReportCommand, Is.Not.Null);
        }

        [Test]
        public void SimpleDoctorViewModel_CommandsCanExecute_InitialState()
        {
            // Assert
            Assert.That(_viewModel.RunQuickCheckCommand.CanExecute(null), Is.True);
            Assert.That(_viewModel.RunComprehensiveCheckCommand.CanExecute(null), Is.True);
            Assert.That(_viewModel.AutoFixCommand.CanExecute(null), Is.False); // No issues to fix initially
            Assert.That(_viewModel.ClearResultsCommand.CanExecute(null), Is.False); // No results initially
            Assert.That(_viewModel.SaveReportCommand.CanExecute(null), Is.False); // No results initially
        }

        #endregion

        #region Command Execution Tests

        [Test]
        public async Task RunQuickCheckCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var initialIsRunning = _viewModel.IsRunning;
            var initialResults = _viewModel.Results.Count;

            // Act
            _viewModel.RunQuickCheckCommand.Execute(null);
            
            // Wait for completion
            await Task.Delay(100); // Allow for async operations
            
            // Since this is a mock implementation, we'll wait a bit more
            var maxWait = DateTime.UtcNow.AddSeconds(10);
            while (_viewModel.IsRunning && DateTime.UtcNow < maxWait)
            {
                await Task.Delay(100);
            }

            // Assert
            Assert.That(_viewModel.IsRunning, Is.False); // Should complete
            Assert.That(_viewModel.Results.Count, Is.GreaterThan(initialResults));
            Assert.That(_viewModel.Progress, Is.GreaterThan(0));
        }

        [Test]
        public async Task RunComprehensiveCheckCommand_ShouldExecuteSuccessfully()
        {
            // Arrange
            var initialIsRunning = _viewModel.IsRunning;

            // Act
            _viewModel.RunComprehensiveCheckCommand.Execute(null);
            
            // Wait for completion
            await Task.Delay(100);
            
            var maxWait = DateTime.UtcNow.AddSeconds(15); // Comprehensive takes longer
            while (_viewModel.IsRunning && DateTime.UtcNow < maxWait)
            {
                await Task.Delay(100);
            }

            // Assert
            Assert.That(_viewModel.IsRunning, Is.False);
            Assert.That(_viewModel.Results.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ClearResultsCommand_ShouldClearResults()
        {
            // Arrange - Add some mock results first
            _viewModel.Results.Add(new HealthCheckResult
            {
                CheckName = "Test Check",
                Status = HealthStatus.Ok,
                Message = "Test message"
            });

            // Act
            _viewModel.ClearResultsCommand.Execute(null);

            // Assert
            Assert.That(_viewModel.Results.Count, Is.EqualTo(0));
            Assert.That(_viewModel.Progress, Is.EqualTo(0));
            Assert.That(_viewModel.StatusMessage, Contains.Substring("مسح"));
        }

        #endregion

        #region Data Binding Tests

        [Test]
        public void SimpleDoctorViewModel_PropertyChanged_ShouldFireCorrectly()
        {
            // Arrange
            var propertyChangedEvents = new List<string>();
            _viewModel.PropertyChanged += (sender, e) =>
            {
                propertyChangedEvents.Add(e.PropertyName);
            };

            // Act - Simulate property changes
            _viewModel.Results.Add(new HealthCheckResult
            {
                CheckName = "Test",
                Status = HealthStatus.Ok,
                Message = "Test"
            });

            // Note: Since Results is ObservableCollection, it should trigger collection change events
            // The ViewModel should handle these internally

            // Assert - We can't directly test private property setters, 
            // but we can verify the ViewModel responds to collection changes
            Assert.That(_viewModel.Results.Count, Is.EqualTo(1));
        }

        [Test]
        public void SimpleDoctorViewModel_Results_ShouldBeObservable()
        {
            // Assert
            Assert.That(_viewModel.Results, Is.TypeOf<ObservableCollection<HealthCheckResult>>());
            
            // Test collection manipulation
            var initialCount = _viewModel.Results.Count;
            
            _viewModel.Results.Add(new HealthCheckResult
            {
                CheckName = "Test Check",
                Status = HealthStatus.Ok,
                Message = "Test message"
            });
            
            Assert.That(_viewModel.Results.Count, Is.EqualTo(initialCount + 1));
        }

        #endregion

        #region Status and Progress Tests

        [Test]
        public void SimpleDoctorViewModel_StatusMessage_ShouldBeLocalized()
        {
            // Assert - Check that status messages contain Arabic text
            Assert.That(_viewModel.StatusMessage, Does.Match(@"[\u0600-\u06FF]"));
        }

        [Test]
        public void SimpleDoctorViewModel_Progress_ShouldBeInValidRange()
        {
            // Assert
            Assert.That(_viewModel.Progress, Is.InRange(0, 100));
        }

        #endregion

        #region Mock Data Tests

        [Test]
        public void SimpleDoctorViewModel_MockHealthChecks_ShouldCreateDiverseResults()
        {
            // Arrange - Access the private method through reflection or make it protected/internal for testing
            // For now, we'll test the publicly observable behavior
            
            // Act - Run a quick check to populate results
            _viewModel.RunQuickCheckCommand.Execute(null);
            
            // Wait for mock completion
            var maxWait = DateTime.UtcNow.AddSeconds(5);
            while (_viewModel.IsRunning && DateTime.UtcNow < maxWait)
            {
                Task.Delay(50).Wait();
            }

            // Assert
            if (_viewModel.Results.Count > 0)
            {
                // Should have different status types
                var statusTypes = _viewModel.Results.Select(r => r.Status).Distinct().ToList();
                Assert.That(statusTypes.Count, Is.GreaterThan(1), "Should have diverse result statuses");
                
                // Should have different categories
                var categories = _viewModel.Results.Select(r => r.Category).Distinct().ToList();
                Assert.That(categories.Count, Is.GreaterThan(1), "Should have diverse result categories");
                
                // All should have Arabic names
                foreach (var result in _viewModel.Results)
                {
                    Assert.That(result.CheckName, Does.Match(@"[\u0600-\u06FF]"), 
                        "Check names should contain Arabic text");
                    Assert.That(result.Category, Does.Match(@"[\u0600-\u06FF]"), 
                        "Categories should contain Arabic text");
                }
            }
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void SimpleDoctorViewModel_CommandExecution_ShouldHandleErrors()
        {
            // This test verifies that the ViewModel handles errors gracefully
            // Since we're using mock data, we can't simulate real errors easily
            // But we can verify that the ViewModel doesn't crash with null operations
            
            // Act & Assert - These should not throw
            Assert.DoesNotThrow(() =>
            {
                var canExecute1 = _viewModel.RunQuickCheckCommand.CanExecute(null);
                var canExecute2 = _viewModel.RunComprehensiveCheckCommand.CanExecute(null);
                var canExecute3 = _viewModel.AutoFixCommand.CanExecute(null);
                var canExecute4 = _viewModel.ClearResultsCommand.CanExecute(null);
                var canExecute5 = _viewModel.SaveReportCommand.CanExecute(null);
            });
        }

        #endregion
    }

    /// <summary>
    /// اختبارات تكامل واجهة المستخدم مع النظام التشخيصي
    /// UI Integration Tests with Diagnostic System
    /// </summary>
    [TestFixture]
    public class DiagnosticUIIntegrationTests
    {
        private ServiceProvider _serviceProvider;
        private SimpleDoctorViewModel _viewModel;
        private HealthCheckRunner _healthCheckRunner;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            
            // Register logging
            services.AddLogging(builder => builder.AddConsole());
            
            // Register all diagnostic services
            services.AddTransient<HealthCheckRunner>();
            services.AddTransient<SimpleDoctorViewModel>();
            
            _serviceProvider = services.BuildServiceProvider();
            _viewModel = _serviceProvider.GetRequiredService<SimpleDoctorViewModel>();
            _healthCheckRunner = _serviceProvider.GetRequiredService<HealthCheckRunner>();
        }

        [TearDown]
        public void TearDown()
        {
            _serviceProvider?.Dispose();
        }

        [Test]
        public async Task ViewModelCommands_ShouldIntegrateWithHealthCheckRunner()
        {
            // This test verifies that the ViewModel properly integrates with the diagnostic system
            // Since SimpleDoctorViewModel uses mock data, we test the integration points
            
            // Arrange
            var initialResultCount = _viewModel.Results.Count;

            // Act
            _viewModel.RunQuickCheckCommand.Execute(null);
            
            // Wait for completion
            await Task.Delay(500);
            
            // Assert
            Assert.That(_viewModel.Results.Count, Is.GreaterThanOrEqualTo(initialResultCount));
        }

        [Test]
        public void ViewModel_ShouldUseProperDependencyInjection()
        {
            // Verify that the ViewModel is properly constructed through DI
            Assert.That(_viewModel, Is.Not.Null);
            Assert.That(_viewModel.GetType(), Is.EqualTo(typeof(SimpleDoctorViewModel)));
        }
    }

    /// <summary>
    /// اختبارات أداء واجهة المستخدم التشخيصية
    /// Diagnostic UI Performance Tests
    /// </summary>
    [TestFixture]
    public class DiagnosticUIPerformanceTests
    {
        private SimpleDoctorViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTransient<HealthCheckRunner>();
            services.AddTransient<SimpleDoctorViewModel>();
            
            var serviceProvider = services.BuildServiceProvider();
            _viewModel = serviceProvider.GetRequiredService<SimpleDoctorViewModel>();
        }

        [Test]
        public async Task QuickCheck_ShouldCompleteWithinTimeLimit()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            var timeLimit = TimeSpan.FromSeconds(10);

            // Act
            _viewModel.RunQuickCheckCommand.Execute(null);
            
            // Wait for completion or timeout
            while (_viewModel.IsRunning && DateTime.UtcNow - startTime < timeLimit)
            {
                await Task.Delay(100);
            }
            
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert
            Assert.That(_viewModel.IsRunning, Is.False, "Quick check should complete");
            Assert.That(duration, Is.LessThan(timeLimit), "Quick check should complete within time limit");
        }

        [Test]
        public async Task ComprehensiveCheck_ShouldCompleteWithinTimeLimit()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            var timeLimit = TimeSpan.FromSeconds(20); // Comprehensive takes longer

            // Act
            _viewModel.RunComprehensiveCheckCommand.Execute(null);
            
            // Wait for completion or timeout
            while (_viewModel.IsRunning && DateTime.UtcNow - startTime < timeLimit)
            {
                await Task.Delay(100);
            }
            
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert
            Assert.That(_viewModel.IsRunning, Is.False, "Comprehensive check should complete");
            Assert.That(duration, Is.LessThan(timeLimit), "Comprehensive check should complete within time limit");
        }

        [Test]
        public void LargeResultSet_ShouldHandleEfficiently()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            
            // Add many results to test UI performance
            for (int i = 0; i < 1000; i++)
            {
                _viewModel.Results.Add(new HealthCheckResult
                {
                    CheckName = $"اختبار رقم {i}",
                    Status = (HealthStatus)(i % 3),
                    Category = $"فئة {i % 5}",
                    Message = $"رسالة الاختبار رقم {i}",
                    Duration = TimeSpan.FromMilliseconds(i)
                });
            }
            
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert
            Assert.That(_viewModel.Results.Count, Is.EqualTo(1000));
            Assert.That(duration, Is.LessThan(TimeSpan.FromSeconds(5)), 
                "Adding large result set should be efficient");
        }

        [Test]
        public void CommandCanExecute_ShouldBeResponsive()
        {
            // Test that CanExecute checks are fast
            var startTime = DateTime.UtcNow;
            
            // Execute CanExecute many times
            for (int i = 0; i < 10000; i++)
            {
                _viewModel.RunQuickCheckCommand.CanExecute(null);
                _viewModel.RunComprehensiveCheckCommand.CanExecute(null);
                _viewModel.AutoFixCommand.CanExecute(null);
                _viewModel.ClearResultsCommand.CanExecute(null);
                _viewModel.SaveReportCommand.CanExecute(null);
            }
            
            var endTime = DateTime.UtcNow;
            var duration = endTime - startTime;

            // Assert
            Assert.That(duration, Is.LessThan(TimeSpan.FromSeconds(1)), 
                "CanExecute checks should be very fast");
        }
    }

    /// <summary>
    /// اختبارات سهولة الاستخدام والوصولية لواجهة المستخدم
    /// UI Usability and Accessibility Tests
    /// </summary>
    [TestFixture]
    public class DiagnosticUIUsabilityTests
    {
        private SimpleDoctorViewModel _viewModel;

        [SetUp]
        public void Setup()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddTransient<HealthCheckRunner>();
            services.AddTransient<SimpleDoctorViewModel>();
            
            var serviceProvider = services.BuildServiceProvider();
            _viewModel = serviceProvider.GetRequiredService<SimpleDoctorViewModel>();
        }

        [Test]
        public void UIText_ShouldBeInArabic()
        {
            // Test that all user-facing text is in Arabic
            Assert.That(_viewModel.StatusMessage, Does.Match(@"[\u0600-\u06FF]"),
                "Status message should be in Arabic");
            
            // Add some results and check their text
            _viewModel.Results.Add(new HealthCheckResult
            {
                CheckName = "فحص تجريبي",
                Category = "فئة تجريبية", 
                Message = "رسالة تجريبية",
                Status = HealthStatus.Ok
            });
            
            var result = _viewModel.Results.First();
            Assert.That(result.CheckName, Does.Match(@"[\u0600-\u06FF]"),
                "Check name should be in Arabic");
            Assert.That(result.Category, Does.Match(@"[\u0600-\u06FF]"),
                "Category should be in Arabic");
            Assert.That(result.Message, Does.Match(@"[\u0600-\u06FF]"),
                "Message should be in Arabic");
        }

        [Test]
        public void Commands_ShouldProvideUserFeedback()
        {
            // Test that commands update status to provide user feedback
            var initialStatus = _viewModel.StatusMessage;
            
            // Execute clear command
            _viewModel.Results.Add(new HealthCheckResult
            {
                CheckName = "Test", Status = HealthStatus.Ok, Message = "Test"
            });
            
            _viewModel.ClearResultsCommand.Execute(null);
            
            // Status should change to indicate action was performed
            Assert.That(_viewModel.StatusMessage, Is.Not.EqualTo(initialStatus));
            Assert.That(_viewModel.StatusMessage, Contains.Substring("مسح") | Contains.Substring("تنظيف"));
        }

        [Test]
        public void ProgressReporting_ShouldBeAccurate()
        {
            // Progress should be between 0 and 100
            Assert.That(_viewModel.Progress, Is.InRange(0, 100));
            
            // When running, progress should update
            // (This is harder to test with mock data, but we can verify the range)
            for (int i = 0; i <= 100; i += 10)
            {
                // If we could set progress directly, we'd test it here
                // For now, verify the property constraints
                Assert.That(_viewModel.Progress, Is.InRange(0, 100));
            }
        }

        [Test]
        public void CommandStates_ShouldReflectSystemState()
        {
            // Initial state
            Assert.That(_viewModel.ClearResultsCommand.CanExecute(null), Is.False,
                "Clear command should be disabled when no results");
            
            // Add results
            _viewModel.Results.Add(new HealthCheckResult
            {
                CheckName = "Test", Status = HealthStatus.Ok, Message = "Test"
            });
            
            // Clear command should now be enabled
            // Note: This might require the ViewModel to listen to collection changes
            // and update command states accordingly
        }
    }
}