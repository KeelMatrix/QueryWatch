using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchReportMathTests {
        [Fact]
        public void AverageDuration_Is_Calculated_From_TotalMs_Over_Count() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Record("a", TimeSpan.FromMilliseconds(1));
            session.Record("b", TimeSpan.FromMilliseconds(2));
            QueryWatchReport r = session.Stop();

            _ = r.AverageDuration.TotalMilliseconds.Should().BeApproximately(1.5, 0.1);
            _ = r.TotalDuration.TotalMilliseconds.Should().BeApproximately(3.0, 0.1);
        }

        [Fact]
        public void AverageDuration_Is_Zero_For_No_Events() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            QueryWatchReport r = session.Stop();
            _ = r.TotalQueries.Should().Be(0);
            _ = r.AverageDuration.Should().Be(TimeSpan.Zero);
            _ = r.TotalDuration.Should().Be(TimeSpan.Zero);
        }
    }
}
