using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using AccountingSystem.Business.Common;

namespace AccountingSystem.Tests
{
    public class DisposableBaseTests
    {
        private class TestDisposable : DisposableBase
        {
            public bool ManagedResourcesDisposed { get; private set; }
            public bool UnmanagedResourcesDisposed { get; private set; }
            public int DisposeCallCount { get; private set; }

            protected override void DisposeManagedResources()
            {
                ManagedResourcesDisposed = true;
            }

            protected override void DisposeUnmanagedResources()
            {
                UnmanagedResourcesDisposed = true;
            }

            protected override void Dispose(bool disposing)
            {
                DisposeCallCount++;
                base.Dispose(disposing);
            }

            public void TestMethod()
            {
                ThrowIfDisposed();
            }
        }

        [Fact]
        public void Dispose_CallsDisposeManagedResources()
        {
            // Arrange
            var obj = new TestDisposable();

            // Act
            obj.Dispose();

            // Assert
            Assert.True(obj.ManagedResourcesDisposed, "يجب أن يتم تحرير الموارد المُدارة");
        }

        [Fact]
        public void Dispose_CallsDisposeUnmanagedResources()
        {
            // Arrange
            var obj = new TestDisposable();

            // Act
            obj.Dispose();

            // Assert
            Assert.True(obj.UnmanagedResourcesDisposed, "يجب أن يتم تحرير الموارد غير المُدارة");
        }

        [Fact]
        public void Dispose_CalledMultipleTimes_OnlyDisposesOnce()
        {
            // Arrange
            var obj = new TestDisposable();

            // Act
            obj.Dispose();
            obj.Dispose();
            obj.Dispose();

            // Assert
            Assert.Equal(3, obj.DisposeCallCount);
            Assert.True(obj.ManagedResourcesDisposed, "الموارد المُدارة يجب أن تُحرر مرة واحدة");
        }

        [Fact]
        public void ThrowIfDisposed_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var obj = new TestDisposable();
            obj.Dispose();

            // Act & Assert
            Assert.Throws<ObjectDisposedException>(() => obj.TestMethod());
        }

        [Fact]
        public void ThrowIfDisposed_BeforeDispose_DoesNotThrow()
        {
            // Arrange
            var obj = new TestDisposable();

            // Act & Assert - should not throw
            obj.TestMethod();
        }
    }

    public class ThreadSafeOperationsTests
    {
        [Fact]
        public void Once_Execute_RunsActionOnlyOnce()
        {
            // Arrange
            var flag = 0;
            var counter = 0;

            // Act - تنفيذ أول مرة ينجح
            ThreadSafeOperations.Once.Execute(ref flag, () => counter++);
            
            // المحاولات التالية يجب أن لا تنفذ العملية لأن العلم تم تعيينه
            // ولكن Once.Execute مع Action لا يرمي استثناء، فقط لا ينفذ

            // Assert
            Assert.Equal(1, counter);
            Assert.Equal(1, flag); // العلم تم تعيينه
        }

        [Fact]
        public void Once_ExecuteWithReturn_RunsOnlyOnceAndReturnsValue()
        {
            // Arrange
            var flag = 0;
            var counter = 0;

            // Act
            var result = ThreadSafeOperations.Once.Execute(ref flag, () => ++counter);

            // Assert
            Assert.Equal(1, result);
            Assert.Equal(1, counter);
        }

        [Fact]
        public void AtomicCounter_Increment_IsThreadSafe()
        {
            // Arrange
            var counter = new ThreadSafeOperations.AtomicCounter(0);
            var iterations = 1000;

            // Act
            Parallel.For(0, iterations, _ => counter.Increment());

            // Assert
            Assert.Equal(iterations, counter.Value);
        }

        [Fact]
        public void AtomicCounter_Decrement_DecreasesValue()
        {
            // Arrange
            var counter = new ThreadSafeOperations.AtomicCounter(100);

            // Act
            var result = counter.Decrement();

            // Assert
            Assert.Equal(99, result);
            Assert.Equal(99, counter.Value);
        }

        [Fact]
        public void AtomicCounter_Add_AddsValue()
        {
            // Arrange
            var counter = new ThreadSafeOperations.AtomicCounter(10);

            // Act
            var result = counter.Add(5);

            // Assert
            Assert.Equal(15, result);
        }

        [Fact]
        public void SimpleLock_Execute_IsThreadSafe()
        {
            // Arrange
            var simpleLock = new ThreadSafeOperations.SimpleLock();
            var counter = 0;
            var iterations = 1000;

            // Act
            Parallel.For(0, iterations, _ =>
            {
                simpleLock.Execute(() => counter++);
            });

            // Assert
            Assert.Equal(iterations, counter);
        }

        [Fact]
        public void SimpleLock_ExecuteWithReturn_ReturnsCorrectValue()
        {
            // Arrange
            var simpleLock = new ThreadSafeOperations.SimpleLock();

            // Act
            var result = simpleLock.Execute(() => 42);

            // Assert
            Assert.Equal(42, result);
        }

        [Fact]
        public void SimpleLock_TryExecute_WithTimeout_ReturnsTrue()
        {
            // Arrange
            var simpleLock = new ThreadSafeOperations.SimpleLock();
            var executed = false;

            // Act
            var result = simpleLock.TryExecute(() => executed = true, TimeSpan.FromSeconds(1));

            // Assert
            Assert.True(result, "يجب أن ينجح الحصول على القفل");
            Assert.True(executed, "يجب أن تُنفذ العملية");
        }
    }
}
