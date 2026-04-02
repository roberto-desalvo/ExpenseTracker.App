using FluentAssertions;
using RDS.ExpenseTracker.DataImport.Business.Helpers;

namespace RDS.ExpenseTracker.DataImport.Business.Tests.Helpers.Utilities
{
    public class UtilitiesTests
    {
        [Fact]
        public void ContainsOne_WhenStringContainsOneOfTheValues_ShouldReturnTrue()
        {
            // Arrange
            var str = "Hello World";
            var values = new[] { "World", "Universe" };

            // Act
            var result = str.ContainsOne(values);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ContainsOne_WhenStringDoesNotContainAnyOfTheValues_ShouldReturnFalse()
        {
            // Arrange
            var str = "Hello World";
            var values = new[] { "Universe", "Galaxy" };

            // Act
            var result = str.ContainsOne(values);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ContainsOne_WhenIgnoreCaseIsTrue_ShouldReturnTrue()
        {
            // Arrange
            var str = "Hello World";
            var values = new[] { "world", "universe" };

            // Act
            var result = str.ContainsOne(true, values);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ParseToDecimal_WhenObjectIsNull_ShouldReturnNull()
        {
            // Arrange
            object obj = null;

            // Act
            var result = obj.ParseToDecimal();

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData("123,45")]
        [InlineData("123.45")]
        public void ParseToDecimal_WhenObjectIsValidDecimal_ShouldReturnDecimal(object input)
        {
            // Act
            var result = input.ParseToDecimal();

            // Assert
            result.Should().Be(123.45m);
        }

        [Fact]
        public void ParseToDecimal_WhenObjectIsInvalidDecimal_ShouldReturnNull()
        {
            // Arrange
            object obj = "invalid";

            // Act
            var result = obj.ParseToDecimal();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseToDateTime_WhenObjectIsNull_ShouldReturnNull()
        {
            // Arrange
            object obj = null;

            // Act
            var result = obj.ParseToDateTime();

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void ParseToDateTime_WhenObjectIsValidDateTime_ShouldReturnDateTime()
        {
            // Arrange
            object obj = "2021-01-01";

            // Act
            var result = obj.ParseToDateTime();

            // Assert
            result.Should().Be(new DateTime(2021, 1, 1));
        }

        [Fact]
        public void ParseToDateTime_WhenObjectIsInvalidDateTime_ShouldReturnNull()
        {
            // Arrange
            object obj = "invalid";

            // Act
            var result = obj.ParseToDateTime();

            // Assert
            result.Should().BeNull();
        }
    }
}
