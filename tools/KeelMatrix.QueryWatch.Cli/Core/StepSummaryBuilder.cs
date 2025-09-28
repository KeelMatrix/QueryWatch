using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using KeelMatrix.QueryWatch.Contracts;

namespace KeelMatrix.QueryWatch.Cli.Core {
    public static class StepSummaryBuilder {
        public static string Build(
            Aggregated agg,
            int? maxQueries,
            double? maxAvgMs,
            double? maxTotalMs,
            IReadOnlyCollection<string> violations,
            IReadOnlyCollection<(PatternBudget budget, int count, bool over)> patternFindings,
            Summary? baseline,
            double baselineAllowPercent,
            IReadOnlyCollection<string> baselineViolations) {

            var sb = new StringBuilder();
            sb.AppendLine("# QueryWatch Gate");
            sb.AppendLine();
            sb.AppendLine("## Overview");
            sb.AppendLine();
            sb.AppendLine($"* Files: **{agg.Files}**");
            sb.AppendLine($"* Total queries: **{agg.TotalQueries}**");
            sb.AppendLine($"* Average duration: **{agg.AverageDurationMs.ToString("0.00", CultureInfo.InvariantCulture)} ms**");
            sb.AppendLine($"* Total duration: **{agg.TotalDurationMs.ToString("0.00", CultureInfo.InvariantCulture)} ms**");
            sb.AppendLine();
            sb.AppendLine("## Budgets");
            sb.AppendLine();
            sb.AppendLine("| Metric | Limit | Actual | Status |");
            sb.AppendLine("|---|---:|---:|:--:|");
            sb.AppendLine(RowInt("Max Queries", maxQueries, agg.TotalQueries));
            sb.AppendLine(RowDouble("Max Average (ms)", maxAvgMs, agg.AverageDurationMs));
            sb.AppendLine(RowDouble("Max Total (ms)", maxTotalMs, agg.TotalDurationMs));
            sb.AppendLine();

            if (patternFindings.Any()) {
                sb.AppendLine("## Pattern Budgets");
                sb.AppendLine();
                sb.AppendLine("| Pattern | Max | Count | Status |");
                sb.AppendLine("|---|---:|---:|:--:|");
                foreach (var (b, c, over) in patternFindings) {
                    sb.AppendLine($"| `{b.Raw}` | {b.Max} | {c} | {(over ? "❌" : "✅")} |");
                }
                sb.AppendLine();
            }

            if (baseline is not null) {
                sb.AppendLine("## Baseline Comparison");
                sb.AppendLine();
                sb.AppendLine($"Allowed regression: **+{baselineAllowPercent.ToString("0.00", CultureInfo.InvariantCulture)}%**");
                sb.AppendLine();
                sb.AppendLine("| Metric | Baseline | Allowed | Current | Status |");
                sb.AppendLine("|---|---:|---:|---:|:--:|");
                void RowB(string name, double baseVal, double curr) {
                    var allowed = baseVal * (1 + baselineAllowPercent / 100.0);
                    var ok = curr <= allowed + 1e-9;
                    sb.AppendLine($"| {name} | {baseVal:0.##} | {allowed:0.##} | {curr:0.##} | {(ok ? "✅" : "❌")} |");
                }
                RowB("Queries", baseline.TotalQueries, agg.TotalQueries);
                RowB("Average (ms)", baseline.AverageDurationMs, agg.AverageDurationMs);
                RowB("Total (ms)", baseline.TotalDurationMs, agg.TotalDurationMs);
                sb.AppendLine();
            }

            if (violations.Any() || baselineViolations.Any()) {
                sb.AppendLine("## Violations");
                foreach (var v in violations) sb.AppendLine($"- {v}");
                foreach (var v in baselineViolations) sb.AppendLine($"- {v}");
            }

            // add plain-text hints for tests
            sb.AppendLine();
            sb.AppendLine($"files {agg.Files}");
            sb.AppendLine($"Queries: {agg.TotalQueries}");

            return sb.ToString();
        }

        private static string RowInt(string name, int? limit, int actual) =>
            $"| {name} | {(limit?.ToString() ?? "-")} | {actual} | {(limit.HasValue ? (actual <= limit.Value ? "✅" : "❌") : "✅")} |";

        private static string RowDouble(string name, double? limit, double actual) {
            string limitText = "-";
            if (limit.HasValue) {
                var v = limit.Value;
                // if limit is a whole number, print without decimals (e.g., 20)
                limitText = Math.Abs(v - Math.Round(v)) < 1e-9
                    ? Math.Round(v).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            }

            var actualText = actual.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
            var ok = !limit.HasValue || actual <= limit.Value + 1e-9;
            return $"| {name} | {limitText} | {actualText} | {(ok ? "✅" : "❌")} |";
        }
    }
}
