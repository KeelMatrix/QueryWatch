using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class UrlQueryTokenRedactorTests {
        [Fact]
        public void Masks_Common_Token_Params() {
            var r = new UrlQueryTokenRedactor();
            var input = "https://ex.com/cb?code=abc&id_token=xyz&AUTH=Z";
            var red = r.Redact(input);
            red.Should().Contain("code=***").And.Contain("id_token=***").And.Contain("AUTH=***");
            red.Should().NotContain("abc").And.NotContain("xyz").And.NotContain("Z");
        }

        [Fact]
        public void Leaves_Other_Params_Alone() {
            var r = new UrlQueryTokenRedactor();
            r.Redact("https://ex.com?tok=1").Should().Be("https://ex.com?tok=1");
        }

        [Fact]
        public void Masks_Tokens_In_Fragment_Section() {
            var r = new UrlQueryTokenRedactor();
            var input = "https://ex.com/cb#access_token=AAA&id_token=BBB&state=123";
            var red = r.Redact(input);
            red.Should().Contain("access_token=***").And.Contain("id_token=***");
            red.Should().NotContain("AAA").And.NotContain("BBB");
            red.Should().Contain("&state=123");
        }
    }
}
