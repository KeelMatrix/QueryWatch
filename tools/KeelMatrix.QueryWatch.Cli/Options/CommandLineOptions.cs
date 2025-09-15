#nullable enable
using System.Text;

namespace KeelMatrix.QueryWatch.Cli.Options {
    internal sealed class CommandLineOptions {
        public List<string> Inputs { get; } = new();
        public int? MaxQueries { get; init; }
        public double? MaxAverageMs { get; init; }
        public double? MaxTotalMs { get; init; }
        public string? BaselinePath { get; init; }
        public bool WriteBaseline { get; init; }
        public double BaselineAllowPercent { get; init; }
        public List<string> PatternBudgetSpecs { get; } = new();
        public bool ShowHelp { get; init; }

        public static ParseResult Parse(string[] args) {
            var inputs = new List<string>();
            int? maxQueries = null;
            double? maxAvg = null;
            double? maxTotal = null;
            string? baseline = null;
            bool writeBaseline = false;
            double baselineAllowPercent = 0.0;
            var budgets = new List<string>();
            bool showHelp = false;

            int p = 0;
            int n = args.Length;
            while (p < n) {
                var arg = args[p];
                switch (arg) {
                    case "--input":
                        if (p + 1 >= n) return ParseResult.Error("Missing value for --input");
                        inputs.Add(args[p + 1]);
                        p += 2;
                        break;

                    case "--max-queries":
                        if (p + 1 >= n) return ParseResult.Error("Missing value for --max-queries");
                        if (!int.TryParse(args[p + 1], out var mq)) return ParseResult.Error("Invalid integer for --max-queries");
                        maxQueries = mq; p += 2; break;

                    case "--max-average-ms":
                        if (p + 1 >= n) return ParseResult.Error("Missing value for --max-average-ms");
                        if (!double.TryParse(args[p + 1], out var ma)) return ParseResult.Error("Invalid number for --max-average-ms");
                        maxAvg = ma; p += 2; break;

                    case "--max-total-ms":
                        if (p + 1 >= n) return ParseResult.Error("Missing value for --max-total-ms");
                        if (!double.TryParse(args[p + 1], out var mt)) return ParseResult.Error("Invalid number for --max-total-ms");
                        maxTotal = mt; p += 2; break;

                    case "--baseline":
                        if (p + 1 >= n) return ParseResult.Error("Missing value for --baseline");
                        baseline = args[p + 1]; p += 2; break;

                    case "--write-baseline":
                        writeBaseline = true; p += 1; break;

                    case "--baseline-allow-percent":
                        if (p + 1 >= n) return ParseResult.Error("Missing value for --baseline-allow-percent");
                        if (!double.TryParse(args[p + 1], out var pct)) return ParseResult.Error("Invalid number for --baseline-allow-percent");
                        if (pct < 0) return ParseResult.Error("baseline-allow-percent must be >= 0");
                        baselineAllowPercent = pct; p += 2; break;

                    case "--budget":
                    case "--pattern-budget":
                        if (p + 1 >= n) return ParseResult.Error("Missing value for --budget");
                        budgets.Add(args[p + 1]); p += 2; break;

                    case "--help":
                    case "-h":
                        showHelp = true; p += 1; break;

                    default:
                        // Ignore unknown tokens to be forgiving; users often pass extra args from wrappers.
                        p += 1;
                        break;
                }
            }

            if (inputs.Count == 0) inputs.Add("artifacts/qwatch.report.json");

            var opts = new CommandLineOptions {
                MaxQueries = maxQueries,
                MaxAverageMs = maxAvg,
                MaxTotalMs = maxTotal,
                BaselinePath = baseline,
                WriteBaseline = writeBaseline,
                BaselineAllowPercent = baselineAllowPercent,
                ShowHelp = showHelp
            };
            opts.Inputs.AddRange(inputs);
            opts.PatternBudgetSpecs.AddRange(budgets);
            return ParseResult.Successful(opts);
        }

        public static string HelpText =>
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
            "                                Pattern supports wildcards (*, ?) or prefix with 'regex:' for raw regex.\n";
    }

    internal readonly record struct ParseResult(bool Success, CommandLineOptions Options, string? ErrorMessage, bool ShowHelp) {
        public static ParseResult Successful(CommandLineOptions opts) => new(true, opts, null, false);
        public static ParseResult Error(string error) => new(false, new CommandLineOptions(), error, true);
    }
}
