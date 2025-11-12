using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class UseRecommendedRedactorsIntegrationTests {
        [Fact]
        public void Recommended_Set_Masks_Common_Pii_And_Normalizes_Whitespace() {
            QueryWatchOptions opts = new QueryWatchOptions().UseRecommendedRedactors(
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

            string redacted = @"
                /* email: user@example.com */
                /* Authorization: Bearer aaaBBBBBBBBB.cccDDDDDDDDD.eeeEEEEEEEEE */
                /* AKIAAAAAAAAAAAAAAAAA */
                /* AccountKey=ZZZ */
                /* code=XYZ */
                /* Password=Secret; */
                /* 123e4567-e89b-12d3-a456-426614174000 */
                SELECT    1
            ";
            foreach (IQueryTextRedactor red in opts.Redactors)
                redacted = red.Redact(redacted);

            // whitespace normalized
            _ = redacted.Should().NotContain("\n").And.NotContain("\r");
            _ = redacted.Should().Contain("SELECT 1");

            // secrets masked
            _ = redacted.Should().NotContain("user@example.com");
            _ = redacted.Should().NotContain("aaaBBBBBBBBB.cccDDDDDDDDD.eeeEEEEEEEEE");
            _ = redacted.Should().NotContain("AKIA");
            _ = redacted.Should().NotContain("AccountKey=ZZZ");
            _ = redacted.Should().NotContain("Password=Secret");
            _ = redacted.Should().NotContain("123e4567-e89b-12d3-a456-426614174000");

            // representative "***"s exist
            _ = redacted.Should().Contain("***");
        }
    }
}
