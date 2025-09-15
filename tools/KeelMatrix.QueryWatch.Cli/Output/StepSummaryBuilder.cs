#nullable enable
using System.Text;

namespace KeelMatrix.QueryWatch.Cli.Core {
    // TODO: REMOVE LATER. Kept as a dedicated builder so CI markdown output is isolated and
    // easily testable in the future without spinning the whole Runner.
    internal static class StepSummaryBuilder {
        public static string Build(
            Aggregated agg,
            int? maxQueries,
            double? maxAvgMs,
            double? maxTotalMs,
            IReadOnlyList<string> violations,
            IReadOnlyList<(PatternBudget budget, int count, bool over)> patternFindings,
            Model.Summary? baseline,
            double baselineAllowPercent,
            IReadOnlyList<string> baselineViolations) {
            var sb = new StringBuilder();
            sb.AppendLine("# QueryWatch Gate");
            sb.AppendLine();
            sb.AppendLine($"Files: **{agg.FileCount}** &nbsp; | &nbsp; Queries: **{agg.TotalQueries}** &nbsp; | &nbsp; Avg: **{agg.AverageDurationMs:N2} ms** &nbsp; | &nbsp; Total: **{agg.TotalDurationMs:N2} ms**");
            sb.AppendLine();

            if (agg.SampledEventsCount < agg.TotalQueries) {
                sb.AppendLine($"> ℹ️ Events are sampled (top-N). Counted {agg.SampledEventsCount} events out of {agg.TotalQueries} queries. Consider exporting with a higher `sampleTop` if you rely on per-pattern budgets.");
                sb.AppendLine();
            }

            sb.AppendLine("## Budgets");
            sb.AppendLine();
            sb.AppendLine("| Metric | Limit | Actual | Status |");
            sb.AppendLine("|---|---:|---:|:--:|");
            void Row(string metric, string limit, double actual, bool ok) {
                var status = ok ? "✅" : "❌";
                sb.AppendLine($"| {metric} | {limit} | {actual:N2} | {status} |");
            }
            Row("Max Queries", maxQueries?.ToString() ?? "—", agg.TotalQueries, !maxQueries.HasValue || agg.TotalQueries <= maxQueries.Value);
            Row("Max Average (ms)", maxAvgMs?.ToString() ?? "—", agg.AverageDurationMs, !maxAvgMs.HasValue || agg.AverageDurationMs <= maxAvgMs.Value);
            Row("Max Total (ms)", maxTotalMs?.ToString() ?? "—", agg.TotalDurationMs, !maxTotalMs.HasValue || agg.TotalDurationMs <= maxTotalMs.Value);
            sb.AppendLine();

            if (patternFindings.Count > 0) {
                sb.AppendLine("## Pattern Budgets");
                sb.AppendLine();
                sb.AppendLine("| Pattern | Max | Count | Status |");
                sb.AppendLine("|---|---:|---:|:--:|");
                foreach (var (budget, count, over) in patternFindings) {
                    var status = over ? "❌" : "✅";
                    sb.AppendLine($"| `{budget.RawPattern}` | {budget.MaxCount} | {count} | {status} |");
                }
                sb.AppendLine();
            }

            if (baseline is not null) {
                sb.AppendLine("## Baseline Comparison");
                sb.AppendLine();
                sb.AppendLine($"Allowed regression: **+{baselineAllowPercent:N2}%**");
                sb.AppendLine();
                sb.AppendLine("| Metric | Baseline | Allowed | Current | Status |");
                sb.AppendLine("|---|---:|---:|---:|:--:|");
                void BRow(string metric, double b, double allowed, double cur) {
                    var ok = cur <= allowed;
                    var status = ok ? "✅" : "❌";
                    sb.AppendLine($"| {metric} | {b:N2} | {allowed:N2} | {cur:N2} | {status} |");
                }
                BRow("Queries", baseline.TotalQueries, baseline.TotalQueries * (1 + baselineAllowPercent / 100.0), agg.TotalQueries);
                BRow("Average (ms)", baseline.AverageDurationMs, baseline.AverageDurationMs * (1 + baselineAllowPercent / 100.0), agg.AverageDurationMs);
                BRow("Total (ms)", baseline.TotalDurationMs, baseline.TotalDurationMs * (1 + baselineAllowPercent / 100.0), agg.TotalDurationMs);
                sb.AppendLine();
            }

            if (violations.Count > 0 || baselineViolations.Count > 0) {
                sb.AppendLine("### Violations");
                sb.AppendLine();
                foreach (var v in violations) sb.AppendLine("- " + v);
                foreach (var v in baselineViolations) sb.AppendLine("- " + v);
                sb.AppendLine();
            }

            return sb.ToString();
        }
    }
}
