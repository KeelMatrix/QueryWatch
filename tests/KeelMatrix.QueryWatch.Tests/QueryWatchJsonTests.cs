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
            using var session = QueryWatcher.Start();
            session.Record("fast", TimeSpan.FromMilliseconds(5));
            session.Record("slow", TimeSpan.FromMilliseconds(12));
            session.Record("medium", TimeSpan.FromMilliseconds(7));
            var report = session.Stop();

            var summary = QueryWatchJson.ToSummary(report, sampleTop: 2);

            summary.Schema.Should().Be(QueryWatchJson.SchemaVersion);
            summary.TotalQueries.Should().Be(3);
            summary.Events.Should().HaveCount(2, "sampleTop limits the number of items");
            summary.Events.Select(e => e.DurationMs).Should().BeInDescendingOrder();
            summary.Events.First().Text.Should().Be("slow");
        }

        [Fact]
        public void ExportToFile_Writes_File_With_Meta_SampleTop_And_Creates_Directory() {
            using var session = QueryWatcher.Start();
            session.Record("a", TimeSpan.FromMilliseconds(3));
            session.Record("b", TimeSpan.FromMilliseconds(9));
            var report = session.Stop();

            var tempRoot = Path.Combine(Path.GetTempPath(), "QueryWatchTests", Guid.NewGuid().ToString("N"));
            var path = Path.Combine(tempRoot, "out", "qwatch.json");
            Directory.Exists(tempRoot).Should().BeFalse("we want to verify the exporter creates the directory");

            QueryWatchJson.ExportToFile(report, path, sampleTop: 1);

            File.Exists(path).Should().BeTrue("exporter should write the file");
            var json = File.ReadAllText(path);
            json.Should().NotBeNullOrWhiteSpace();

            var summary = JsonSerializer.Deserialize<QueryWatchJson.Summary>(json);
            summary.Should().NotBeNull();
            summary!.Schema.Should().Be(QueryWatchJson.SchemaVersion);
            summary.Events.Should().HaveCount(1);
            summary.Meta.Should().ContainKey("sampleTop");
            summary.Meta["sampleTop"].Should().Be("1");
        }
    }
}
