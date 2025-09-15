using System;
using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchReportMathTests {
        [Fact]
        public void AverageDuration_Is_Calculated_From_TotalMs_Over_Count() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Record("a", TimeSpan.FromMilliseconds(1));
            session.Record("b", TimeSpan.FromMilliseconds(2));
            var r = session.Stop();

            r.AverageDuration.TotalMilliseconds.Should().BeApproximately(1.5, 0.1);
            r.TotalDuration.TotalMilliseconds.Should().BeApproximately(3.0, 0.1);
        }

        [Fact]
        public void AverageDuration_Is_Zero_For_No_Events() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            var r = session.Stop();
            r.TotalQueries.Should().Be(0);
            r.AverageDuration.Should().Be(TimeSpan.Zero);
            r.TotalDuration.Should().Be(TimeSpan.Zero);
        }
    }
}
