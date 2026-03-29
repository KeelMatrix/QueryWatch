// Copyright (c) KeelMatrix

using System.Text;

namespace KeelMatrix.QueryWatch.Cli.Options {
    internal sealed record CliOption(string Flag, string? ValueSyntax, string Description, string? Notes = null, bool Repeatable = false);
    internal sealed record CliCommand(string Command, string Description);

    internal static class CliSpec {
        public static readonly CliOption[] Options = [
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
        ];

        public static readonly CliCommand[] TelemetryCommands = [
            new CliCommand("telemetry status [--json]", "Show effective telemetry state for the current repo."),
            new CliCommand("telemetry disable", "Write repo-local keelmatrix.telemetry.json with disabled=true."),
            new CliCommand("telemetry enable", "Remove or neutralize qwatch-managed repo-local telemetry opt-out.")
        ];

        public static string BuildHelpText() {
            // column widths
            const int leftWidth = 30;
            StringBuilder sb = new();
            _ = sb.AppendLine("QueryWatch CLI");
            _ = sb.AppendLine();
            _ = sb.AppendLine("Usage:");
            _ = sb.AppendLine("  qwatch --input file.json [options]");
            _ = sb.AppendLine("  qwatch telemetry <status|disable|enable> [options]");
            _ = sb.AppendLine();
            _ = sb.AppendLine("Commands:");
            foreach (CliCommand command in TelemetryCommands) {
                _ = AppendAlignedLine(sb, leftWidth, command.Command, command.Description);
            }
            _ = sb.AppendLine();
            _ = sb.AppendLine("Options:");
            foreach (CliOption o in Options) {
                var left = o.Flag + (o.ValueSyntax is not null ? " " + o.ValueSyntax : string.Empty);
                var repeat = o.Repeatable ? " (repeatable)" : string.Empty;
                _ = AppendAlignedLine(sb, leftWidth, left, o.Description + repeat);
                if (!string.IsNullOrWhiteSpace(o.Notes)) {
                    _ = sb.Append(' ', 2 + leftWidth);
                    _ = sb.AppendLine(o.Notes);
                }
            }
            return sb.ToString();
        }

        public static string BuildTelemetryHelpText() {
            const int leftWidth = 28;
            StringBuilder sb = new();
            _ = sb.AppendLine("QueryWatch CLI telemetry");
            _ = sb.AppendLine();
            _ = sb.AppendLine("Usage:");
            _ = sb.AppendLine("  qwatch telemetry status [--json]");
            _ = sb.AppendLine("  qwatch telemetry disable");
            _ = sb.AppendLine("  qwatch telemetry enable");
            _ = sb.AppendLine();
            _ = sb.AppendLine("Commands:");
            _ = AppendAlignedLine(sb, leftWidth, "status [--json]", "Show effective telemetry state for the current repo.");
            _ = AppendAlignedLine(sb, leftWidth, "disable", "Write repo-local keelmatrix.telemetry.json with disabled=true.");
            _ = AppendAlignedLine(sb, leftWidth, "enable", "Remove or neutralize qwatch-managed repo-local telemetry opt-out.");
            _ = sb.AppendLine();
            _ = sb.AppendLine("Options:");
            _ = AppendAlignedLine(sb, leftWidth, "--json", "Output telemetry status as JSON.");
            _ = AppendAlignedLine(sb, leftWidth, "--help", "Show this help.");
            return sb.ToString();
        }

        public static string BuildReadmeFlagsMarkdown() {
            StringBuilder sb = new();
            _ = sb.AppendLine("```");
            _ = sb.AppendLine("Usage:");
            _ = sb.AppendLine("  qwatch --input file.json [options]");
            _ = sb.AppendLine("  qwatch telemetry <status|disable|enable> [options]");
            _ = sb.AppendLine();
            _ = sb.AppendLine("Commands:");
            foreach (CliCommand command in TelemetryCommands) {
                var left = command.Command.PadRight(28);
                _ = sb.AppendLine($"{left} {command.Description}");
            }
            _ = sb.AppendLine();
            _ = sb.AppendLine("Options:");
            foreach (CliOption o in Options) {
                var left = (o.Flag + (o.ValueSyntax is not null ? " " + o.ValueSyntax : string.Empty)).PadRight(28);
                var repeat = o.Repeatable ? " (repeatable)" : string.Empty;
                _ = sb.AppendLine($"{left} {o.Description}{repeat}");
                if (!string.IsNullOrWhiteSpace(o.Notes)) {
                    _ = sb.AppendLine(new string(' ', 29) + o.Notes);
                }
            }
            _ = sb.AppendLine("```");
            return sb.ToString();
        }

        private static StringBuilder AppendAlignedLine(StringBuilder sb, int leftWidth, string left, string right) {
            _ = sb.Append("  ");
            if (left.Length >= leftWidth) {
                _ = sb.AppendLine(left);
                _ = sb.Append(' ', leftWidth);
            }
            else {
                _ = sb.Append(left.PadRight(leftWidth));
            }

            _ = sb.AppendLine(right);
            return sb;
        }
    }
}
