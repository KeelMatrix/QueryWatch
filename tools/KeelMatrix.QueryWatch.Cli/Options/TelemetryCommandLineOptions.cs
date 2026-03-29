// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Cli.Options {
    internal enum TelemetryCommandKind {
        Status,
        Disable,
        Enable
    }

    internal sealed class TelemetryCommandLineOptions {
        public TelemetryCommandKind Command { get; init; }
        public bool Json { get; init; }
        public bool ShowHelp { get; init; }

        public static TelemetryParseResult Parse(string[] args) {
            if (HasHelp(args))
                return TelemetryParseResult.Successful(new TelemetryCommandLineOptions { ShowHelp = true });

            if (args.Length == 0)
                return TelemetryParseResult.Error("Missing telemetry command. Use 'qwatch telemetry --help' to see available commands.");

            if (string.Equals(args[0], "status", StringComparison.OrdinalIgnoreCase))
                return ParseStatus(args[1..]);

            if (string.Equals(args[0], "disable", StringComparison.OrdinalIgnoreCase))
                return ParseNoOptionCommand(TelemetryCommandKind.Disable, "disable", args[1..]);

            if (string.Equals(args[0], "enable", StringComparison.OrdinalIgnoreCase))
                return ParseNoOptionCommand(TelemetryCommandKind.Enable, "enable", args[1..]);

            return TelemetryParseResult.Error($"Unknown telemetry command: {args[0]}");
        }

        public static string HelpText => CliSpec.BuildTelemetryHelpText();

        private static TelemetryParseResult ParseStatus(string[] args) {
            bool json = false;

            foreach (string arg in args) {
                if (string.Equals(arg, "--json", StringComparison.OrdinalIgnoreCase)) {
                    json = true;
                    continue;
                }

                return TelemetryParseResult.Error($"Unknown telemetry status argument: {arg}");
            }

            return TelemetryParseResult.Successful(new TelemetryCommandLineOptions {
                Command = TelemetryCommandKind.Status,
                Json = json
            });
        }

        private static TelemetryParseResult ParseNoOptionCommand(TelemetryCommandKind kind, string verb, string[] args) {
            if (args.Length > 0)
                return TelemetryParseResult.Error($"Unknown telemetry {verb} argument: {args[0]}");

            return TelemetryParseResult.Successful(new TelemetryCommandLineOptions { Command = kind });
        }

        private static bool HasHelp(string[] args) {
            return args.Any(static arg =>
                string.Equals(arg, "--help", StringComparison.OrdinalIgnoreCase)
                || string.Equals(arg, "-h", StringComparison.OrdinalIgnoreCase)
                || string.Equals(arg, "/?", StringComparison.OrdinalIgnoreCase));
        }
    }

    internal readonly record struct TelemetryParseResult(bool Success, TelemetryCommandLineOptions Options, string? ErrorMessage, bool ShowHelp) {
        public static TelemetryParseResult Successful(TelemetryCommandLineOptions opts) => new(true, opts, null, opts.ShowHelp);
        public static TelemetryParseResult Error(string error) => new(false, new TelemetryCommandLineOptions(), error, true);
    }
}
