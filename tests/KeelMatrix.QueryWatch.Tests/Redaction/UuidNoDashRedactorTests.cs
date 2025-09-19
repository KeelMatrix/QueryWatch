using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class UuidNoDashRedactorTests {
        [Fact]
        public void Masks_32_Hex_With_Letters() {
            var r = new UuidNoDashRedactor();
            var token = "1234567890abcdef1234567890ABCDEF"; // 32 hex, contains letters
            var input = $"/* {token} */";
            var red = r.Redact(input);
            red.Should().NotContain(token);
            red.Should().Contain("***");
        }

        [Fact]
        public void Does_Not_Mask_32_Digits_Only() {
            var r = new UuidNoDashRedactor();
            var numeric = "12345678901234567890123456789012"; // 32 digits, no letters
            var input = $"/* {numeric} */";
            r.Redact(input).Should().Contain(numeric);
        }
    }
}
