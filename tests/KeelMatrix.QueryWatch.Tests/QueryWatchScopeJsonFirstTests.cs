using System.Text.Json;
using FluentAssertions;
using KeelMatrix.QueryWatch.Reporting;
using KeelMatrix.QueryWatch.Testing;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchScopeJsonFirstTests {
        [Fact]
        public void Dispose_Writes_JSON_Even_When_Thresholds_Violated() {
            string root = Path.Combine(Path.GetTempPath(), "QueryWatchTests", Guid.NewGuid().ToString("N"));
            string path = Path.Combine(root, "artifacts", "qwatch.report.json");

            Action act = () => {
                using QueryWatchScope scope = QueryWatchScope.Start(
                    maxQueries: 1,
                    exportJsonPath: path,
                    sampleTop: 2);

                scope.Session.Record("A", TimeSpan.FromMilliseconds(3));
                scope.Session.Record("B", TimeSpan.FromMilliseconds(4));
            };

            _ = act.Should().Throw<KeelMatrix.QueryWatch.QueryWatchViolationException>();

            _ = File.Exists(path).Should().BeTrue("JSON must be exported before asserting budgets");
            string json = File.ReadAllText(path);
            var summary = JsonSerializer.Deserialize<QueryWatchJson.Summary>(json);
            _ = summary.Should().NotBeNull();
            _ = summary!.Schema.Should().Be(QueryWatchJson.SchemaVersion);
            _ = summary.Meta.Should().ContainKey("sampleTop").WhoseValue.Should().Be("2");
            _ = summary.Events.Count.Should().BeLessOrEqualTo(2);
        }
    }
}
