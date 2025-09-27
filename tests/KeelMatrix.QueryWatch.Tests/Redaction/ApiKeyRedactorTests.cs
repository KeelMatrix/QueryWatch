
using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class ApiKeyRedactorTests {
        [Fact]
        public void Masks_Header_Preserves_Name_And_Only_Value() {
            var r = new ApiKeyRedactor();
            var input = "X-Api-Key: SECRET\r\nSELECT 1;";
            var red = r.Redact(input);
            red.Should().Contain("X-Api-Key: ***");
            red.Should().NotContain("SECRET");
            // Other lines stay intact
            red.Should().Contain("SELECT 1;");
        }

        [Fact]
        public void Masks_Common_Query_Param_Variants() {
            var r = new ApiKeyRedactor();
            var input = "/* url=https://ex.com/path?a=1&api_key=SUPERSECRET&x=2 */";
            var red = r.Redact(input);
            red.Should().Contain("api_key=***");
            red.Should().NotContain("SUPERSECRET");

            input = "/* url=https://ex.com/path?a=1&apikey=TOP&ApiKey=DOWN */";
            red = r.Redact(input);
            red.Should().Contain("apikey=***");
            red.Should().Contain("ApiKey=***");
            red.Should().NotContain("TOP").And.NotContain("DOWN");
        }

        [Fact]
        public void Does_Not_Mask_Similar_Param_Names() {
            var r = new ApiKeyRedactor();
            var input = "/* url=https://ex.com/path?apikey_hint=nothing&x=2 */";
            var red = r.Redact(input);
            red.Should().Contain("apikey_hint=nothing", "suffixes should not be masked");
        }

        [Fact]
        public void Idempotent_Application() {
            var r = new ApiKeyRedactor();
            var input = "X-Api-Key: SECRET";
            var once = r.Redact(input);
            var twice = r.Redact(once);
            twice.Should().Be(once);
        }

        [Fact]
        public void Masks_Query_Param_With_Hyphen_Or_Underscore() {
            var r = new ApiKeyRedactor();
            r.Redact("https://ex.com?api-key=XYZ").Should().Be("https://ex.com?api-key=***");
            r.Redact("https://ex.com?api_key=XYZ").Should().Be("https://ex.com?api_key=***");
        }
    }
}
