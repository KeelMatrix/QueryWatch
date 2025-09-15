// Copyright (c) KeelMatrix
#nullable enable
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using KeelMatrix.QueryWatch.Cli;

// QueryWatch CLI gate (NuGet-production-ready style).
// Adds in this sprint:
//  - Multi-file support: repeat --input to aggregate multiple JSON summaries.
//  - Baseline tolerance: --baseline-allow-percent <P> allows +P% vs baseline before failing.
//  - Per-pattern budgets: --budget "<pattern>=<maxCount>" (pattern supports wildcards * ? or use regex: prefix).
//  - GitHub PR annotations: writes a Markdown summary to $GITHUB_STEP_SUMMARY (if set).
//
// Usage examples:
//   dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --input artifacts/qwatch1.json --input artifacts/qwatch2.json --max-queries 50
//   dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --input artifacts/qwatch.json --baseline artifacts/base.json --baseline-allow-percent 10
//   dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --input artifacts/qwatch.json --budget "SELECT * FROM Users*=1"
//
// Exit codes:
//   0  OK
//   1  Invalid arguments
//   2  Input file not found
//   3  JSON parse error
//   4  Budget thresholds exceeded
//   5  Baseline regression detected
//
// Notes:
//  - Pattern budgets evaluate against the 'events' contained in each JSON summary, which are
//    the top-N slowest queries by duration (controlled by the exporter 'sampleTop').
//    For strict coverage, export with a high sampleTop.
return await RunAsync(args);

static async Task<int> RunAsync(string[] args) {
    static int ExitWith(string message, int code) {
        Console.Error.WriteLine(message);
        return code;
    }

    var inputs = new List<string>();
    int? maxQueries = null;
    double? maxAvgMs = null;
    double? maxTotalMs = null;
    string? baselinePath = null;
    bool writeBaseline = false;
    double baselineAllowPercent = 0.0;

    var patternBudgets = new List<PatternBudget>();

    int p = 0;
    int n = args.Length;

    while (p < n) {
        var arg = args[p];

        switch (arg) {
            case "--input":
                if (p + 1 >= n) return ExitWith("Missing value for --input", 1);
                inputs.Add(args[p + 1]);
                p += 2;
                break;

            case "--max-queries":
                if (p + 1 >= n) return ExitWith("Missing value for --max-queries", 1);
                if (!int.TryParse(args[p + 1], out var mq)) return ExitWith("Invalid integer for --max-queries", 1);
                maxQueries = mq;
                p += 2;
                break;

            case "--max-average-ms":
                if (p + 1 >= n) return ExitWith("Missing value for --max-average-ms", 1);
                if (!double.TryParse(args[p + 1], out var mav)) return ExitWith("Invalid number for --max-average-ms", 1);
                maxAvgMs = mav;
                p += 2;
                break;

            case "--max-total-ms":
                if (p + 1 >= n) return ExitWith("Missing value for --max-total-ms", 1);
                if (!double.TryParse(args[p + 1], out var mtv)) return ExitWith("Invalid number for --max-total-ms", 1);
                maxTotalMs = mtv;
                p += 2;
                break;

            case "--baseline":
                if (p + 1 >= n) return ExitWith("Missing value for --baseline", 1);
                baselinePath = args[p + 1];
                p += 2;
                break;

            case "--write-baseline":
                writeBaseline = true;
                p += 1;
                break;

            case "--baseline-allow-percent":
                if (p + 1 >= n) return ExitWith("Missing value for --baseline-allow-percent", 1);
                if (!double.TryParse(args[p + 1], out var pct)) return ExitWith("Invalid number for --baseline-allow-percent", 1);
                if (pct < 0) return ExitWith("baseline-allow-percent must be >= 0", 1);
                baselineAllowPercent = pct;
                p += 2;
                break;

            case "--budget":
            case "--pattern-budget":
                if (p + 1 >= n) return ExitWith("Missing value for --budget", 1);
                var spec = args[p + 1];
                if (!PatternBudget.TryParse(spec, out var budget, out var err))
                    return ExitWith($"Invalid --budget value '{spec}': {err}", 1);
                patternBudgets.Add(budget!);
                p += 2;
                break;

            case "--help":
            case "-h":
                Console.WriteLine(
                    "KeelMatrix.QueryWatch.Cli\n\n" +
                    "Options:\n" +
                    "  --input <file>               (repeatable) JSON summary exported by QueryWatch\n" +
                    "  --max-queries N              Fail if total queries exceed N\n" +
                    "  --max-average-ms MS          Fail if average duration (ms) exceeds MS\n" +
                    "  --max-total-ms MS            Fail if total duration (ms) exceeds MS\n" +
                    "  --baseline <file>            Compare against a baseline summary file\n" +
                    "  --baseline-allow-percent P   Allow +P% regression vs baseline before failing\n" +
                    "  --write-baseline             Write current aggregated summary to --baseline\n" +
                    "  --budget \"<pattern>=<max>\"   Per-pattern query count budget (repeatable).\n" +
                    "                                Pattern supports wildcards (*, ?) or prefix with 'regex:' for raw regex.\n"
                );
                return 0;

            default:
                // Unknown token — advance one and keep scanning
                p += 1;
                break;
        }
    }

    if (inputs.Count == 0) inputs.Add("artifacts/qwatch.report.json");

    // Load summaries
    var summaries = new List<Summary>();
    var notFound = new List<string>();
    foreach (var path in inputs) {
        if (!File.Exists(path)) { notFound.Add(path); continue; }
        try {
            await using var stream = File.OpenRead(path);
            var s = await JsonSerializer.DeserializeAsync<Summary>(stream).ConfigureAwait(false);
            if (s is null) return ExitWith($"Summary is null after parsing: {path}", 3);
            summaries.Add(s);
        }
        catch (Exception ex) {
            return ExitWith($"Failed to parse JSON '{path}': {ex.Message}", 3);
        }
    }

    if (summaries.Count == 0) {
        var msg = new StringBuilder("No input JSON found.");
        if (notFound.Count > 0) msg.Append(" Missing: ").Append(string.Join(", ", notFound));
        return ExitWith(msg.ToString(), 2);
    }

    // Aggregate
    var agg = Aggregated.From(summaries);

    // Basic console report
    Console.WriteLine($"QueryWatch Summary (schema {agg.Schema}, files {summaries.Count})");
    Console.WriteLine($" - Started: {agg.StartedAt:u}");
    Console.WriteLine($" - Stopped: {agg.StoppedAt:u}");
    Console.WriteLine($" - Queries: {agg.TotalQueries}");
    Console.WriteLine($" - Total:   {agg.TotalDurationMs:N2} ms");
    Console.WriteLine($" - Average: {agg.AverageDurationMs:N2} ms");
    if (agg.SampledEventsCount < agg.TotalQueries) {
        Console.WriteLine($" - Note: Events contain {agg.SampledEventsCount} sampled entries (top-N), total queries = {agg.TotalQueries}.");
    }

    var exitCode = 0;
    var summaryMd = new StringBuilder();
    summaryMd.AppendLine("# QueryWatch Gate");
    summaryMd.AppendLine();
    summaryMd.AppendLine($"**Files:** {summaries.Count} &nbsp;&nbsp; **Total queries:** {agg.TotalQueries}  &nbsp;&nbsp; **Avg:** {agg.AverageDurationMs:N2} ms  &nbsp;&nbsp; **Total:** {agg.TotalDurationMs:N2} ms");

    // Gate logic (simple thresholds)
    var violations = new List<string>();
    if (maxQueries.HasValue && agg.TotalQueries > maxQueries.Value)
        violations.Add($"MaxQueries={maxQueries.Value} but executed {agg.TotalQueries}.");
    if (maxAvgMs.HasValue && agg.AverageDurationMs > maxAvgMs.Value)
        violations.Add($"MaxAverageMs={maxAvgMs.Value} but actual {agg.AverageDurationMs:N2} ms.");
    if (maxTotalMs.HasValue && agg.TotalDurationMs > maxTotalMs.Value)
        violations.Add($"MaxTotalMs={maxTotalMs.Value} but actual {agg.TotalDurationMs:N2} ms.");

    if (violations.Count > 0) {
        await Console.Error.WriteLineAsync("Budget violations:");
        foreach (var v in violations)
            await Console.Error.WriteLineAsync(" - " + v);
        exitCode = Math.Max(exitCode, 4);
    }

    // Pattern budgets
    var patternFindings = new List<(PatternBudget budget, int count, bool over)>();
    if (patternBudgets.Count > 0) {
        var texts = agg.Events.Select(e => e.Text ?? string.Empty);
        foreach (var b in patternBudgets) {
            var count = texts.Count(t => b.Regex.IsMatch(t));
            var over = count > b.MaxCount;
            patternFindings.Add((b, count, over));
            if (over) {
                exitCode = Math.Max(exitCode, 4);
            }
        }
    }

    // Baseline comparison
    var baselineViolations = new List<string>();
    Summary? baseline = null;
    if (!string.IsNullOrWhiteSpace(baselinePath) && File.Exists(baselinePath)) {
        try {
            await using var bstream = File.OpenRead(baselinePath);
            baseline = await JsonSerializer.DeserializeAsync<Summary>(bstream).ConfigureAwait(false);
        }
        catch (Exception ex) {
            await Console.Error.WriteLineAsync($"Baseline parse failed (ignored): {ex.Message}");
        }
    }

    if (!string.IsNullOrWhiteSpace(baselinePath) && baseline is null && writeBaseline) {
        // If baseline doesn't exist (or failed to parse) and write requested → write it
        try {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            var dir = Path.GetDirectoryName(baselinePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            await using var outStream = File.Create(baselinePath!);
            await JsonSerializer.SerializeAsync(outStream, agg.ToSummary(), opts).ConfigureAwait(false);
            Console.WriteLine($"Baseline written: {baselinePath}");
        }
        catch (Exception ex) {
            await Console.Error.WriteLineAsync($"Failed to write baseline: {ex.Message}");
        }
    }
    else if (baseline is not null) {
        var tol = baselineAllowPercent / 100.0;
        double allowedQueries = baseline.TotalQueries * (1.0 + tol);
        double allowedAvg = baseline.AverageDurationMs * (1.0 + tol);
        double allowedTotal = baseline.TotalDurationMs * (1.0 + tol);

        if (agg.TotalQueries > allowedQueries) baselineViolations.Add($"Queries increased beyond +{baselineAllowPercent:N2}%: {baseline.TotalQueries} -> {agg.TotalQueries}");
        if (agg.AverageDurationMs > allowedAvg) baselineViolations.Add($"AverageMs increased beyond +{baselineAllowPercent:N2}%: {baseline.AverageDurationMs:N2} -> {agg.AverageDurationMs:N2}");
        if (agg.TotalDurationMs > allowedTotal) baselineViolations.Add($"TotalMs increased beyond +{baselineAllowPercent:N2}%: {baseline.TotalDurationMs:N2} -> {agg.TotalDurationMs:N2}");

        if (baselineViolations.Count > 0) {
            await Console.Error.WriteLineAsync("Baseline regressions:");
            foreach (var v in baselineViolations)
                await Console.Error.WriteLineAsync(" - " + v);
            exitCode = Math.Max(exitCode, 5);
        }
    }

    // Step summary (GitHub)
    var stepSummaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
    if (!string.IsNullOrWhiteSpace(stepSummaryPath)) {
        var md = BuildStepSummaryMarkdown(agg, maxQueries, maxAvgMs, maxTotalMs, violations, patternFindings, baseline, baselineAllowPercent, baselineViolations);
        try {
            await File.AppendAllTextAsync(stepSummaryPath!, md);
        }
        catch {
            // ignore
        }
    }

    // Write baseline if requested and we have a path
    if (!string.IsNullOrWhiteSpace(baselinePath) && writeBaseline && baseline is not null) {
        try {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            var dir = Path.GetDirectoryName(baselinePath);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            await using var outStream = File.Create(baselinePath!);
            await JsonSerializer.SerializeAsync(outStream, agg.ToSummary(), opts).ConfigureAwait(false);
            Console.WriteLine($"Baseline written: {baselinePath}");
        }
        catch (Exception ex) {
            await Console.Error.WriteLineAsync($"Failed to write baseline: {ex.Message}");
        }
    }

    // Final console outcome
    if (exitCode == 0) {
        Console.WriteLine("QueryWatch gate: OK");
    }

    return exitCode;
}

// TODO: REMOVE LATER. We build a compact markdown for GitHub's Step Summary to make PRs readable.
// The function is self-contained and safe to call outside GitHub (it simply returns a string).
static string BuildStepSummaryMarkdown(
    Aggregated agg,
    int? maxQueries,
    double? maxAvgMs,
    double? maxTotalMs,
    List<string> violations,
    List<(PatternBudget budget, int count, bool over)> patternFindings,
    Summary? baseline,
    double baselineAllowPercent,
    List<string> baselineViolations) {

    var sb = new StringBuilder();
    sb.AppendLine("# QueryWatch Gate");
    sb.AppendLine();
    sb.AppendLine($"Files: **{agg.FileCount}** &nbsp; | &nbsp; Queries: **{agg.TotalQueries}** &nbsp; | &nbsp; Avg: **{agg.AverageDurationMs:N2} ms** &nbsp; | &nbsp; Total: **{agg.TotalDurationMs:N2} ms**");
    sb.AppendLine();

    if (agg.SampledEventsCount < agg.TotalQueries) {
        sb.AppendLine($"> ℹ️ Events are sampled (top-N). Counted {agg.SampledEventsCount} events out of {agg.TotalQueries} queries. Consider exporting with a higher `sampleTop` if you rely on per-pattern budgets.");
        sb.AppendLine();
    }

    // Hard budgets
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

    // Pattern budgets
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

    // Baseline
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

// Aggregation helpers
sealed class Aggregated {
    public string Schema { get; private set; } = "1.0.0";
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset StoppedAt { get; private set; }
    public int TotalQueries { get; private set; }
    public double TotalDurationMs { get; private set; }
    public double AverageDurationMs { get; private set; }
    public int SampledEventsCount => Events.Count;
    public int FileCount { get; private set; }
    public List<EventSample> Events { get; } = new();

    public static Aggregated From(IEnumerable<Summary> summaries) {
        var agg = new Aggregated();
        agg.FileCount = 0;
        var haveTimes = false;
        var totalDurMs = 0.0;
        var totalQueries = 0;

        foreach (var s in summaries) {
            agg.FileCount++;
            agg.Schema = s.Schema;
            if (!haveTimes) { agg.StartedAt = s.StartedAt; agg.StoppedAt = s.StoppedAt; haveTimes = true; }
            else {
                if (s.StartedAt < agg.StartedAt) agg.StartedAt = s.StartedAt;
                if (s.StoppedAt > agg.StoppedAt) agg.StoppedAt = s.StoppedAt;
            }

            totalQueries += s.TotalQueries;
            totalDurMs += s.TotalDurationMs;
            if (s.Events is not null && s.Events.Count > 0) {
                agg.Events.AddRange(s.Events);
            }
        }

        agg.TotalQueries = totalQueries;
        agg.TotalDurationMs = totalDurMs;
        agg.AverageDurationMs = totalQueries == 0 ? 0.0 : (totalDurMs / totalQueries);
        // Keep the combined events in descending duration order (most helpful first)
        agg.Events.Sort((a, b) => b.DurationMs.CompareTo(a.DurationMs));
        return agg;
    }

    public Summary ToSummary() {
        return new Summary {
            Schema = Schema,
            StartedAt = StartedAt,
            StoppedAt = StoppedAt,
            TotalQueries = TotalQueries,
            TotalDurationMs = TotalDurationMs,
            AverageDurationMs = AverageDurationMs,
            Events = Events.ToList(),
            Meta = new Dictionary<string, string> { { "aggregatedFromFiles", FileCount.ToString() } }
        };
    }
}

// Pattern budget: "<pattern>=<maxCount>"
sealed class PatternBudget {
    public required Regex Regex { get; init; }
    public required int MaxCount { get; init; }
    public required string RawPattern { get; init; }

    public static bool TryParse(string spec, out PatternBudget? budget, out string? error) {
        budget = null; error = null;
        if (string.IsNullOrWhiteSpace(spec)) { error = "Empty spec"; return false; }
        var idx = spec.LastIndexOf('=');
        if (idx <= 0 || idx == spec.Length - 1) { error = "Expected '<pattern>=<max>'"; return false; }
        var pRaw = spec.Substring(0, idx).Trim();
        var maxRaw = spec[(idx + 1)..].Trim();
        if (!int.TryParse(maxRaw, out var max) || max < 0) { error = "Invalid <max> (must be non-negative integer)"; return false; }

        Regex rx;
        if (pRaw.StartsWith("regex:", StringComparison.OrdinalIgnoreCase)) {
            var body = pRaw.Substring("regex:".Length);
            try { rx = new Regex(body, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled); }
            catch (Exception ex) { error = "Invalid regex: " + ex.Message; return false; }
        }
        else {
            // Treat as wildcard: escape then replace * -> .*, ? -> .
            var escaped = Regex.Escape(pRaw).Replace(@"\*", ".*").Replace(@"\?", ".");
            rx = new Regex("^" + escaped + "$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        budget = new PatternBudget { Regex = rx, MaxCount = max, RawPattern = pRaw };
        return true;
    }
}

// Keep these types inside a named namespace to satisfy analyzers (S3903) and add XML docs (CS1591).
namespace KeelMatrix.QueryWatch.Cli {
    /// <summary>
    /// Compact representation of a QueryWatch report used by the CLI,
    /// matching the JSON written by <c>KeelMatrix.QueryWatch.Reporting.QueryWatchJson</c>.
    /// </summary>
    public sealed class Summary {
        /// <summary>JSON schema version string.</summary>
        [JsonPropertyName("schema")]
        public string Schema { get; set; } = "1.0.0";

        /// <summary>UTC timestamp when the monitored session started.</summary>
        [JsonPropertyName("startedAt")]
        public DateTimeOffset StartedAt { get; set; }

        /// <summary>UTC timestamp when the monitored session stopped.</summary>
        [JsonPropertyName("stoppedAt")]
        public DateTimeOffset StoppedAt { get; set; }

        /// <summary>Total number of queries recorded during the session.</summary>
        [JsonPropertyName("totalQueries")]
        public int TotalQueries { get; set; }

        /// <summary>Total duration of all queries in milliseconds.</summary>
        [JsonPropertyName("totalDurationMs")]
        public double TotalDurationMs { get; set; }

        /// <summary>Average query duration in milliseconds.</summary>
        [JsonPropertyName("averageDurationMs")]
        public double AverageDurationMs { get; set; }

        /// <summary>Top-N per-query samples (by duration) included for diagnostics.</summary>
        [JsonPropertyName("events")]
        public List<EventSample> Events { get; set; } = new();

        /// <summary>Optional product metadata emitted by the reporter.</summary>
        [JsonPropertyName("meta")]
        public Dictionary<string, string> Meta { get; set; } = new();
    }

    /// <summary>
    /// Per-event sample included in the summary JSON.
    /// </summary>
    public sealed class EventSample {
        /// <summary>UTC timestamp when the query completed.</summary>
        [JsonPropertyName("at")]
        public DateTimeOffset At { get; set; }

        /// <summary>Query duration in milliseconds.</summary>
        [JsonPropertyName("durationMs")]
        public double DurationMs { get; set; }

        /// <summary>Redacted SQL or provider-specific textual representation.</summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
