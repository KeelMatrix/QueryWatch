// Copyright (c) KeelMatrix
#nullable enable
using System.Text.Json;
using System.Text.Json.Serialization;
using KeelMatrix.QueryWatch.Cli;

// Minimal CLI that reads a QueryWatch JSON summary and enforces simple budgets.
// Usage:
//   dotnet run --project tools/KeelMatrix.QueryWatch.Cli -- --input artifacts/qwatch.report.json --max-queries 25 --max-average-ms 50 --max-total-ms 2000 --baseline artifacts/qwatch.baseline.json --write-baseline
//
// Exit codes:
//   0  OK
//   1  Invalid arguments
//   2  Input file not found
//   3  JSON parse error
//   4  Budget thresholds exceeded
//   5  Baseline regression detected

return await RunAsync(args);

static async Task<int> RunAsync(string[] args) {
    static int ExitWith(string message, int code) {
        Console.Error.WriteLine(message);
        return code;
    }

    string? input = null;
    int? maxQueries = null;
    double? maxAvgMs = null;
    double? maxTotalMs = null;
    string? baseline = null;
    bool writeBaseline = false;

    int p = 0;
    int n = args.Length;

    while (p < n) {
        var arg = args[p];

        switch (arg) {
            case "--input":
                if (p + 1 >= n) return ExitWith("Missing value for --input", 1);
                input = args[p + 1];
                p += 2;
                break;

            case "--max-queries":
                if (p + 1 >= n) return ExitWith("Missing value for --max-queries", 1);
                maxQueries = int.Parse(args[p + 1]);
                p += 2;
                break;

            case "--max-average-ms":
                if (p + 1 >= n) return ExitWith("Missing value for --max-average-ms", 1);
                maxAvgMs = double.Parse(args[p + 1]);
                p += 2;
                break;

            case "--max-total-ms":
                if (p + 1 >= n) return ExitWith("Missing value for --max-total-ms", 1);
                maxTotalMs = double.Parse(args[p + 1]);
                p += 2;
                break;

            case "--baseline":
                if (p + 1 >= n) return ExitWith("Missing value for --baseline", 1);
                baseline = args[p + 1];
                p += 2;
                break;

            case "--write-baseline":
                writeBaseline = true;
                p += 1;
                break;

            case "--help":
            case "-h":
                Console.WriteLine(
                    "KeelMatrix.QueryWatch.Cli\n\n" +
                    "--input <file> [--max-queries N] [--max-average-ms MS] [--max-total-ms MS] " +
                    "[--baseline <file>] [--write-baseline]\n"
                );
                return 0;

            default:
                // Unknown token — advance one and keep scanning
                p += 1;
                break;
        }
    }

    input ??= "artifacts/qwatch.report.json";
    if (!File.Exists(input)) {
        return ExitWith($"Input not found: {input}", 2);
    }

    Summary? s;
    try {
        await using var stream = File.OpenRead(input);
        s = await JsonSerializer.DeserializeAsync<Summary>(stream).ConfigureAwait(false);
    }
    catch (Exception ex) {
        return ExitWith($"Failed to parse JSON: {ex.Message}", 3);
    }

    if (s is null) {
        return ExitWith("Summary is null after parsing.", 3);
    }

    // Basic report
    Console.WriteLine($"QueryWatch Summary (schema {s.Schema})");
    Console.WriteLine($" - Started: {s.StartedAt:u}");
    Console.WriteLine($" - Stopped: {s.StoppedAt:u}");
    Console.WriteLine($" - Queries: {s.TotalQueries}");
    Console.WriteLine($" - Total:   {s.TotalDurationMs:N2} ms");
    Console.WriteLine($" - Average: {s.AverageDurationMs:N2} ms");

    // Gate logic (simple MVP)
    var violations = new List<string>();
    if (maxQueries.HasValue && s.TotalQueries > maxQueries.Value)
        violations.Add($"MaxQueries={maxQueries.Value} but executed {s.TotalQueries}");
    if (maxAvgMs.HasValue && s.AverageDurationMs > maxAvgMs.Value)
        violations.Add($"MaxAverageMs={maxAvgMs.Value} but actual {s.AverageDurationMs:N2}");
    if (maxTotalMs.HasValue && s.TotalDurationMs > maxTotalMs.Value)
        violations.Add($"MaxTotalMs={maxTotalMs.Value} but actual {s.TotalDurationMs:N2}");

    if (violations.Count > 0) {
        await Console.Error.WriteLineAsync("Budget violations:");
        foreach (var v in violations)
            await Console.Error.WriteLineAsync(" - " + v);
        return 4;
    }

    // Baseline comparison (strict: any increase fails)
    if (!string.IsNullOrWhiteSpace(baseline) && File.Exists(baseline)) {
        try {
            await using var bstream = File.OpenRead(baseline);
            var b = await JsonSerializer.DeserializeAsync<Summary>(bstream).ConfigureAwait(false);
            if (b is not null) {
                var baseViolations = new List<string>();
                if (s.TotalQueries > b.TotalQueries) baseViolations.Add($"Queries increased: {b.TotalQueries} -> {s.TotalQueries}");
                if (s.AverageDurationMs > b.AverageDurationMs) baseViolations.Add($"AverageMs increased: {b.AverageDurationMs:N2} -> {s.AverageDurationMs:N2}");
                if (s.TotalDurationMs > b.TotalDurationMs) baseViolations.Add($"TotalMs increased: {b.TotalDurationMs:N2} -> {s.TotalDurationMs:N2}");

                if (baseViolations.Count > 0) {
                    await Console.Error.WriteLineAsync("Baseline regressions:");
                    foreach (var v in baseViolations)
                        await Console.Error.WriteLineAsync(" - " + v);
                    return 5;
                }
            }
        }
        catch (Exception ex) {
            await Console.Error.WriteLineAsync($"Baseline parse failed (ignored): {ex.Message}");
        }
    }

    if (!string.IsNullOrWhiteSpace(baseline) && writeBaseline) {
        try {
            var opts = new JsonSerializerOptions { WriteIndented = true };
            var dir = Path.GetDirectoryName(baseline);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            await using var outStream = File.Create(baseline!);
            await JsonSerializer.SerializeAsync(outStream, s, opts).ConfigureAwait(false);
            Console.WriteLine($"Baseline written: {baseline}");
        }
        catch (Exception ex) {
            await Console.Error.WriteLineAsync($"Failed to write baseline: {ex.Message}");
        }
    }

    return 0;
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
