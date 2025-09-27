#nullable enable
using System.Globalization;
using System.Text;

namespace KeelMatrix.QueryWatch.Cli.Options {
    internal sealed class CommandLineOptions {
        public List<string> Inputs { get; } = new();
        public int? MaxQueries { get; set; }
        public double? MaxAverageMs { get; set; }
        public double? MaxTotalMs { get; set; }
        public string? BaselinePath { get; set; }
        public bool WriteBaseline { get; set; }
        public double BaselineAllowPercent { get; set; }
        public List<string> PatternBudgetSpecs { get; } = new();
        public bool RequireFullEvents { get; set; }
        public bool ShowHelp { get; set; }

        public static ParseResult Parse(string[] args) {
            var opts = new CommandLineOptions { BaselineAllowPercent = 0 };
            // invariant culture for numeric parsing
            var culture = CultureInfo.InvariantCulture;

            int i = 0;
            while (i < args.Length) {
                var a = args[i];
                if (string.Equals(a, "--help", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a, "-h", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(a, "/?", StringComparison.OrdinalIgnoreCase)) {
                    return ParseResult.Successful(new CommandLineOptions { ShowHelp = true });
                }
                else if (string.Equals(a, "--input", StringComparison.OrdinalIgnoreCase)) {
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal)) {
                        return ParseResult.Error("Missing value for --input");
                    }
                    opts.Inputs.Add(args[i + 1]);
                    i += 2; continue;
                }
                else if (string.Equals(a, "--max-queries", StringComparison.OrdinalIgnoreCase)) {
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal)) {
                        return ParseResult.Error("Missing value for --max-queries");
                    }
                    if (!int.TryParse(args[i + 1], NumberStyles.Integer, culture, out var mq) || mq < 0) {
                        return ParseResult.Error($"Invalid value for --max-queries: '{args[i + 1]}'");
                    }
                    opts.MaxQueries = mq;
                    i += 2; continue;
                }
                else if (string.Equals(a, "--max-average-ms", StringComparison.OrdinalIgnoreCase)) {
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal)) {
                        return ParseResult.Error("Missing value for --max-average-ms");
                    }
                    if (!double.TryParse(args[i + 1], NumberStyles.Float, culture, out var mav) || mav < 0) {
                        return ParseResult.Error($"Invalid value for --max-average-ms: '{args[i + 1]}'");
                    }
                    opts.MaxAverageMs = mav;
                    i += 2; continue;
                }
                else if (string.Equals(a, "--max-total-ms", StringComparison.OrdinalIgnoreCase)) {
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal)) {
                        return ParseResult.Error("Missing value for --max-total-ms");
                    }
                    if (!double.TryParse(args[i + 1], NumberStyles.Float, culture, out var mt) || mt < 0) {
                        return ParseResult.Error($"Invalid value for --max-total-ms: '{args[i + 1]}'");
                    }
                    opts.MaxTotalMs = mt;
                    i += 2; continue;
                }
                else if (string.Equals(a, "--baseline", StringComparison.OrdinalIgnoreCase)) {
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal)) {
                        return ParseResult.Error("Missing value for --baseline");
                    }
                    opts.BaselinePath = args[i + 1];
                    i += 2; continue;
                }
                else if (string.Equals(a, "--baseline-allow-percent", StringComparison.OrdinalIgnoreCase)) {
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal)) {
                        return ParseResult.Error("Missing value for --baseline-allow-percent");
                    }
                    if (!double.TryParse(args[i + 1], NumberStyles.Float, culture, out var p) || p < 0) {
                        return ParseResult.Error($"Invalid value for --baseline-allow-percent: '{args[i + 1]}'");
                    }
                    opts.BaselineAllowPercent = p;
                    i += 2; continue;
                }
                else if (string.Equals(a, "--write-baseline", StringComparison.OrdinalIgnoreCase)) {
                    opts.WriteBaseline = true;
                    i += 1; continue;
                }
                else if (string.Equals(a, "--budget", StringComparison.OrdinalIgnoreCase)) {
                    if (i + 1 >= args.Length || args[i + 1].StartsWith("--", StringComparison.Ordinal)) {
                        return ParseResult.Error("Missing value for --budget");
                    }
                    opts.PatternBudgetSpecs.Add(args[i + 1]);
                    i += 2; continue;
                }
                else if (string.Equals(a, "--require-full-events", StringComparison.OrdinalIgnoreCase)) {
                    opts.RequireFullEvents = true;
                    i += 1; continue;
                }
                else {
                    return ParseResult.Error($"Unknown argument: {a}");
                }
            }

            // Post-parse validations
            if (opts.WriteBaseline && string.IsNullOrWhiteSpace(opts.BaselinePath)) {
                return ParseResult.Error("Cannot use --write-baseline without --baseline");
            }

            return ParseResult.Successful(opts);
        }

        public static readonly string HelpText =
            "QueryWatch CLI\n" +
            "\n" +
            "Usage: qwatch --input file.json [options]\n" +
            "\n" +
            "Options:\n" +
            "  --input <path>                Input JSON summary file (repeatable).\n" +
            "  --max-queries N               Fail if total query count exceeds N.\n" +
            "  --max-average-ms N            Fail if average duration exceeds N ms.\n" +
            "  --max-total-ms N              Fail if total duration exceeds N ms.\n" +
            "  --baseline <path>             Baseline summary JSON to compare against.\n" +
            "  --baseline-allow-percent P    Allow +P% regression vs baseline before failing.\n" +
            "  --write-baseline              Write current aggregated summary to --baseline.\n" +
            "  --budget \"<pattern>=<max>\"    Per-pattern query count budget (repeatable).\n" +
            "                                 Pattern supports wildcards (*, ?) or prefix with 'regex:' for raw regex.\n" +
            "  --require-full-events         Fail if input summaries are top-N sampled.\n" +
            "  --help                        Show this help.\n";
    }

    internal readonly record struct ParseResult(bool Success, CommandLineOptions Options, string? ErrorMessage, bool ShowHelp) {
        public static ParseResult Successful(CommandLineOptions opts) => new(true, opts, null, opts.ShowHelp);
        public static ParseResult Error(string error) => new(false, new CommandLineOptions(), error, true);
    }
}
