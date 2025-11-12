using FluentAssertions;
using KeelMatrix.QueryWatch.Reporting;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchJsonMetaTests {
        [Fact]
        public void ToSummary_Includes_Library_Meta() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Record("x", TimeSpan.FromMilliseconds(1));
            QueryWatchReport report = session.Stop();

            QueryWatchJson.Summary s = QueryWatchJson.ToSummary(report, sampleTop: 1);
            _ = s.Meta.Should().ContainKey("library").WhoseValue.Should().Be("KeelMatrix.QueryWatch");
        }

        [Fact]
        public void ToSummary_Populates_SampleTop_Meta() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Record("x", TimeSpan.FromMilliseconds(1));
            QueryWatchReport report = session.Stop();

            QueryWatchJson.Summary s = QueryWatchJson.ToSummary(report, sampleTop: 2);
            _ = s.Meta.Should().ContainKey("sampleTop").WhoseValue.Should().Be("2");
        }
    }
}
