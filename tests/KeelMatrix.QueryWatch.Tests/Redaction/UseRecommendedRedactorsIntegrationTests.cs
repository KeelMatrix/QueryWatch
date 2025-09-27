using FluentAssertions;
using KeelMatrix.QueryWatch;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class UseRecommendedRedactorsIntegrationTests {
        [Fact]
        public void Recommended_Set_Masks_Common_Pii_And_Normalizes_Whitespace() {
            var opts = new QueryWatchOptions().UseRecommendedRedactors(
                includeWhitespaceNormalizer: true,
                includeEmails: true,
                includeLongHex: true,
                includeJwt: true,
                includeAuthorization: true,
                includeConnStringPwd: true,
                includeGuid: true,
                includeUrlTokens: true,
                includeAwsAccessKey: true,
                includeAzureKeys: true,
                includeGuidLikeHex: true
            );

            var input = @"
                /* email: user@example.com */
                /* Authorization: Bearer aaaBBBBBBBBB.cccDDDDDDDDD.eeeEEEEEEEEE */
                /* AKIAAAAAAAAAAAAAAAAA */
                /* AccountKey=ZZZ */
                /* code=XYZ */
                /* Password=Secret; */
                /* 123e4567-e89b-12d3-a456-426614174000 */
                SELECT    1
            ";

            string redacted = input;
            foreach (var red in opts.Redactors)
                redacted = red.Redact(redacted);

            // whitespace normalized
            redacted.Should().NotContain("\n").And.NotContain("\r");
            redacted.Should().Contain("SELECT 1");

            // secrets masked
            redacted.Should().NotContain("user@example.com");
            redacted.Should().NotContain("aaaBBBBBBBBB.cccDDDDDDDDD.eeeEEEEEEEEE");
            redacted.Should().NotContain("AKIA");
            redacted.Should().NotContain("AccountKey=ZZZ");
            redacted.Should().NotContain("Password=Secret");
            redacted.Should().NotContain("123e4567-e89b-12d3-a456-426614174000");

            // representative "***"s exist
            redacted.Should().Contain("***");
        }
    }
}
