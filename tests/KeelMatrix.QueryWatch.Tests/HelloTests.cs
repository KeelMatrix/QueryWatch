using Xunit;
using FluentAssertions;
using KeelMatrix.QueryWatch;

namespace KeelMatrix.QueryWatch.Tests
{
    /// <summary>
    /// Sample unit tests demonstrating how to test the Hello class.
    /// </summary>
    public class HelloTests
    {
        [Fact]
        public void Greet_ReturnsMessageContainingName()
        {
            // Arrange
            var hello = new Hello();
            var name = "Tester";

            // Act
            var result = hello.Greet(name);

            // Assert
            result.Should().Contain(name);
        }
    }
}