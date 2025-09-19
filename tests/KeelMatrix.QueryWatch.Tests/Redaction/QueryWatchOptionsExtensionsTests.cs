using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class QueryWatchOptionsExtensionsTests {
        [Fact]
        public void UseRecommendedRedactors_Normalize_Then_UrlTokens_Functionally() {
            var opts = new QueryWatchOptions().UseRecommendedRedactors(
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

            using var session = QueryWatcher.Start(opts);
            var input = "  SELECT 1  /* url=https://ex.com?a=1&token=abc123 */  ";
            session.Record(input, TimeSpan.FromMilliseconds(1));
            var report = session.Stop();

            var txt = report.Events[0].CommandText;
            txt.Should().Be("SELECT 1 /* url=https://ex.com?a=1&token=*** */");
        }

        [Fact]
        public void AddRegexRedactor_Adds_Rule_With_Default_Replacement() {
            var opts = new QueryWatchOptions().AddRegexRedactor("foo");
            using var session = QueryWatcher.Start(opts);
            session.Record("Foo BAR", TimeSpan.FromMilliseconds(1));
            var report = session.Stop();
            report.Events[0].CommandText.Should().Contain("***");
            report.Events[0].CommandText.Should().NotContain("Foo");
        }
    }
}
