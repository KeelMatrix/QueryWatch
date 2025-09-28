
#nullable enable
using System.Text;
using System.Text.Json;
using KeelMatrix.QueryWatch.Cli.IO;
using KeelMatrix.QueryWatch.Cli.Options;
using KeelMatrix.QueryWatch.Contracts;

namespace KeelMatrix.QueryWatch.Cli.Core {
    internal sealed class Runner {
        public async Task<int> ExecuteAsync(CommandLineOptions opts) {
            // 1) Load inputs
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

            // 2) Aggregate
            var agg = Aggregated.From(summaries);

            // 2a) Optional: require full events
            if (opts.RequireFullEvents) {
                var anySampled = summaries.Any(s => s.Meta.TryGetValue("sampleTop", out var _))
                                 || agg.SampledEventsCount < agg.TotalQueries;
                if (anySampled) {
                    await Console.Error.WriteLineAsync("Events are sampled; rerun with a higher sampleTop to satisfy --require-full-events (meta.sampleTop found).").ConfigureAwait(false);
                    return ExitCodes.InvalidArguments;
                }
            }

            // 2b) Write baseline if requested (use default path if missing)
            if (opts.WriteBaseline) {
                var path = string.IsNullOrWhiteSpace(opts.BaselinePath) ? "querywatch.baseline.json" : opts.BaselinePath!;
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir!);
                var json = JsonSerializer.Serialize(agg.ToSummary(), QueryWatchJsonContext.Default.Summary); // 'Aggregated' does not contain a definition for 'ToSummary' and no accessible extension method 'ToSummary' accepting a first argument of type 'Aggregated' could be found (are you missing a using directive or an assembly reference?)
                await File.WriteAllTextAsync(path, json, Encoding.UTF8).ConfigureAwait(false);
                await Console.Out.WriteLineAsync($"Baseline written: {path}").ConfigureAwait(false);
                return ExitCodes.Ok;
            }

            // 3) Budgets
            var budgets = new List<PatternBudget>();
            foreach (var spec in opts.PatternBudgetSpecs) {
                if (!PatternBudget.TryParse(spec, out var b, out var err)) {
                    await Console.Error.WriteLineAsync($"Invalid --budget value '{spec}': {err}").ConfigureAwait(false);
                    return ExitCodes.InvalidArguments;
                }
                budgets.Add(b!);
            }

            var violations = new List<string>();
            if (opts.MaxQueries.HasValue && agg.TotalQueries > opts.MaxQueries.Value)
                violations.Add($"Total queries {agg.TotalQueries} > {opts.MaxQueries}");
            if (opts.MaxAverageMs.HasValue && agg.AverageDurationMs > opts.MaxAverageMs.Value)
                violations.Add($"Average {agg.AverageDurationMs:N2} ms > {opts.MaxAverageMs} ms");
            if (opts.MaxTotalMs.HasValue && agg.TotalDurationMs > opts.MaxTotalMs.Value)
                violations.Add($"Total {agg.TotalDurationMs:N2} ms > {opts.MaxTotalMs} ms");

            var patternFindings = PatternBudget.EvaluateBudgets(agg.Events, budgets); // 'PatternBudget' does not contain a definition for 'EvaluateBudgets'

            // 4) Baseline compare (if provided)
            bool baselineRegression = false;
            if (!string.IsNullOrWhiteSpace(opts.BaselinePath) && File.Exists(opts.BaselinePath)) {
                var baseJson = await File.ReadAllTextAsync(opts.BaselinePath!).ConfigureAwait(false);
                var baseline = JsonSerializer.Deserialize(baseJson, QueryWatchJsonContext.Default.Summary);
                if (baseline is not null) {
                    bool Over(double current, double baselineValue, double allowPercent) {
                        var limit = baselineValue * (1.0 + allowPercent / 100.0);
                        return current > limit;
                    }
                    var allow = opts.BaselineAllowPercent;
                    if (Over(agg.TotalQueries, baseline.TotalQueries, allow) ||
                        Over(agg.AverageDurationMs, baseline.AverageDurationMs, allow) ||
                        Over(agg.TotalDurationMs, baseline.TotalDurationMs, allow)) {
                        baselineRegression = true;
                        await Console.Error.WriteLineAsync("Baseline regressions:").ConfigureAwait(false);
                    }
                }
            }

            // 5) Emit summary
            var md = StepSummaryBuilder.Build(
                agg,
                opts.MaxQueries,
                opts.MaxAverageMs,
                opts.MaxTotalMs,
                violations,
                patternFindings,
                baseline: null,
                baselineAllowPercent: 0,
                baselineViolations: Array.Empty<string>());

            var gh = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (string.IsNullOrEmpty(gh)) {
                await Console.Out.WriteLineAsync(md).ConfigureAwait(false);
            }
            else {
                await File.AppendAllTextAsync(gh!, md, Encoding.UTF8).ConfigureAwait(false);
            }

            // 6) Exit code
            if (baselineRegression) return ExitCodes.BaselineRegression;
            if (violations.Count > 0 || patternFindings.Any(p => p.over)) return ExitCodes.BudgetExceeded;
            return ExitCodes.Ok;
        }
    }
}
