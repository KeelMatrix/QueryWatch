#nullable enable
using System.IO;
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

            // 1a) Friendly schema-forward warning (never fails the run)
            var toolSchema = new Summary().Schema; // single source from contracts default
            if (summaries.Any(s => IsNewerSchema(s.Schema, toolSchema))) {
                await Console.Error
                    .WriteLineAsync($"Warning: one or more inputs use a newer schema than this tool supports (input>'{toolSchema}'). Some fields may be ignored. Consider upgrading qwatch.")
                    .ConfigureAwait(false);
            }

            // 2) Aggregate
            var agg = Aggregated.From(summaries);

            // 2a) --require-full-events: fail only if meta.sampleTop is present
            if (opts.RequireFullEvents) {
                bool anySampled = summaries.Any(s => s.Meta?.ContainsKey("sampleTop") == true);
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
                    Schema = toolSchema,
                    StartedAt = summaries[0].StartedAt,
                    StoppedAt = summaries[^1].StoppedAt,
                    TotalQueries = agg.TotalQueries,
                    TotalDurationMs = agg.TotalDurationMs,
                    AverageDurationMs = agg.AverageDurationMs,
                    Events = Array.Empty<EventSample>(),
                    Meta = []
                };

                var json = JsonSerializer.Serialize(current, QueryWatchJsonContext.Default.Summary);
                await File.WriteAllTextAsync(outPath, json, Encoding.UTF8).ConfigureAwait(false);
                await Console.Out.WriteLineAsync("Baseline written: " + outPath).ConfigureAwait(false);
            }

            // 3) Numeric budgets
            var violations = new List<string>();
            if (opts.MaxQueries.HasValue && agg.TotalQueries > opts.MaxQueries.Value) {
                violations.Add($"Max queries exceeded: {agg.TotalQueries} > {opts.MaxQueries.Value}");
            }
            if (opts.MaxAverageMs.HasValue && agg.AverageDurationMs > opts.MaxAverageMs.Value + 1e-9) {
                violations.Add($"Max average duration exceeded: {agg.AverageDurationMs:0.##} > {opts.MaxAverageMs.Value:0.##} ms");
            }
            if (opts.MaxTotalMs.HasValue && agg.TotalDurationMs > opts.MaxTotalMs.Value + 1e-9) {
                violations.Add($"Max total duration exceeded: {agg.TotalDurationMs:0.##} > {opts.MaxTotalMs.Value:0.##} ms");
            }

            // 4) Pattern budgets
            var patternFindings = new List<(PatternBudget budget, int count, bool over)>();
            foreach (var spec in opts.PatternBudgetSpecs) {
                if (!PatternBudget.TryParse(spec, out var budget, out var error)) {
                    await Console.Error.WriteLineAsync(error).ConfigureAwait(false);
                    return ExitCodes.InvalidArguments;
                }
                var count = budget!.CountMatches(agg.Events.Select(e => e.Text!));
                bool over = count > budget.Max;
                patternFindings.Add((budget, count, over));
                if (over) {
                    violations.Add($"{budget.Raw} \u2192 {count} > {budget.Max}");
                }
            }

            // 5) Baseline comparison (if provided and not writing)
            Summary? baseline = null;
            var baselineViolations = new List<string>();
            if (!opts.WriteBaseline && !string.IsNullOrWhiteSpace(opts.BaselinePath)) {
                try {
                    baseline = (await SummaryLoader.LoadAsync([opts.BaselinePath!]).ConfigureAwait(false)).FirstOrDefault();
                }
                catch (InputFileNotFoundException ex) {
                    await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
                    return ExitCodes.InputFileNotFound;
                }
                catch (JsonParseException ex) {
                    await Console.Error.WriteLineAsync(ex.Message).ConfigureAwait(false);
                    return ExitCodes.JsonParseError;
                }

                if (baseline is not null) {
                    var p = opts.BaselineAllowPercent;
                    double Allowed(double baseVal) => baseVal * (1 + (p / 100.0));
                    void Check(string name, double baseVal, double curr) {
                        var allowed = Allowed(baseVal);
                        if (curr > allowed + 1e-9)
                            baselineViolations.Add($"{name}: {curr:0.##} > {allowed:0.##} (baseline {baseVal:0.##}, +{p:0.##}% allowed)");
                    }
                    Check("Queries", baseline.TotalQueries, agg.TotalQueries);
                    Check("Average (ms)", baseline.AverageDurationMs, agg.AverageDurationMs);
                    Check("Total (ms)", baseline.TotalDurationMs, agg.TotalDurationMs);

                    if (baselineViolations.Count > 0) {
                        await Console.Error.WriteLineAsync("Baseline regressions:").ConfigureAwait(false);
                        foreach (var v in baselineViolations)
                            await Console.Error.WriteLineAsync("  - " + v).ConfigureAwait(false);
                    }
                }
            }

            // 6) Emit Markdown step summary (GitHub Actions)
            var summaryMd = StepSummaryBuilder.Build(
                agg,
                maxQueries: opts.MaxQueries,
                maxAvgMs: opts.MaxAverageMs,
                maxTotalMs: opts.MaxTotalMs,
                violations: violations.Where(v => v.StartsWith("Max ", StringComparison.Ordinal)).ToList(),
                patternFindings: patternFindings,
                baseline: baseline,
                baselineAllowPercent: opts.BaselineAllowPercent,
                baselineViolations: baselineViolations);
            var stepSummaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (!string.IsNullOrWhiteSpace(stepSummaryPath)) {
                try {
                    Directory.CreateDirectory(Path.GetDirectoryName(stepSummaryPath)!);
                }
                catch { /* ignore */ }
                try {
                    await File.AppendAllTextAsync(stepSummaryPath!, summaryMd + Environment.NewLine);
                }
                catch { /* swallow for CI resiliency */ }
            }

            // 7) Choose exit code
            if (baselineViolations.Count > 0) {
                return ExitCodes.BaselineRegression;
            }
            if (violations.Count > 0) {
                await Console.Error.WriteLineAsync("Budget violations:").ConfigureAwait(false);
                foreach (var v in violations) {
                    await Console.Error.WriteLineAsync(" - " + v).ConfigureAwait(false);
                }
                return ExitCodes.BudgetExceeded;
            }

            // 8) Print a compact success line for logs + tests
            var sb = new StringBuilder();
            sb.AppendLine($"files {agg.Files}");
            sb.AppendLine($"Queries: {agg.TotalQueries}");
            await Console.Out.WriteLineAsync(sb.ToString()).ConfigureAwait(false);

            return ExitCodes.Ok;
        }

        private static bool IsNewerSchema(string? input, string? tool) {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(tool)) return false;
            static (int a, int b, int c) Parse(string s) {
                var parts = s.Split('.', 3, StringSplitOptions.RemoveEmptyEntries);
                int A = parts.Length > 0 && int.TryParse(parts[0], out var x) ? x : 0;
                int B = parts.Length > 1 && int.TryParse(parts[1], out var y) ? y : 0;
                int C = parts.Length > 2 && int.TryParse(parts[2], out var z) ? z : 0;
                return (A, B, C);
            }
            var (a, b, c) = Parse(input);
            var t = Parse(tool);
            if (a != t.a) return a > t.a;
            if (b != t.b) return b > t.b;
            return c > t.c;
        }
    }
}
