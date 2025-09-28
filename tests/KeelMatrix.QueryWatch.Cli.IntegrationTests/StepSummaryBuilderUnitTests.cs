using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KeelMatrix.QueryWatch.Cli.Core;
using KeelMatrix.QueryWatch.Contracts;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class StepSummaryBuilderUnitTests {
        private static Aggregated AggFrom(params Summary[] summaries) => Aggregated.From(summaries);

        private static Summary MakeSummary(int totalQueries, double avgMs, double totalMs, int eventCount, bool sampled) {
            var s = new Summary {
                Schema = "1.0.0",
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-5),
                StoppedAt = DateTimeOffset.UtcNow,
                TotalQueries = totalQueries,
                AverageDurationMs = avgMs,
                TotalDurationMs = totalMs,
                Events = Enumerable.Range(1, eventCount).Select(i => new EventSample {
                    At = DateTimeOffset.UtcNow.AddMilliseconds(i),
                    DurationMs = avgMs,
                    Text = $"SELECT * FROM T{i}"
                }).ToList(),
                Meta = new Dictionary<string, string>()
            };
            if (sampled) s.Meta["sampleTop"] = "5";
            return s;
        }

        [Fact]
        public void Budgets_Table_Shows_Statuses() {
            // Arrange
            var s = MakeSummary(totalQueries: 3, avgMs: 10, totalMs: 30, eventCount: 3, sampled: false);
            var agg = AggFrom(s);
            var budgetsViolations = new List<string>();
            var ok = PatternBudget.TryParse("SELECT *|2", out var b1, out _) ? b1! : throw new InvalidOperationException();
            var patternFindings = new List<(PatternBudget budget, int count, bool over)>() {
                (ok, 3, true) // over
            };

            // Act
            var md = StepSummaryBuilder.Build(
                agg,
                maxQueries: 5,
                maxAvgMs: 20,
                maxTotalMs: 50,
                violations: budgetsViolations,
                patternFindings: patternFindings,
                baseline: null,
                baselineAllowPercent: 0,
                baselineViolations: Array.Empty<string>());

            // Assert
            md.Should().Contain("| Max Queries | 5 | 3 | ✅ |");
            md.Should().Contain("| Max Average (ms) | 20 | 10.00 | ✅ |");
            md.Should().Contain("| Max Total (ms) | 50 | 30.00 | ✅ |");
        }
    }
}
