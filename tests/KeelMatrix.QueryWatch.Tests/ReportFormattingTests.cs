using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class ReportFormattingTests {
        [Fact]
        public void ThrowIfViolations_Message_Should_Contain_Summary_Header() {
            QueryWatchOptions options = new() { MaxQueries = 1 };
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start(options);
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(2));
            QueryWatchReport report = session.Stop();

            Action act = report.ThrowIfViolations;

            _ = act.Should().Throw<KeelMatrix.QueryWatch.QueryWatchViolationException>()
               .Which.Message.Should().Contain("Summary:");
        }
    }
}
