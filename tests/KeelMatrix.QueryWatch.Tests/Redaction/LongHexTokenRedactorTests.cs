using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class LongHexTokenRedactorTests {
        [Fact]
        public void Masks_32_Or_More_Hex() {
            var r = new LongHexTokenRedactor();
            var token = new string('a', 32);
            r.Redact(token).Should().Be("***");
            r.Redact(token + "b").Should().Be("***");
        }

        [Fact]
        public void Does_Not_Mask_31_Hex() {
            var r = new LongHexTokenRedactor();
            var token = new string('a', 31);
            r.Redact(token).Should().Be(token);
        }
    }
}
