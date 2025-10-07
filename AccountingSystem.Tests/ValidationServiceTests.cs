using Xunit;
using AccountingSystem.Business;

namespace AccountingSystem.Tests
{
    public class ValidationServiceTests
    {
        [Theory]
        [InlineData("0882345678")] // Assiut (3-digit code)
        [InlineData("0223456789")] // Cairo (2-digit code)
        [InlineData("01012345678")] // Valid Mobile
        public void IsValidPhone_ValidEgyptianNumbers_ReturnsTrue(string phoneNumber)
        {
            // Act
            var result = ValidationService.IsValidPhone(phoneNumber);

            // Assert
            Assert.True(result, $"Expected {phoneNumber} to be a valid Egyptian phone number.");
        }

        [Theory]
        [InlineData("0882345")] // Too short
        [InlineData("1234567890")] // Doesn't start with 0
        [InlineData("01312345678")] // Invalid mobile prefix
        public void IsValidPhone_InvalidEgyptianNumbers_ReturnsFalse(string phoneNumber)
        {
            // Act
            var result = ValidationService.IsValidPhone(phoneNumber);

            // Assert
            Assert.False(result, $"Expected {phoneNumber} to be an invalid Egyptian phone number.");
        }
    }
}