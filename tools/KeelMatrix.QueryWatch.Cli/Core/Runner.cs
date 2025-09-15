#nullable enable
using System.Text.Json;
using KeelMatrix.QueryWatch.Cli.IO;
using KeelMatrix.QueryWatch.Cli.Model;
using KeelMatrix.QueryWatch.Cli.Options;

namespace KeelMatrix.QueryWatch.Cli.Core {
    internal sealed class Runner {
        public async Task<int> ExecuteAsync(CommandLineOptions opts) {
            IReadOnlyList<Summary> summaries;
            try {
                summaries = await SummaryLoader.LoadAsync(opts.Inputs);
            }
            catch (FileNotFoundException fnf) {
                await Console.Error.WriteLineAsync(fnf.Message);
                return ExitCodes.InputFileNotFound;
            }
            catch (JsonException jex) {
                await Console.Error.WriteLineAsync(jex.Message);
                return ExitCodes.JsonParseError;
            }

            var agg = Aggregated.From(summaries);

            Console.WriteLine($"QueryWatch Summary (schema {agg.Schema}, files {summaries.Count})");
            Console.WriteLine($" - Started: {agg.StartedAt:u}");
            Console.WriteLine($" - Stopped: {agg.StoppedAt:u}");
            Console.WriteLine($" - Queries: {agg.TotalQueries}");
            Console.WriteLine($" - Total:   {agg.TotalDurationMs:N2} ms");
            Console.WriteLine($" - Average: {agg.AverageDurationMs:N2} ms");
            if (agg.SampledEventsCount < agg.TotalQueries)
                Console.WriteLine($" - Note: Events contain {agg.SampledEventsCount} sampled entries (top-N), total queries = {agg.TotalQueries}.");

            var exitCode = ExitCodes.Ok;

            // Hard budgets
            var violations = new List<string>();
            if (opts.MaxQueries.HasValue && agg.TotalQueries > opts.MaxQueries.Value)
                violations.Add($"MaxQueries={opts.MaxQueries.Value} but executed {agg.TotalQueries}.");
            if (opts.MaxAverageMs.HasValue && agg.AverageDurationMs > opts.MaxAverageMs.Value)
                violations.Add($"MaxAverageMs={opts.MaxAverageMs.Value} but actual {agg.AverageDurationMs:N2} ms.");
            if (opts.MaxTotalMs.HasValue && agg.TotalDurationMs > opts.MaxTotalMs.Value)
                violations.Add($"MaxTotalMs={opts.MaxTotalMs.Value} but actual {agg.TotalDurationMs:N2} ms.");

            if (violations.Count > 0) {
                await Console.Error.WriteLineAsync("Budget violations:");
                foreach (var v in violations) await Console.Error.WriteLineAsync(" - " + v);
                exitCode = Math.Max(exitCode, ExitCodes.BudgetExceeded);
            }

            // Pattern budgets
            var patternFindings = new List<(PatternBudget budget, int count, bool over)>();
            if (opts.PatternBudgetSpecs.Count > 0) {
                var texts = agg.Events.Select(e => e.Text ?? string.Empty);
                foreach (var spec in opts.PatternBudgetSpecs) {
                    if (!PatternBudget.TryParse(spec, out var b, out var err)) {
                        await Console.Error.WriteLineAsync($"Invalid --budget value '{spec}': {err}");
                        return ExitCodes.InvalidArguments;
                    }
                    var count = texts.Count(t => b!.Regex.IsMatch(t));
                    var over = count > b!.MaxCount;
                    patternFindings.Add((b!, count, over));
                    if (over) exitCode = Math.Max(exitCode, ExitCodes.BudgetExceeded);
                }
            }

            // Baseline comparison
            var baselineViolations = new List<string>();
            Summary? baseline = null;
            if (!string.IsNullOrWhiteSpace(opts.BaselinePath) && File.Exists(opts.BaselinePath)) {
                try {
                    await using var bstream = File.OpenRead(opts.BaselinePath);
                    baseline = await JsonSerializer.DeserializeAsync<Summary>(bstream).ConfigureAwait(false);
                }
                catch (Exception ex) {
                    await Console.Error.WriteLineAsync($"Baseline parse failed (ignored): {ex.Message}");
                }
            }

            if (!string.IsNullOrWhiteSpace(opts.BaselinePath) && baseline is null && opts.WriteBaseline) {
                try {
                    var dir = Path.GetDirectoryName(opts.BaselinePath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    await using var outStream = File.Create(opts.BaselinePath!);
                    await JsonSerializer.SerializeAsync(outStream, agg.ToSummary(), new JsonSerializerOptions { WriteIndented = true }).ConfigureAwait(false);
                    Console.WriteLine($"Baseline written: {opts.BaselinePath}");
                }
                catch (Exception ex) {
                    await Console.Error.WriteLineAsync($"Failed to write baseline: {ex.Message}");
                }
            }
            else if (baseline is not null) {
                var tol = opts.BaselineAllowPercent / 100.0;
                double allowedQueries = baseline.TotalQueries * (1.0 + tol);
                double allowedAvg = baseline.AverageDurationMs * (1.0 + tol);
                double allowedTotal = baseline.TotalDurationMs * (1.0 + tol);

                if (agg.TotalQueries > allowedQueries) baselineViolations.Add($"Queries increased beyond +{opts.BaselineAllowPercent:N2}%: {baseline.TotalQueries} -> {agg.TotalQueries}");
                if (agg.AverageDurationMs > allowedAvg) baselineViolations.Add($"AverageMs increased beyond +{opts.BaselineAllowPercent:N2}%: {baseline.AverageDurationMs:N2} -> {agg.AverageDurationMs:N2}");
                if (agg.TotalDurationMs > allowedTotal) baselineViolations.Add($"TotalMs increased beyond +{opts.BaselineAllowPercent:N2}%: {baseline.TotalDurationMs:N2} -> {agg.TotalDurationMs:N2}");

                if (baselineViolations.Count > 0) {
                    await Console.Error.WriteLineAsync("Baseline regressions:");
                    foreach (var v in baselineViolations) await Console.Error.WriteLineAsync(" - " + v);
                    exitCode = Math.Max(exitCode, ExitCodes.BaselineRegression);
                }
            }

            // Step summary for GitHub Actions
            var stepSummaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (!string.IsNullOrWhiteSpace(stepSummaryPath)) {
                var md = StepSummaryBuilder.Build(agg, opts.MaxQueries, opts.MaxAverageMs, opts.MaxTotalMs, violations, patternFindings, baseline, opts.BaselineAllowPercent, baselineViolations);
                try { await File.AppendAllTextAsync(stepSummaryPath!, md); } catch { /* ignore */ }
            }

            if (exitCode == ExitCodes.Ok) Console.WriteLine("QueryWatch gate: OK");
            return exitCode;
        }
    }
}
