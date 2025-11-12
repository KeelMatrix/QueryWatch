using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public partial class RedactionTests {
        private sealed partial class DigitsToAsterisks : KeelMatrix.QueryWatch.IQueryTextRedactor {
            public string Redact(string input) => RedactRegex().Replace(input ?? string.Empty, "*");

            [System.Text.RegularExpressions.GeneratedRegex(@"\d")]
            private static partial System.Text.RegularExpressions.Regex RedactRegex();
        }

        [Fact]
        public void Record_Applies_Redactors_To_CommandText() {
            QueryWatchOptions options = new() {
                CaptureSqlText = true
            };
            options.Redactors.Add(new DigitsToAsterisks());

            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start(options);
            session.Record("SELECT * FROM Users WHERE Id=123", TimeSpan.FromMilliseconds(5));
            QueryWatchReport report = session.Stop();

            _ = report.Events.Should().HaveCount(1);
            var recorded = report.Events[0].CommandText;

            _ = recorded.Should().NotContain("123");
            _ = recorded.Should().Contain("***");
        }
    }
}
