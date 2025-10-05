// Copyright (c) KeelMatrix
#nullable enable
using System.Text;

namespace KeelMatrix.QueryWatch.Cli.Options {
    internal sealed record CliOption(string Flag, string? ValueSyntax, string Description, string? Notes = null, bool Repeatable = false);

    internal static class CliSpec {
        public static readonly CliOption[] Options = new[] {
            new CliOption("--input", "<path>", "Input JSON summary file.", Repeatable: true),
            new CliOption("--max-queries", "N", "Fail if total query count exceeds N."),
            new CliOption("--max-average-ms", "N", "Fail if average duration exceeds N ms."),
            new CliOption("--max-total-ms", "N", "Fail if total duration exceeds N ms."),
            new CliOption("--baseline", "<path>", "Baseline summary JSON to compare against."),
            new CliOption("--baseline-allow-percent", "P", "Allow +P% regression vs baseline before failing."),
            new CliOption("--write-baseline", null, "Write current aggregated summary to --baseline."),
            new CliOption("--budget", "\"<pattern>=<max>\"", "Per-pattern query count budget (repeatable).",
                Notes: "Pattern supports wildcards (*, ?) or prefix with 'regex:' for raw regex.", Repeatable: true),
            new CliOption("--require-full-events", null, "Fail if input summaries are top-N sampled."),
            new CliOption("--help", null, "Show this help.")
        };

        public static string BuildHelpText() {
            // column widths
            const int leftWidth = 30;
            var sb = new StringBuilder();
            sb.AppendLine("QueryWatch CLI");
            sb.AppendLine();
            sb.AppendLine("Usage: qwatch --input file.json [options]");
            sb.AppendLine();
            sb.AppendLine("Options:");
            foreach (var o in Options) {
                var left = o.Flag + (o.ValueSyntax is not null ? " " + o.ValueSyntax : string.Empty);
                var repeat = o.Repeatable ? " (repeatable)" : string.Empty;
                sb.Append("  ");
                if (left.Length >= leftWidth) {
                    sb.AppendLine(left);
                    sb.Append(' ', leftWidth);
                }
                else {
                    sb.Append(left.PadRight(leftWidth));
                }
                sb.AppendLine(o.Description + repeat);
                if (!string.IsNullOrWhiteSpace(o.Notes)) {
                    sb.Append(' ', 2 + leftWidth);
                    sb.AppendLine(o.Notes);
                }
            }
            return sb.ToString();
        }

        public static string BuildReadmeFlagsMarkdown() {
            var sb = new StringBuilder();
            sb.AppendLine("```");
            foreach (var o in Options) {
                var left = (o.Flag + (o.ValueSyntax is not null ? " " + o.ValueSyntax : string.Empty)).PadRight(28);
                var repeat = o.Repeatable ? " (repeatable)" : string.Empty;
                sb.AppendLine($"{left} {o.Description}{repeat}");
                if (!string.IsNullOrWhiteSpace(o.Notes)) {
                    sb.AppendLine(new string(' ', 29) + o.Notes);
                }
            }
            sb.AppendLine("```");
            return sb.ToString();
        }
    }
}
