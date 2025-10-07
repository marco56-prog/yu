using System;
using Xunit;
using AccountingSystem.Business.Exceptions;
using AccountingSystem.Business.Helpers;
using AccountingSystem.Models;

namespace AccountingSystem.Tests
{
    public class ValidationHelpersTests
    {
        [Fact]
        public void EnsureNotNull_WithNullValue_ThrowsArgumentNullException()
        {
            // Arrange
            string? value = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                ValidationHelpers.EnsureNotNull(value, "testParam"));
        }

        [Fact]
        public void EnsureNotNull_WithValidValue_ReturnsValue()
        {
            // Arrange
            var value = "test";

            // Act
            var result = ValidationHelpers.EnsureNotNull(value, "testParam");

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public void EnsureNotNullOrEmpty_WithEmptyString_ThrowsArgumentException()
        {
            // Arrange
            var value = "";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ValidationHelpers.EnsureNotNullOrEmpty(value, "testParam"));
        }

        [Fact]
        public void EnsureNotNullOrWhiteSpace_WithWhiteSpace_ThrowsArgumentException()
        {
            // Arrange
            var value = "   ";

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ValidationHelpers.EnsureNotNullOrWhiteSpace(value, "testParam"));
        }

        [Fact]
        public void EnsureInRange_WithValueInRange_ReturnsValue()
        {
            // Arrange
            var value = 5;

            // Act
            var result = ValidationHelpers.EnsureInRange(value, 1, 10, "testParam");

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public void EnsureInRange_WithValueOutOfRange_ThrowsArgumentOutOfRangeException()
        {
            // Arrange
            var value = 15;

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => 
                ValidationHelpers.EnsureInRange(value, 1, 10, "testParam"));
        }

        [Fact]
        public void EnsurePositive_WithPositiveValue_ReturnsValue()
        {
            // Arrange
            decimal value = 10.5m;

            // Act
            var result = ValidationHelpers.EnsurePositive(value, "testParam");

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public void EnsurePositive_WithZero_ThrowsArgumentException()
        {
            // Arrange
            decimal value = 0;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ValidationHelpers.EnsurePositive(value, "testParam"));
        }

        [Fact]
        public void EnsureNonNegative_WithZero_ReturnsValue()
        {
            // Arrange
            decimal value = 0;

            // Act
            var result = ValidationHelpers.EnsureNonNegative(value, "testParam");

            // Assert
            Assert.Equal(value, result);
        }

        [Fact]
        public void EnsureNonNegative_WithNegative_ThrowsArgumentException()
        {
            // Arrange
            decimal value = -5;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ValidationHelpers.EnsureNonNegative(value, "testParam"));
        }

        [Fact]
        public void EnsureValidId_WithPositiveId_ReturnsId()
        {
            // Arrange
            var id = 123;

            // Act
            var result = ValidationHelpers.EnsureValidId(id, "testId");

            // Assert
            Assert.Equal(id, result);
        }

        [Fact]
        public void EnsureValidId_WithZero_ThrowsArgumentException()
        {
            // Arrange
            var id = 0;

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                ValidationHelpers.EnsureValidId(id, "testId"));
        }

        [Fact]
        public void SanitizeString_WithWhitespace_TrimsWhitespace()
        {
            // Arrange
            var value = "  test  ";

            // Act
            var result = ValidationHelpers.SanitizeString(value);

            // Assert
            Assert.Equal("test", result);
        }

        [Fact]
        public void SanitizeStringNotNull_WithNull_ReturnsEmptyString()
        {
            // Arrange
            string? value = null;

            // Act
            var result = ValidationHelpers.SanitizeStringNotNull(value);

            // Assert
            Assert.Equal(string.Empty, result);
        }
    }

    public class BusinessExceptionsTests
    {
        [Fact]
        public void EntityNotFoundException_CreatesExceptionWithTypeAndId()
        {
            // Arrange & Act
            var exception = new EntityNotFoundException(typeof(Product), 123);

            // Assert
            Assert.Equal("ENTITY_NOT_FOUND", exception.ErrorCode);
            Assert.Equal(typeof(Product), exception.EntityType);
            Assert.Equal(123, exception.EntityId);
            Assert.Contains("Product", exception.Message);
            Assert.Contains("123", exception.Message);
        }

        [Fact]
        public void ValidationException_CreatesExceptionWithMessage()
        {
            // Arrange & Act
            var exception = new ValidationException("حقل مطلوب");

            // Assert
            Assert.Equal("VALIDATION_ERROR", exception.ErrorCode);
            Assert.Single(exception.ValidationErrors);
            Assert.Equal("حقل مطلوب", exception.ValidationErrors[0]);
        }

        [Fact]
        public void InsufficientStockException_ContainsProductInfo()
        {
            // Arrange & Act
            var exception = new InsufficientStockException(1, "منتج تجريبي", 100, 50);

            // Assert
            Assert.Equal("INSUFFICIENT_STOCK", exception.ErrorCode);
            Assert.Equal(1, exception.ProductId);
            Assert.Equal("منتج تجريبي", exception.ProductName);
            Assert.Equal(100, exception.RequestedQuantity);
            Assert.Equal(50, exception.AvailableQuantity);
            Assert.Contains("منتج تجريبي", exception.Message);
        }

        [Fact]
        public void BusinessRuleViolationException_ContainsRuleName()
        {
            // Arrange & Act
            var exception = new BusinessRuleViolationException("MaxDiscount", "الخصم يجب ألا يتجاوز 50%");

            // Assert
            Assert.Equal("BUSINESS_RULE_VIOLATION", exception.ErrorCode);
            Assert.Equal("MaxDiscount", exception.RuleName);
            Assert.Contains("50%", exception.Message);
        }

        [Fact]
        public void DuplicateEntityException_ContainsFieldInfo()
        {
            // Arrange & Act
            var exception = new DuplicateEntityException("العميل", "البريد الإلكتروني", "test@example.com");

            // Assert
            Assert.Equal("DUPLICATE_ENTITY", exception.ErrorCode);
            Assert.Equal("البريد الإلكتروني", exception.DuplicateField);
            Assert.Equal("test@example.com", exception.DuplicateValue);
        }
    }
}
