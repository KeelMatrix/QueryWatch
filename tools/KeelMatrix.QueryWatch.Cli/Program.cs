// Copyright (c) KeelMatrix
using System.Globalization;
using System.Text.Json;
using KeelMatrix.QueryWatch.Cli.Core;
using KeelMatrix.QueryWatch.Contracts;

class Program {
    static int Main(string[] args) {
        try {
            return Run(args);
        }
        catch (ArgumentException ex) {
            Console.Error.WriteLine(ex.Message);
            return ExitCodes.InvalidArguments;
        }
    }

    static int Run(string[] args) {
        var inputs = new List<string>();
        int? maxQueries = null;
        double? maxAvgMs = null;
        double? maxTotalMs = null;
        string? baselinePath = null;
        bool writeBaseline = false;
        double baselineAllowPercent = 0;
        bool requireFullEvents = false;
        var budgets = new List<PatternBudget>();

        for (int i = 0; i < args.Length; i++) {
            var a = args[i];
            switch (a) {
                case "--input":
                    if (++i >= args.Length) throw new ArgumentException("Missing value for --input");
                    inputs.Add(args[i]);
                    break;
                case "--max-queries":
                    if (++i >= args.Length) throw new ArgumentException("Missing value for --max-queries");
                    maxQueries = int.Parse(args[i], CultureInfo.InvariantCulture);
                    break;
                case "--max-average-ms":
                    if (++i >= args.Length) throw new ArgumentException("Missing value for --max-average-ms");
                    maxAvgMs = double.Parse(args[i], CultureInfo.InvariantCulture);
                    break;
                case "--max-total-ms":
                    if (++i >= args.Length) throw new ArgumentException("Missing value for --max-total-ms");
                    maxTotalMs = double.Parse(args[i], CultureInfo.InvariantCulture);
                    break;
                case "--baseline":
                    if (++i >= args.Length) throw new ArgumentException("Missing value for --baseline");
                    baselinePath = args[i];
                    break;
                case "--baseline-allow-percent":
                    if (++i >= args.Length) throw new ArgumentException("Missing value for --baseline-allow-percent");
                    baselineAllowPercent = double.Parse(args[i], CultureInfo.InvariantCulture);
                    break;
                case "--write-baseline":
                    writeBaseline = true;
                    break;
                case "--require-full-events":
                    requireFullEvents = true;
                    break;
                case "--budget":
                    if (++i >= args.Length) {
                        Console.Error.WriteLine("Missing value for --budget");
                        return ExitCodes.InvalidArguments;
                    }
                    if (!PatternBudget.TryParse(args[i], out var b, out var err) || b is null) {
                        Console.Error.WriteLine(err ?? "Invalid --budget");
                        return ExitCodes.InvalidArguments;
                    }
                    budgets.Add(b);
                    break;
                default:
                    // ignore unknowns for now
                    break;
            }
        }

        if (writeBaseline && string.IsNullOrWhiteSpace(baselinePath)) {
            Console.Error.WriteLine("Cannot use --write-baseline without --baseline");
            return ExitCodes.InvalidArguments;
        }

        var tried = new List<string>();
        var summaries = new List<Summary>();
        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        foreach (var path in inputs) {
            if (!File.Exists(path)) {
                tried.Add(path);
                continue;
            }
            try {
                using var fs = File.OpenRead(path);
                var s = JsonSerializer.Deserialize(fs, QueryWatchJsonContext.Default.Summary);
                if (s is null) throw new JsonException("null");
                summaries.Add(s);
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Failed to parse JSON: {ex.Message}");
                return ExitCodes.JsonParseError;
            }
        }

        if (summaries.Count == 0) {
            Console.Error.WriteLine("No input JSON found.");
            foreach (var p in inputs.Concat(tried)) Console.Error.WriteLine("Missing: " + p);
            return ExitCodes.InputFileNotFound;
        }

        if (requireFullEvents && summaries.Any(s => s.Meta is not null && s.Meta.ContainsKey("sampleTop"))) {
            Console.Error.WriteLine("One or more inputs are sampled (meta.sampleTop present); rerun with full events.");
            return ExitCodes.InvalidArguments;
        }

        var agg = Aggregated.From(summaries);

        // budgets
        var violations = new List<string>();
        if (maxQueries.HasValue && agg.TotalQueries > maxQueries.Value) violations.Add($"MaxQueries {maxQueries} < {agg.TotalQueries}");
        if (maxAvgMs.HasValue && agg.AverageDurationMs > maxAvgMs.Value + 1e-9) violations.Add($"MaxAverageMs {maxAvgMs} < {agg.AverageDurationMs:0.##}");
        if (maxTotalMs.HasValue && agg.TotalDurationMs > maxTotalMs.Value + 1e-9) violations.Add($"MaxTotalMs {maxTotalMs} < {agg.TotalDurationMs:0.##}");

        var patternFindings = new List<(PatternBudget budget, int count, bool over)>();
        if (budgets.Count > 0) {
            var corpus = summaries.SelectMany(s => s.Events ?? Array.Empty<EventSample>()).Select(e => e.Text ?? string.Empty).ToList();
            foreach (var b in budgets) {
                var count = b.CountMatches(corpus);
                var over = count > b.Max;
                patternFindings.Add((b, count, over));
                if (over) violations.Add($"Pattern '{b.Raw}' exceeded: {count} > {b.Max}");
            }
        }

        Summary? baseline = null;
        var baselineViolations = new List<string>();
        if (!string.IsNullOrWhiteSpace(baselinePath) && File.Exists(baselinePath)) {
            try {
                using var fs = File.OpenRead(baselinePath);
                baseline = JsonSerializer.Deserialize(fs, QueryWatchJsonContext.Default.Summary);
            }
            catch (Exception ex) {
                Console.Error.WriteLine($"Failed to parse JSON: {ex.Message}");
                return ExitCodes.JsonParseError;
            }

            if (baseline is not null) {
                double allowedQ = baseline.TotalQueries * (1 + baselineAllowPercent / 100.0);
                double allowedAvg = baseline.AverageDurationMs * (1 + baselineAllowPercent / 100.0);
                double allowedTotal = baseline.TotalDurationMs * (1 + baselineAllowPercent / 100.0);

                if (agg.TotalQueries > allowedQ + 1e-9) baselineViolations.Add($"Queries {agg.TotalQueries} > allowed {allowedQ:0.##}");
                if (agg.AverageDurationMs > allowedAvg + 1e-9) baselineViolations.Add($"Average {agg.AverageDurationMs:0.##} > allowed {allowedAvg:0.##}");
                if (agg.TotalDurationMs > allowedTotal + 1e-9) baselineViolations.Add($"Total {agg.TotalDurationMs:0.##} > allowed {allowedTotal:0.##}");
            }
        }

        // write baseline if asked
        if (writeBaseline) {
            var outDir = Path.GetDirectoryName(baselinePath!)!;
            if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir)) Directory.CreateDirectory(outDir);
            var current = new Summary {
                Schema = "1.0.0",
                StartedAt = summaries.First().StartedAt,
                StoppedAt = summaries.Last().StoppedAt,
                TotalQueries = agg.TotalQueries,
                TotalDurationMs = agg.TotalDurationMs,
                AverageDurationMs = agg.AverageDurationMs,
                Events = Array.Empty<EventSample>(),
                Meta = new Dictionary<string, string>()
            };
            var json = JsonSerializer.Serialize(current, QueryWatchJsonContext.Default.Summary);
            File.WriteAllText(baselinePath!, json);
            Console.WriteLine("Baseline written: " + baselinePath);
        }

        var md = StepSummaryBuilder.Build(agg, maxQueries, maxAvgMs, maxTotalMs, violations, patternFindings, baseline, baselineAllowPercent, baselineViolations);
        Console.WriteLine(md);

        // GITHUB_STEP_SUMMARY append if present
        var gh = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (!string.IsNullOrWhiteSpace(gh)) {
            try {
                File.AppendAllText(gh!, md);
            }
            catch { /* ignore */ }
        }

        // choose exit code
        if (baselineViolations.Count > 0) {
            Console.Error.WriteLine("Baseline regressions:");
            foreach (var v in baselineViolations) Console.Error.WriteLine(" - " + v);
            return ExitCodes.BaselineRegression;
        }
        if (violations.Count > 0) {
            Console.Error.WriteLine("Budget violations:");
            foreach (var v in violations) Console.Error.WriteLine(" - " + v);
            return ExitCodes.BudgetExceeded;
        }

        return ExitCodes.Ok;
    }
}
