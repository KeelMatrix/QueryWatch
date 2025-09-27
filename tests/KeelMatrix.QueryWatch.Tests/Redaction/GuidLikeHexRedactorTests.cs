using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class GuidLikeHexRedactorTests {
        [Fact]
        public void Masks_16_to_31_Hex_With_Letters() {
            var r = new GuidLikeHexRedactor();
            var token = "0123456789abcDEF"; // 16 chars with letters
            r.Redact(token).Should().Be("***");
        }

        [Fact]
        public void Does_Not_Mask_Purely_Numeric_Long_Ids() {
            var r = new GuidLikeHexRedactor();
            var token = "12345678901234567890"; // 20 digits, no letters
            r.Redact(token).Should().Be(token);
        }
    }
}
