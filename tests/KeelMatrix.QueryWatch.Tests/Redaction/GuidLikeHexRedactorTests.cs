using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class GuidLikeHexRedactorTests {
        [Fact]
        public void Masks_16_To_31_Hex_With_Letters() {
            var r = new GuidLikeHexRedactor();
            var token = "abcdef123456abcdef123456"; // 24 chars with letters
            var input = $"/* {token} */";
            var red = r.Redact(input);
            red.Should().NotContain(token);
            red.Should().Contain("***");
        }

        [Fact]
        public void Does_Not_Mask_Purely_Numeric_16_Plus() {
            var r = new GuidLikeHexRedactor();
            var token = "1234567890123456"; // 16 digits, no letters -> should NOT mask
            var input = $"/* {token} */";
            var red = r.Redact(input);
            red.Should().Contain(token);
        }
    }
}
