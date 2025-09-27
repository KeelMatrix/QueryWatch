using FluentAssertions;
using KeelMatrix.QueryWatch.Reporting;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchJsonMetaTests {
        [Fact]
        public void ToSummary_Includes_Library_Meta() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Record("x", TimeSpan.FromMilliseconds(1));
            var report = session.Stop();

            var s = QueryWatchJson.ToSummary(report, sampleTop: 1);
            s.Meta.Should().ContainKey("library").WhoseValue.Should().Be("KeelMatrix.QueryWatch");
        }

        [Fact]
        public void ToSummary_Populates_SampleTop_Meta() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Record("x", TimeSpan.FromMilliseconds(1));
            var report = session.Stop();

            var s = QueryWatchJson.ToSummary(report, sampleTop: 2);
            s.Meta.Should().ContainKey("sampleTop").WhoseValue.Should().Be("2");
        }
    }
}
