#nullable enable
using System.Text;
using System.Text.Json;
using KeelMatrix.QueryWatch.Cli.IO;
using KeelMatrix.QueryWatch.Cli.Model;
using KeelMatrix.QueryWatch.Cli.Options;

namespace KeelMatrix.QueryWatch.Cli.Core {
    internal sealed class Runner {
        public async Task<int> ExecuteAsync(CommandLineOptions opts) {
            // 1) Load inputs with friendly JSON error mapping
            IReadOnlyList<Summary> summaries;
            try {
                summaries = await SummaryLoader.LoadAsync(opts.Inputs).ConfigureAwait(false);
            }
            // Map our custom loader exceptions to deterministic exit codes
            catch (InputFileNotFoundException fex) {
                await Console.Error.WriteLineAsync(fex.Message).ConfigureAwait(false);
                return ExitCodes.InputFileNotFound; // 2
            }
            catch (JsonParseException jex) {
                await Console.Error.WriteLineAsync(jex.Message).ConfigureAwait(false);
                return ExitCodes.JsonParseError; // 3
            }
            // Also keep the generic mappings as a safety net (shouldn't normally trigger)
            catch (FileNotFoundException fex) {
                await Console.Error.WriteLineAsync(fex.Message).ConfigureAwait(false);
                return ExitCodes.InputFileNotFound;
            }
            catch (DirectoryNotFoundException dex) {
                await Console.Error.WriteLineAsync(dex.Message).ConfigureAwait(false);
                return ExitCodes.InputFileNotFound;
            }
            catch (JsonException jex) {
                await Console.Error.WriteLineAsync(jex.Message).ConfigureAwait(false);
                return ExitCodes.JsonParseError;
            }

            // 2) Aggregate
            var agg = Aggregated.From(summaries);

            // 2a) Optional: require full (unsampled) events
            if (opts.RequireFullEvents) {
                // If any input explicitly declares sampling (meta.sampleTop), or if the
                // aggregated count of events is less than total queries, fail early.
                var anyDeclaresSampleTop = summaries.Any(s => s.Meta != null && s.Meta.ContainsKey("sampleTop"));
                var appearsSampled = agg.SampledEventsCount < agg.TotalQueries;

                if (anyDeclaresSampleTop || appearsSampled) {
                    await Console.Error.WriteLineAsync(
                        "Input summaries appear to be top-N sampled (meta contains 'sampleTop' or events < total queries). " +
                        "Re-export with a higher sampleTop or full events to use --require-full-events.")
                        .ConfigureAwait(false);
                    return ExitCodes.InvalidArguments; // 1
                }
            }

            // 3) Verbose console header (kept for DX)
            Console.WriteLine($"QueryWatch Summary (schema {agg.Schema}, files {summaries.Count})");
            Console.WriteLine($" - Started: {agg.StartedAt:u}");
            Console.WriteLine($" - Stopped: {agg.StoppedAt:u}");
            Console.WriteLine($" - Queries: {agg.TotalQueries}");
            Console.WriteLine($" - Total:   {agg.TotalDurationMs:N2} ms");
            Console.WriteLine($" - Average: {agg.AverageDurationMs:N2} ms");
            if (agg.SampledEventsCount < agg.TotalQueries)
                Console.WriteLine($" - Note: Events contain {agg.SampledEventsCount} sampled entries (top-N), total queries = {agg.TotalQueries}.");

            var exitCode = ExitCodes.Ok;

            // 4) Hard budgets (max-queries / avg-ms / total-ms)
            var violations = new List<string>();
            if (opts.MaxQueries.HasValue && agg.TotalQueries > opts.MaxQueries.Value)
                violations.Add($"MaxQueries={opts.MaxQueries.Value} but executed {agg.TotalQueries}.");
            if (opts.MaxAverageMs.HasValue && agg.AverageDurationMs > opts.MaxAverageMs.Value)
                violations.Add($"MaxAverageMs={opts.MaxAverageMs.Value} but actual {agg.AverageDurationMs:N2} ms.");
            if (opts.MaxTotalMs.HasValue && agg.TotalDurationMs > opts.MaxTotalMs.Value)
                violations.Add($"MaxTotalMs={opts.MaxTotalMs.Value} but actual {agg.TotalDurationMs:N2} ms.");

            if (violations.Count > 0)
                exitCode = Math.Max(exitCode, ExitCodes.BudgetExceeded);

            // 5) Pattern budgets (derived from event texts)
            var patternFindings = new List<(PatternBudget budget, int count, bool over)>();
            if (opts.PatternBudgetSpecs.Count > 0) {
                var texts = agg.Events.Select(e => e.Text ?? string.Empty);
                foreach (var spec in opts.PatternBudgetSpecs) {
                    if (!PatternBudget.TryParse(spec, out var b, out var err)) {
                        await Console.Error.WriteLineAsync($"Invalid --budget value '{spec}': {err}").ConfigureAwait(false);
                        return ExitCodes.InvalidArguments;
                    }

                    var count = texts.Count(t => b!.Regex.IsMatch(t));
                    var over = count > b!.MaxCount;
                    patternFindings.Add((b!, count, over));
                    if (over)
                        exitCode = Math.Max(exitCode, ExitCodes.BudgetExceeded);
                }
            }

            // 6) Baseline compare (optional)
            Summary? baseline = null;
            var baselineViolations = new List<string>();
            if (!string.IsNullOrWhiteSpace(opts.BaselinePath) && !opts.WriteBaseline) {
                try {
                    var loaded = await SummaryLoader.LoadAsync(new[] { opts.BaselinePath! }).ConfigureAwait(false);
                    baseline = loaded.Single();
                    var tol = opts.BaselineAllowPercent / 100.0;

                    double allowedQueries = baseline.TotalQueries * (1.0 + tol);
                    double allowedAvg = baseline.AverageDurationMs * (1.0 + tol);
                    double allowedTotal = baseline.TotalDurationMs * (1.0 + tol);

                    if (agg.TotalQueries > allowedQueries)
                        baselineViolations.Add($"Queries exceeded by > {opts.BaselineAllowPercent:N2}%: {baseline.TotalQueries} -> {agg.TotalQueries}");
                    if (agg.AverageDurationMs > allowedAvg)
                        baselineViolations.Add($"Average ms exceeded by > {opts.BaselineAllowPercent:N2}%: {baseline.AverageDurationMs:N2} -> {agg.AverageDurationMs:N2}");
                    if (agg.TotalDurationMs > allowedTotal)
                        baselineViolations.Add($"Total ms exceeded by > {opts.BaselineAllowPercent:N2}%: {baseline.TotalDurationMs:N2} -> {agg.TotalDurationMs:N2}");

                    if (baselineViolations.Count > 0)
                        exitCode = Math.Max(exitCode, ExitCodes.BaselineRegression);
                }
                catch (Exception ex) {
                    // Keep friendly & non-fatal (users sometimes forget to publish the baseline artifact)
                    await Console.Error.WriteLineAsync($"Baseline parse failed (ignored): {ex.Message}").ConfigureAwait(false);
                }
            }

            // 7) Write baseline if requested
            if (!string.IsNullOrWhiteSpace(opts.BaselinePath) && baseline is null && opts.WriteBaseline) {
                try {
                    var dir = Path.GetDirectoryName(opts.BaselinePath);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                    await using var outStream = File.Create(opts.BaselinePath!);
                    await JsonSerializer.SerializeAsync(outStream, summaries.Single(), new JsonSerializerOptions { WriteIndented = true }).ConfigureAwait(false);
                    Console.WriteLine($"Baseline written: {opts.BaselinePath}");
                }
                catch (Exception ex) {
                    await Console.Error.WriteLineAsync($"Failed to write baseline: {ex.Message}").ConfigureAwait(false);
                    return ExitCodes.InvalidArguments;
                }
            }
            else if (baseline is not null && baselineViolations.Count > 0) {
                await Console.Error.WriteLineAsync("Baseline regressions:").ConfigureAwait(false);
                foreach (var v in baselineViolations)
                    await Console.Error.WriteLineAsync(" - " + v).ConfigureAwait(false);
            }

            // 8) Concise lines for CI/tests
            var ok = exitCode == ExitCodes.Ok;
            Console.WriteLine($"QueryWatch gate: {(ok ? "OK" : "FAIL")}");
            Console.WriteLine($"files {summaries.Count}");
            Console.WriteLine($"Queries: {agg.TotalQueries}");
            if (patternFindings.Any(f => f.over))
                Console.WriteLine("Pattern budget violations detected.");

            // 9) GitHub Step Summary (append, ANSI-free)
            var stepSummaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
            if (!string.IsNullOrWhiteSpace(stepSummaryPath)) {
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

                // UTF8 without BOM; append
                var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                await File.AppendAllTextAsync(stepSummaryPath!, md, utf8).ConfigureAwait(false);
            }

            // 10) Emit budget violations to stderr for diagnosability
            if (violations.Count > 0) {
                await Console.Error.WriteLineAsync("Budget violations:").ConfigureAwait(false);
                foreach (var v in violations)
                    await Console.Error.WriteLineAsync(" - " + v).ConfigureAwait(false);
            }

            return exitCode;
        }
    }
}
