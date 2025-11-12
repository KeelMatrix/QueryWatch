using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class LongHexTokenRedactorTests {
        [Fact]
        public void Masks_32_Or_More_Hex() {
            LongHexTokenRedactor r = new();
            string token = new('a', 32);
            _ = r.Redact(token).Should().Be("***");
            _ = r.Redact(token + "b").Should().Be("***");
        }

        [Fact]
        public void Does_Not_Mask_31_Hex() {
            LongHexTokenRedactor r = new();
            string token = new('a', 31);
            _ = r.Redact(token).Should().Be(token);
        }
    }
}
