using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using KeelMatrix.QueryWatch.Cli.Core;
using KeelMatrix.QueryWatch.Cli.Model;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class StepSummaryBuilderUnitTests {
        private static Aggregated AggFrom(params Summary[] summaries) => Aggregated.From(summaries);

        private static Summary MakeSummary(int totalQueries, double avgMs, double totalMs, int eventCount, bool sampled) {
            var s = new Summary {
                Schema = "1.0.0",
                StartedAt = DateTimeOffset.UtcNow,
                StoppedAt = DateTimeOffset.UtcNow,
                TotalQueries = totalQueries,
                AverageDurationMs = avgMs,
                TotalDurationMs = totalMs,
                Events = new List<EventSample>(),
                Meta = new Dictionary<string, string>()
            };
            for (int i = 0; i < eventCount; i++) {
                s.Events.Add(new EventSample { At = DateTimeOffset.UtcNow, DurationMs = 1 + i, Text = "SELECT * FROM Users WHERE Id=" + i });
            }
            if (sampled) {
                s.Meta["sampleTop"] = eventCount.ToString();
            }
            return s;
        }

        [Fact]
        public void Budgets_Table_Shows_Statuses() {
            // Arrange
            var s = MakeSummary(totalQueries: 3, avgMs: 10, totalMs: 30, eventCount: 3, sampled: false);
            var agg = AggFrom(s);
            var budgetsViolations = new List<string>();
            var patternBudget = PatternBudget.TryParse("SELECT * FROM Users*=2", out var b1, out _) ? b1! : throw new InvalidOperationException();
            var patternFindings = new List<(PatternBudget budget, int count, bool over)>() {
                (patternBudget, 3, true) // over
            };

            // Act
            var md = StepSummaryBuilder.Build(agg,
                maxQueries: 5, maxAvgMs: 20, maxTotalMs: 50,
                violations: budgetsViolations,
                patternFindings: patternFindings,
                baseline: null,
                baselineAllowPercent: 0,
                baselineViolations: Array.Empty<string>());

            // Assert
            md.Should().Contain("## Budgets");
            md.Should().Contain("| Metric | Limit | Actual | Status |");
            md.Should().Contain("Max Queries").And.Contain("Max Average (ms)").And.Contain("Max Total (ms)");
            md.Should().Contain("✅"); // at least one good status
            // pattern budgets table
            md.Should().Contain("## Pattern Budgets");
            md.Should().Contain("| Pattern | Max | Count | Status |");
            md.Should().Contain("❌");
        }

        [Fact]
        public void Sampled_Events_Note_Is_Present() {
            // Arrange: only 1 event out of 3 total, with sampleTop set
            var s = MakeSummary(totalQueries: 3, avgMs: 10, totalMs: 30, eventCount: 1, sampled: true);
            var agg = AggFrom(s);

            // Act
            var md = StepSummaryBuilder.Build(agg,
                maxQueries: null, maxAvgMs: null, maxTotalMs: null,
                violations: Array.Empty<string>(),
                patternFindings: Array.Empty<(PatternBudget budget, int count, bool over)>(),
                baseline: null,
                baselineAllowPercent: 0,
                baselineViolations: Array.Empty<string>());

            // Assert
            md.Should().Contain("Events are sampled", "builder should warn when events are top-N sampled");
        }

        [Fact]
        public void Baseline_Table_Present_When_Baseline_Provided() {
            // Arrange
            var s = MakeSummary(totalQueries: 100, avgMs: 10, totalMs: 1000, eventCount: 0, sampled: false);
            var agg = AggFrom(s);
            var baseline = new Summary {
                Schema = "1.0.0",
                StartedAt = DateTimeOffset.UtcNow,
                StoppedAt = DateTimeOffset.UtcNow,
                TotalQueries = 90,
                AverageDurationMs = 9.0,
                TotalDurationMs = 900,
                Events = new List<EventSample>(),
                Meta = new Dictionary<string, string>()
            };

            // Act
            var md = StepSummaryBuilder.Build(agg,
                maxQueries: null, maxAvgMs: null, maxTotalMs: null,
                violations: Array.Empty<string>(),
                patternFindings: Array.Empty<(PatternBudget budget, int count, bool over)>(),
                baseline: baseline,
                baselineAllowPercent: 10,
                baselineViolations: Array.Empty<string>());

            // Assert
            md.Should().Contain("## Baseline Comparison");
            md.Should().Contain("Allowed regression: **+10.00%**");
            md.Should().Contain("| Metric | Baseline | Allowed | Current | Status |");
            md.Should().Contain("Queries").And.Contain("Average (ms)").And.Contain("Total (ms)");
        }
    }
}
