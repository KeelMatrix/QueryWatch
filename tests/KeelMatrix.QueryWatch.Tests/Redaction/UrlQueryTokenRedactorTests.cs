using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class UrlQueryTokenRedactorTests {
        [Fact]
        public void Masks_Common_Token_Params() {
            UrlQueryTokenRedactor r = new();
            const string input = "https://ex.com/cb?code=abc&id_token=xyz&AUTH=Z";
            string red = r.Redact(input);
            _ = red.Should().Contain("code=***").And.Contain("id_token=***").And.Contain("AUTH=***");
            _ = red.Should().NotContain("abc").And.NotContain("xyz").And.NotContain("Z");
        }

        [Fact]
        public void Leaves_Other_Params_Alone() {
            UrlQueryTokenRedactor r = new();
            _ = r.Redact("https://ex.com?tok=1").Should().Be("https://ex.com?tok=1");
        }

        [Fact]
        public void Masks_Tokens_In_Fragment_Section() {
            UrlQueryTokenRedactor r = new();
            const string input = "https://ex.com/cb#access_token=AAA&id_token=BBB&state=123";
            string red = r.Redact(input);
            _ = red.Should().Contain("access_token=***").And.Contain("id_token=***");
            _ = red.Should().NotContain("AAA").And.NotContain("BBB");
            _ = red.Should().Contain("&state=123");
        }
    }
}
