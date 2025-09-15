using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using KeelMatrix.QueryWatch.Reporting;
using KeelMatrix.QueryWatch.Testing;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchScopeJsonFirstTests {
        [Fact]
        public void Dispose_Writes_JSON_Even_When_Thresholds_Violated() {
            var root = Path.Combine(Path.GetTempPath(), "QueryWatchTests", Guid.NewGuid().ToString("N"));
            var path = Path.Combine(root, "artifacts", "qwatch.report.json");

            Action act = () => {
                using var scope = QueryWatchScope.Start(
                    maxQueries: 1,
                    exportJsonPath: path,
                    sampleTop: 2);

                scope.Session.Record("A", TimeSpan.FromMilliseconds(3));
                scope.Session.Record("B", TimeSpan.FromMilliseconds(4));
            };

            act.Should().Throw<KeelMatrix.QueryWatch.QueryWatchViolationException>();

            File.Exists(path).Should().BeTrue("JSON must be exported before asserting budgets");
            var json = File.ReadAllText(path);
            var summary = JsonSerializer.Deserialize<QueryWatchJson.Summary>(json);
            summary.Should().NotBeNull();
            summary!.Schema.Should().Be(QueryWatchJson.SchemaVersion);
            summary.Meta.Should().ContainKey("sampleTop").WhoseValue.Should().Be("2");
            summary.Events.Count.Should().BeLessOrEqualTo(2);
        }
    }
}
