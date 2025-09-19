using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class UrlQueryTokenRedactorTests {
        [Fact]
        public void Masks_Common_Sensitive_Params() {
            var r = new UrlQueryTokenRedactor();
            var input = "GET /cb?code=abc&access_token=xyz&id_token=foo&auth=bar";
            var red = r.Redact(input);
            red.Should().Contain("code=***");
            red.Should().Contain("access_token=***");
            red.Should().Contain("id_token=***");
            red.Should().Contain("auth=***");
            red.Should().NotContain("abc");
            red.Should().NotContain("xyz");
            red.Should().NotContain("foo");
            red.Should().NotContain("bar");
        }
    }
}
