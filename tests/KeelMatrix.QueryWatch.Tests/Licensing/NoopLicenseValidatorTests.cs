using FluentAssertions;
using KeelMatrix.QueryWatch.Licensing;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Licensing {
    public class NoopLicenseValidatorTests {
        [Theory]
        [InlineData("")]
        [InlineData("ABC-123-DEF")]
        [InlineData("some-random-key")]
        public void IsValid_Always_Returns_True(string key) {
            NoopLicenseValidator validator = new();
            _ = validator.IsValid(key).Should().BeTrue();
        }
    }
}
