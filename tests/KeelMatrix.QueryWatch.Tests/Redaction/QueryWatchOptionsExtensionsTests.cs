using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class QueryWatchOptionsExtensionsTests {
        [Fact]
        public void UseRecommendedRedactors_Normalize_Then_UrlTokens_Functionally() {
            QueryWatchOptions opts = new QueryWatchOptions().UseRecommendedRedactors(
                includeWhitespaceNormalizer: true,
                includeEmails: false,
                includeLongHex: false,
                includeJwt: false,
                includeAuthorization: false,
                includeConnStringPwd: false,
                includeGuid: false,
                includeUrlTokens: true,
                includeAwsAccessKey: false,
                includeAzureKeys: false,
                includeGuidLikeHex: false,
                includeTimestamps: false,
                includeIpAddresses: false,
                includePhone: false);

            using QueryWatchSession session = QueryWatcher.Start(opts);
            const string input = "  SELECT 1  /* url=https://ex.com?a=1&token=abc123 */  ";
            session.Record(input, TimeSpan.FromMilliseconds(1));
            QueryWatchReport report = session.Stop();

            string txt = report.Events[0].CommandText;
            _ = txt.Should().Be("SELECT 1 /* url=https://ex.com?a=1&token=*** */");
        }

        [Fact]
        public void AddRegexRedactor_Adds_Rule_With_Default_Replacement() {
            QueryWatchOptions opts = new QueryWatchOptions().AddRegexRedactor("foo");
            using QueryWatchSession session = QueryWatcher.Start(opts);
            session.Record("Foo BAR", TimeSpan.FromMilliseconds(1));
            QueryWatchReport report = session.Stop();
            _ = report.Events[0].CommandText.Should().Contain("***");
            _ = report.Events[0].CommandText.Should().NotContain("Foo");
        }
    }
}
