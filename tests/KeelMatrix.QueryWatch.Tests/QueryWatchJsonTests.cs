using System.Text.Json;
using FluentAssertions;
using KeelMatrix.QueryWatch.Reporting;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchJsonTests {
        // Note: These tests lock down the JSON "shape" that CI relies on.
        // We purposely avoid overfitting to timestamps and only assert stable fields and sampling behavior.

        [Fact]
        public void ToSummary_Respects_SampleTop_And_Sorts_Descending() {
            using QueryWatchSession session = QueryWatcher.Start();
            session.Record("fast", TimeSpan.FromMilliseconds(5));
            session.Record("slow", TimeSpan.FromMilliseconds(12));
            session.Record("medium", TimeSpan.FromMilliseconds(7));
            QueryWatchReport report = session.Stop();

            QueryWatchJson.Summary summary = QueryWatchJson.ToSummary(report, sampleTop: 2);

            _ = summary.Schema.Should().Be(QueryWatchJson.SchemaVersion);
            _ = summary.TotalQueries.Should().Be(3);
            _ = summary.Events.Should().HaveCount(2, "sampleTop limits the number of items");
            _ = summary.Events.Select(e => e.DurationMs).Should().BeInDescendingOrder();
            _ = summary.Events[0].Text.Should().Be("slow");
        }

        [Fact]
        public void ExportToFile_Writes_File_With_Meta_SampleTop_And_Creates_Directory() {
            using QueryWatchSession session = QueryWatcher.Start();
            session.Record("a", TimeSpan.FromMilliseconds(3));
            session.Record("b", TimeSpan.FromMilliseconds(9));
            QueryWatchReport report = session.Stop();

            var tempRoot = Path.Combine(Path.GetTempPath(), "QueryWatchTests", Guid.NewGuid().ToString("N"));
            var path = Path.Combine(tempRoot, "out", "qwatch.json");
            _ = Directory.Exists(tempRoot).Should().BeFalse("we want to verify the exporter creates the directory");

            QueryWatchJson.ExportToFile(report, path, sampleTop: 1);

            _ = File.Exists(path).Should().BeTrue("exporter should write the file");
            var json = File.ReadAllText(path);
            _ = json.Should().NotBeNullOrWhiteSpace();

            QueryWatchJson.Summary? summary = JsonSerializer.Deserialize<QueryWatchJson.Summary>(json);
            _ = summary.Should().NotBeNull();
            _ = summary!.Schema.Should().Be(QueryWatchJson.SchemaVersion);
            _ = summary.Events.Should().HaveCount(1);
            _ = summary.Meta.Should().ContainKey("sampleTop");
            _ = summary.Meta["sampleTop"].Should().Be("1");
        }
    }
}
