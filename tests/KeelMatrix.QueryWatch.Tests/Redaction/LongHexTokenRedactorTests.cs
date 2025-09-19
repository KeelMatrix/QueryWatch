using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class LongHexTokenRedactorTests {
        [Fact]
        public void Masks_Hex_Tokens_Of_Length_32_Or_More() {
            var r = new LongHexTokenRedactor();
            var token = "0123456789abcdef0123456789ABCDEF"; // 32 chars
            var input = $"/* {token} */";
            var red = r.Redact(input);
            red.Should().NotContain(token);
            red.Should().Contain("***");
        }

        [Fact]
        public void Does_Not_Mask_31_Chars() {
            var r = new LongHexTokenRedactor();
            var token31 = "0123456789abcdef0123456789ABCDE"; // 31 chars
            var input = $"/* {token31} */";
            r.Redact(input).Should().Contain(token31);
        }
    }
}
