// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Cli.Options {
    internal static class CliCommandParser {
        public static RootParseResult Parse(string[] args) {
            if (args.Length > 0 && string.Equals(args[0], "telemetry", StringComparison.OrdinalIgnoreCase)) {
                return RootParseResult.FromTelemetry(TelemetryCommandLineOptions.Parse(args[1..]));
            }

            return RootParseResult.FromAnalysis(CommandLineOptions.Parse(args));
        }
    }

    internal readonly record struct RootParseResult(
        bool Success,
        CommandLineOptions? Options,
        TelemetryCommandLineOptions? TelemetryOptions,
        string? ErrorMessage,
        bool ShowHelp,
        string HelpText) {

        public static RootParseResult FromAnalysis(ParseResult parsed) {
            return new RootParseResult(
                parsed.Success,
                parsed.Success ? parsed.Options : null,
                null,
                parsed.ErrorMessage,
                parsed.ShowHelp,
                CommandLineOptions.HelpText);
        }

        public static RootParseResult FromTelemetry(TelemetryParseResult parsed) {
            return new RootParseResult(
                parsed.Success,
                null,
                parsed.Success ? parsed.Options : null,
                parsed.ErrorMessage,
                parsed.ShowHelp,
                TelemetryCommandLineOptions.HelpText);
        }
    }
}
