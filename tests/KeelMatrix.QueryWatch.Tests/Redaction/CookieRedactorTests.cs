using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class CookieRedactorTests {
        [Fact]
        public void Masks_Cookie_Header_Entirely() {
            var r = new CookieRedactor();
            var input = "Cookie: a=b; session=abc\r\nSELECT 1;";
            var red = r.Redact(input);
            red.Should().Contain("Cookie: ***");
            red.Should().NotContain("session=abc");
        }

        [Fact]
        public void Masks_SetCookie_Value_But_Keeps_Name_And_Attributes() {
            var r = new CookieRedactor();
            var input = "Set-Cookie: session=abc123; Path=/; HttpOnly";
            var red = r.Redact(input);
            red.Should().Be("Set-Cookie: session=***; Path=/; HttpOnly");
        }
    }
}
