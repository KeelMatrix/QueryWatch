using System.Text.Json;
using FluentAssertions;
using KeelMatrix.QueryWatch.Reporting;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchJsonEdgeTests {
        [Fact]
        public void ToSummary_Clamps_Negative_SampleTop_To_Zero() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Record("a", TimeSpan.FromMilliseconds(1));
            session.Record("b", TimeSpan.FromMilliseconds(2));
            var report = session.Stop();

            var summary = QueryWatchJson.ToSummary(report, sampleTop: -5);
            summary.Should().NotBeNull();
            summary.Events.Should().NotBeNull();
            summary.Events.Count.Should().Be(0, "negative sampleTop should be treated as zero");
            summary.Meta.Should().ContainKey("sampleTop").WhoseValue.Should().Be("0");
        }

        [Fact]
        public void Export_Omits_Event_Meta_Property_When_Null() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Record("query", TimeSpan.FromMilliseconds(1), meta: null);
            var report = session.Stop();

            var tempRoot = Path.Combine(Path.GetTempPath(), "qwatch-json-edge-" + Guid.NewGuid().ToString("N"));
            var path = Path.Combine(tempRoot, "out", "summary.json");

            try {
                QueryWatchJson.ExportToFile(report, path, sampleTop: 1);
                File.Exists(path).Should().BeTrue();

                using var doc = JsonDocument.Parse(File.ReadAllText(path));
                var root = doc.RootElement;
                var events = root.GetProperty("events");
                events.GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
                var first = events.EnumerateArray().First();
                // Contract: omit event-level "meta" when it's null to keep JSON compact
                first.TryGetProperty("meta", out _).Should().BeFalse("null meta should be omitted to reduce JSON size");
            }
            finally {
                if (Directory.Exists(tempRoot)) {
                    try { Directory.Delete(tempRoot, recursive: true); } catch { /* ignore */ }
                }
            }
        }
    }
}
