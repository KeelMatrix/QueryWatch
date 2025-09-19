using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class JwtTokenRedactorTests {
        [Fact]
        public void Masks_Jwt_Like_Tokens() {
            var r = new JwtTokenRedactor();
            var input = "Authorization: Bearer aaaaaaaaaa.bbbbbbbbbbb.cccccccccccc";
            var red = r.Redact(input);
            red.Should().NotContain("aaaaaaaaaa.bbbbbbbbbbb.cccccccccccc");
            red.Should().Contain("***");
        }

        [Fact]
        public void Does_Not_Mask_Short_Dot_Separated_Tokens() {
            var r = new JwtTokenRedactor();
            var input = "token=a.b.c";
            r.Redact(input).Should().Be(input);
        }
    }
}
