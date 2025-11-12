using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class CookieRedactorTests {
        [Fact]
        public void Masks_Cookie_Header_Completely() {
            CookieRedactor r = new();
            const string input = "Cookie: a=1; b=2\r\nSELECT 1;";
            string red = r.Redact(input);
            _ = red.Should().Contain("Cookie: ***");
            _ = red.Should().Contain("SELECT 1;");
        }

        [Fact]
        public void Masks_SetCookie_Value_But_Keeps_Name_And_Attributes() {
            CookieRedactor r = new();
            const string input = "Set-Cookie: sessionid=abc123; Path=/; HttpOnly";
            string red = r.Redact(input);
            _ = red.Should().Be("Set-Cookie: sessionid=***; Path=/; HttpOnly");
        }

        [Fact]
        public void Multiple_Lines_Are_Handled() {
            CookieRedactor r = new();
            const string input = "Set-Cookie: A=1\r\nSet-Cookie: B=2; Secure";
            string red = r.Redact(input);
            _ = red.Should().Contain("Set-Cookie: A=***");
            _ = red.Should().Contain("Set-Cookie: B=***; Secure");
        }
    }
}
