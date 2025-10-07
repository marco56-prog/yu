using AccountingSystem.Business;
using Xunit;

namespace AccountingSystem.Tests
{
    public class ValidationServiceTests
    {
        [Fact]
        public void IsValidPhone_ValidEgyptianLandline_ReturnsTrue()
        {
            // Arrange
            var phoneNumber = "0882345678"; // Assiut city code (088)

            // Act
            var result = ValidationService.IsValidPhone(phoneNumber);

            // Assert
            Assert.True(result);
        }
    }
}