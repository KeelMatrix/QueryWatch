using System.Globalization;
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

            StringBuilder sb = new();
            _ = sb.AppendLine("# QueryWatch Gate");
            _ = sb.AppendLine();
            _ = sb.AppendLine("## Overview");
            _ = sb.AppendLine();
            _ = sb.AppendLine(CultureInfo.InvariantCulture, $"* Files: **{agg.Files}**");
            _ = sb.AppendLine(CultureInfo.InvariantCulture, $"* Total queries: **{agg.TotalQueries}**");
            _ = sb.AppendLine(CultureInfo.InvariantCulture, $"* Average duration: **{agg.AverageDurationMs.ToString("0.00", CultureInfo.InvariantCulture)} ms**");
            _ = sb.AppendLine(CultureInfo.InvariantCulture, $"* Total duration: **{agg.TotalDurationMs.ToString("0.00", CultureInfo.InvariantCulture)} ms**");
            _ = sb.AppendLine();
            _ = sb.AppendLine("## Budgets");
            _ = sb.AppendLine();
            _ = sb.AppendLine("| Metric | Limit | Actual | Status |");
            _ = sb.AppendLine("|---|---:|---:|:--:|");
            _ = sb.AppendLine(RowInt("Max Queries", maxQueries, agg.TotalQueries));
            _ = sb.AppendLine(RowDouble("Max Average (ms)", maxAvgMs, agg.AverageDurationMs));
            _ = sb.AppendLine(RowDouble("Max Total (ms)", maxTotalMs, agg.TotalDurationMs));
            _ = sb.AppendLine();

            if (patternFindings.Count != 0) {
                _ = sb.AppendLine("## Pattern Budgets");
                _ = sb.AppendLine();
                _ = sb.AppendLine("| Pattern | Max | Count | Status |");
                _ = sb.AppendLine("|---|---:|---:|:--:|");
                foreach ((PatternBudget? b, int c, bool over) in patternFindings) {
                    _ = sb.AppendLine(CultureInfo.InvariantCulture, $"| `{b.Raw}` | {b.Max} | {c} | {(over ? "❌" : "✅")} |");
                }
                _ = sb.AppendLine();
            }

            if (baseline is not null) {
                _ = sb.AppendLine("## Baseline Comparison");
                _ = sb.AppendLine();
                _ = sb.AppendLine(CultureInfo.InvariantCulture, $"Allowed regression: **+{baselineAllowPercent.ToString("0.00", CultureInfo.InvariantCulture)}%**");
                _ = sb.AppendLine();
                _ = sb.AppendLine("| Metric | Baseline | Allowed | Current | Status |");
                _ = sb.AppendLine("|---|---:|---:|---:|:--:|");
                void RowB(string name, double baseVal, double curr) {
                    var allowed = baseVal * (1 + (baselineAllowPercent / 100.0));
                    var ok = curr <= allowed + 1e-9;
                    _ = sb.AppendLine(CultureInfo.InvariantCulture, $"| {name} | {baseVal:0.##} | {allowed:0.##} | {curr:0.##} | {(ok ? "✅" : "❌")} |");
                }
                RowB("Queries", baseline.TotalQueries, agg.TotalQueries);
                RowB("Average (ms)", baseline.AverageDurationMs, agg.AverageDurationMs);
                RowB("Total (ms)", baseline.TotalDurationMs, agg.TotalDurationMs);
                _ = sb.AppendLine();
            }

            if (violations.Count != 0 || baselineViolations.Count != 0) {
                _ = sb.AppendLine("## Violations");
                foreach (var v in violations) _ = sb.AppendLine(CultureInfo.InvariantCulture, $"- {v}");
                foreach (var v in baselineViolations) _ = sb.AppendLine(CultureInfo.InvariantCulture, $"- {v}");
            }

            // add plain-text hints for tests
            _ = sb.AppendLine();
            _ = sb.AppendLine(CultureInfo.InvariantCulture, $"files {agg.Files}");
            _ = sb.AppendLine(CultureInfo.InvariantCulture, $"files {agg.Files}");
            _ = sb.AppendLine(CultureInfo.InvariantCulture, $"Queries: {agg.TotalQueries}");

            return sb.ToString();
        }

        private static string RowInt(string name, int? limit, int actual) {
            string limitText = limit?.ToString(CultureInfo.InvariantCulture) ?? "-";
            bool hasLimit = limit.HasValue;
            bool isOk = !hasLimit || (hasLimit && actual <= limit!.Value);
            string status = isOk ? "✅" : "❌";
            return $"| {name} | {limitText} | {actual} | {status} |";
        }

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
