using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class ApiKeyRedactorTests {
        [Fact]
        public void Masks_Header_Preserves_Name_And_Only_Value() {
            ApiKeyRedactor r = new();
            const string input = "X-Api-Key: SECRET\r\nSELECT 1;";
            string red = r.Redact(input);
            _ = red.Should().Contain("X-Api-Key: ***");
            _ = red.Should().NotContain("SECRET");
            // Other lines stay intact
            _ = red.Should().Contain("SELECT 1;");
        }

        [Fact]
        public void Masks_Common_Query_Param_Variants() {
            ApiKeyRedactor r = new();
            string input = "/* url=https://ex.com/path?a=1&api_key=SUPERSECRET&x=2 */";
            string red = r.Redact(input);
            _ = red.Should().Contain("api_key=***");
            _ = red.Should().NotContain("SUPERSECRET");

            input = "/* url=https://ex.com/path?a=1&apikey=TOP&ApiKey=DOWN */";
            red = r.Redact(input);
            _ = red.Should().Contain("apikey=***");
            _ = red.Should().Contain("ApiKey=***");
            _ = red.Should().NotContain("TOP").And.NotContain("DOWN");
        }

        [Fact]
        public void Does_Not_Mask_Similar_Param_Names() {
            ApiKeyRedactor r = new();
            const string input = "/* url=https://ex.com/path?apikey_hint=nothing&x=2 */";
            string red = r.Redact(input);
            _ = red.Should().Contain("apikey_hint=nothing", "suffixes should not be masked");
        }

        [Fact]
        public void Idempotent_Application() {
            ApiKeyRedactor r = new();
            const string input = "X-Api-Key: SECRET";
            string once = r.Redact(input);
            string twice = r.Redact(once);
            _ = twice.Should().Be(once);
        }

        [Fact]
        public void Masks_Query_Param_With_Hyphen_Or_Underscore() {
            ApiKeyRedactor r = new();
            _ = r.Redact("https://ex.com?api-key=XYZ").Should().Be("https://ex.com?api-key=***");
            _ = r.Redact("https://ex.com?api_key=XYZ").Should().Be("https://ex.com?api_key=***");
        }
    }
}
