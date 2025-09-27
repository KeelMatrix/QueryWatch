using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class UuidNoDashRedactorTests {
        [Fact]
        public void Masks_32_Hex_With_Letters() {
            var r = new UuidNoDashRedactor();
            var id = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"; // 32 chars with letters
            r.Redact(id).Should().Be("***");
        }

        [Fact]
        public void Does_Not_Mask_32_Digits_Only() {
            var r = new UuidNoDashRedactor();
            var digits = "12345678901234567890123456789012";
            r.Redact(digits).Should().Be(digits);
        }
    }
}
