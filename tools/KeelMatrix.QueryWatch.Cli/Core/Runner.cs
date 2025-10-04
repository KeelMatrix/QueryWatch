#nullable enable
using System.Linq;
using System.Text;
using System.Text.Json;
using KeelMatrix.QueryWatch.Cli.IO;
using KeelMatrix.QueryWatch.Cli.Options;
using KeelMatrix.QueryWatch.Contracts;

namespace KeelMatrix.QueryWatch.Cli.Core {
    internal static class Runner {
        public static async Task<int> ExecuteAsync(CommandLineOptions opts) {
            // 1) Load inputs (per-file JSON parse with hard failures)
            IReadOnlyList<Summary> summaries;
            try {
                summaries = await SummaryLoader.LoadAsync(opts.Inputs).ConfigureAwait(false);
            }
            catch (InputFileNotFoundException dex) {
                await Console.Error.WriteLineAsync(dex.Message).ConfigureAwait(false);
                return ExitCodes.InputFileNotFound;
            }
            catch (JsonParseException jex) {
                await Console.Error.WriteLineAsync(jex.Message).ConfigureAwait(false);
                return ExitCodes.JsonParseError;
            }

            if (summaries.Count == 0) {
                await Console.Error.WriteLineAsync("No input JSON found.").ConfigureAwait(false);
                foreach (var p in from p in opts.Inputs
                                  where !File.Exists(p)
                                  select p) {
                    await Console.Error.WriteLineAsync("Missing: " + p).ConfigureAwait(false);
                }

                return ExitCodes.InputFileNotFound;
            }

            // 2) Aggregate
            var agg = Aggregated.From(summaries);

            // 2a) --require-full-events: fail only if meta.sampleTop is present
            if (opts.RequireFullEvents) {
                bool anySampled = summaries.Any(s => s.Meta is not null && s.Meta.ContainsKey("sampleTop"));
                if (anySampled) {
                    await Console.Error
                        .WriteLineAsync("One or more inputs are sampled (meta.sampleTop present); rerun with full events.")
                        .ConfigureAwait(false);
                    return ExitCodes.InvalidArguments;
                }
            }

            // 2b) --write-baseline requires --baseline
            if (opts.WriteBaseline && string.IsNullOrWhiteSpace(opts.BaselinePath)) {
                await Console.Error.WriteLineAsync("Cannot use --write-baseline without --baseline").ConfigureAwait(false);
                return ExitCodes.InvalidArguments;
            }

            // 2c) Write baseline snapshot
            if (opts.WriteBaseline) {
                var outPath = opts.BaselinePath!;
                var outDir = Path.GetDirectoryName(outPath);
                if (!string.IsNullOrEmpty(outDir) && !Directory.Exists(outDir))
                    Directory.CreateDirectory(outDir!);

                var current = new Summary {
                    Schema = "1.0.0",
                    StartedAt = summaries[0].StartedAt,
                    StoppedAt = summaries[^1].StoppedAt,
                    TotalQueries = agg.TotalQueries,
                    TotalDurationMs = agg.TotalDurationMs,
                    AverageDurationMs = agg.AverageDurationMs,
                    Events = Array.Empty<EventSample>(),
                    Meta = new Dictionary<string, string>()
                };

                var json = JsonSerializer.Serialize(current, QueryWatchJsonContext.Default.Summary);
                await File.WriteAllTextAsync(outPath, json, Encoding.UTF8).ConfigureAwait(false);
                await Console.Out.WriteLineAsync("Baseline written: " + outPath).ConfigureAwait(false);
            }

            // 3) Numeric budgets
            var violations = new List<string>();
            if (opts.MaxQueries.HasValue && agg.TotalQueries > opts.MaxQueries.Value)
                violations.Add($"MaxQueries {opts.MaxQueries} < {agg.TotalQueries}");
            if (opts.MaxAverageMs.HasValue && agg.AverageDurationMs > opts.MaxAverageMs.Value + 1e-9)
                violations.Add($"MaxAverageMs {opts.MaxAverageMs} < {agg.AverageDurationMs:0.##}");
            if (opts.MaxTotalMs.HasValue && agg.TotalDurationMs > opts.MaxTotalMs.Value + 1e-9)
                violations.Add($"MaxTotalMs {opts.MaxTotalMs} < {agg.TotalDurationMs:0.##}");

            // 3a) Pattern budgets
            var parsedBudgets = new List<PatternBudget>();
            foreach (var raw in opts.PatternBudgetSpecs ?? Enumerable.Empty<string>()) {
                if (!PatternBudget.TryParse(raw, out var budget, out var error) || budget is null) {
                    await Console.Error.WriteLineAsync(error ?? "Invalid --budget").ConfigureAwait(false);
                    return ExitCodes.InvalidArguments;
                }
                parsedBudgets.Add(budget);
            }

            var patternFindings = new List<(PatternBudget budget, int count, bool over)>();
            if (parsedBudgets.Count > 0) {
                var corpus = summaries
                    .SelectMany(s => s.Events ?? Array.Empty<EventSample>())
                    .Select(e => e.Text ?? string.Empty)
                    .ToList();

                foreach (var b in parsedBudgets) {
                    var count = b.CountMatches(corpus);
                    var over = count > b.Max;
                    patternFindings.Add((b, count, over));
                    if (over) violations.Add($"Pattern '{b.Raw}' exceeded: {count} > {b.Max}");
                }
            }

            // 4) Baseline compare — malformed baseline is a hard JsonParseError
            Summary? baseline = null;
            var baselineViolations = new List<string>();
            if (!string.IsNullOrWhiteSpace(opts.BaselinePath) && File.Exists(opts.BaselinePath)) {
                try {
                    var baseJson = await File.ReadAllTextAsync(opts.BaselinePath!).ConfigureAwait(false);
                    baseline = JsonSerializer.Deserialize(baseJson, QueryWatchJsonContext.Default.Summary);
                }
                catch (Exception ex) {
                    await Console.Error.WriteLineAsync($"Failed to parse JSON: {ex.Message}").ConfigureAwait(false);
                    return ExitCodes.JsonParseError;
                }

                if (baseline is not null) {
                    double allowedQ = baseline.TotalQueries * (1 + opts.BaselineAllowPercent / 100.0);
                    double allowedAvg = baseline.AverageDurationMs * (1 + opts.BaselineAllowPercent / 100.0);
                    double allowedTot = baseline.TotalDurationMs * (1 + opts.BaselineAllowPercent / 100.0);

                    if (agg.TotalQueries > allowedQ + 1e-9) baselineViolations.Add($"Queries {agg.TotalQueries} > allowed {allowedQ:0.##}");
                    if (agg.AverageDurationMs > allowedAvg + 1e-9) baselineViolations.Add($"Average {agg.AverageDurationMs:0.##} > allowed {allowedAvg:0.##}");
                    if (agg.TotalDurationMs > allowedTot + 1e-9) baselineViolations.Add($"Total {agg.TotalDurationMs:0.##} > allowed {allowedTot:0.##}");
                }
            }

            // 5) Step summary — always stdout + append to GH summary if present
            var md = StepSummaryBuilder.Build(
                agg,
                opts.MaxQueries,
                opts.MaxAverageMs,
                opts.MaxTotalMs,
                violations,
                patternFindings,
                baseline,
                opts.BaselineAllowPercent,
                baselineViolations);

            await Console.Out.WriteLineAsync(md).ConfigureAwait(false);
            var gh = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (!string.IsNullOrWhiteSpace(gh)) {
                try { await File.AppendAllTextAsync(gh!, md, Encoding.UTF8).ConfigureAwait(false); } catch { /* ignore */ }
            }

            // 6) stderr banners
            if (baselineViolations.Count > 0) {
                await Console.Error.WriteLineAsync("Baseline regressions:").ConfigureAwait(false);
                foreach (var v in baselineViolations)
                    await Console.Error.WriteLineAsync(" - " + v).ConfigureAwait(false);
            }
            if (violations.Count > 0) {
                await Console.Error.WriteLineAsync("Budget violations:").ConfigureAwait(false);
                foreach (var v in violations)
                    await Console.Error.WriteLineAsync(" - " + v).ConfigureAwait(false);
            }

            // 7) Exit codes
            if (baselineViolations.Count > 0) return ExitCodes.BaselineRegression;  // 5
            if (violations.Count > 0) return ExitCodes.BudgetExceeded;              // 4
            return ExitCodes.Ok;                                                    // 0
        }
    }
}
