using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class ApiKeyRedactorTests {
        [Fact]
        public void Masks_Header_Case_Insensitive_And_Preserves_Name() {
            var r = new ApiKeyRedactor();
            var input = "x-api-key: SECRET\r\nSELECT 1;";
            var red = r.Redact(input);
            red.Should().Contain("x-api-key: ***");
            red.Should().NotContain("SECRET");
        }

        [Fact]
        public void Masks_Query_Param_Variants() {
            var r = new ApiKeyRedactor();
            var input = "SELECT /* url=https://ex.com/path?a=1&api_key=SUPERSECRET&x=2 */ 1;";
            var red = r.Redact(input);
            red.Should().Contain("api_key=***");
            red.Should().NotContain("SUPERSECRET");
        }
    }
}
