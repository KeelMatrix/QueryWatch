using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class GuidLikeHexRedactorTests {
        [Fact]
        public void Masks_16_to_31_Hex_With_Letters() {
            GuidLikeHexRedactor r = new();
            const string token = "0123456789abcDEF"; // 16 chars with letters
            _ = r.Redact(token).Should().Be("***");
        }

        [Fact]
        public void Does_Not_Mask_Purely_Numeric_Long_Ids() {
            GuidLikeHexRedactor r = new();
            const string token = "12345678901234567890"; // 20 digits, no letters
            _ = r.Redact(token).Should().Be(token);
        }
    }
}
