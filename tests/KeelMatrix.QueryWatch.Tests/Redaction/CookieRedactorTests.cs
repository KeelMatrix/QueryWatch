using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class CookieRedactorTests {
        [Fact]
        public void Masks_Cookie_Header_Completely() {
            var r = new CookieRedactor();
            var input = "Cookie: a=1; b=2\r\nSELECT 1;";
            var red = r.Redact(input);
            red.Should().Contain("Cookie: ***");
            red.Should().Contain("SELECT 1;");
        }

        [Fact]
        public void Masks_SetCookie_Value_But_Keeps_Name_And_Attributes() {
            var r = new CookieRedactor();
            var input = "Set-Cookie: sessionid=abc123; Path=/; HttpOnly";
            var red = r.Redact(input);
            red.Should().Be("Set-Cookie: sessionid=***; Path=/; HttpOnly");
        }

        [Fact]
        public void Multiple_Lines_Are_Handled() {
            var r = new CookieRedactor();
            var input = "Set-Cookie: A=1\r\nSet-Cookie: B=2; Secure";
            var red = r.Redact(input);
            red.Should().Contain("Set-Cookie: A=***");
            red.Should().Contain("Set-Cookie: B=***; Secure");
        }
    }
}
