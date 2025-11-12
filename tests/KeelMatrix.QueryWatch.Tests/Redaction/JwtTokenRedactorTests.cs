using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class JwtTokenRedactorTests {
        [Fact]
        public void Masks_Three_Segment_Base64Url_Tokens() {
            JwtTokenRedactor r = new();
            const string tok = "aaaBBBBBBBBB.cccDDDDDDDDD.eeeEEEEEEEEE"; // each segment >=10
            _ = r.Redact(tok).Should().Be("***");
        }

        [Fact]
        public void Does_Not_Mask_Two_Segments() {
            JwtTokenRedactor r = new();
            const string tok = "aaaBBBBBBBBB.cccDDDDDDDDD";
            _ = r.Redact(tok).Should().Be(tok);
        }
    }
}
