using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchReportMessageTests {
        [Fact]
        public void ThrowIfViolations_Aggregates_All_Problems_And_Prefixes_With_Summary_Header() {
            var options = new KeelMatrix.QueryWatch.QueryWatchOptions {
                MaxQueries = 1,
                MaxAverageDuration = TimeSpan.FromMilliseconds(1),
                MaxTotalDuration = TimeSpan.FromMilliseconds(1)
            };
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start(options);

            // Two slow queries → violate all three
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(2));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(2));
            var report = session.Stop();

            Action act = () => report.ThrowIfViolations();
            act.Should().Throw<KeelMatrix.QueryWatch.QueryWatchViolationException>()
               .Which.Message.Should().Contain("Summary:")
                               .And.Contain("MaxQueries=")
                               .And.Contain("MaxAverageDuration=")
                               .And.Contain("MaxTotalDuration=");
        }
    }
}
