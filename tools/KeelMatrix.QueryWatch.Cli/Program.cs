// Copyright (c) KeelMatrix

using KeelMatrix.QueryWatch.Cli.Core;
using KeelMatrix.QueryWatch.Cli.Options;
using KeelMatrix.QueryWatch.Cli.Telemetry;

namespace KeelMatrix.QueryWatch.Cli {
    internal static class Program {
        private static Task<int> Main(string[] args) {
            return RunAsync(args);
        }

        internal static async Task<int> RunAsync(string[] args) {
            // 0) No arguments → show help
            if (args.Length == 0) {
                Console.WriteLine(CommandLineOptions.HelpText);
                return ExitCodes.Ok;
            }

            // 1) Hidden doc-switch to keep README and --help in sync from the same source.
            if (args.Length == 1 && string.Equals(args[0], "--print-flags-md", StringComparison.OrdinalIgnoreCase)) {
                Console.WriteLine(CliSpec.BuildReadmeFlagsMarkdown());
                return ExitCodes.Ok;
            }

            RootParseResult parsed = CliCommandParser.Parse(args);

            // 2) If the caller asked for help, always print it and return:
            //    - 0 when parse succeeded (pure help)
            //    - 1 when parse failed (help + error)
            if (parsed.ShowHelp) {
                Console.WriteLine(parsed.HelpText);
                if (!parsed.Success && !string.IsNullOrEmpty(parsed.ErrorMessage))
                    await Console.Error.WriteLineAsync(parsed.ErrorMessage);
                return parsed.Success ? ExitCodes.Ok : ExitCodes.InvalidArguments;
            }

            // 3) If parsing failed without an explicit help request, emit the error and exit 1
            if (!parsed.Success) {
                if (!string.IsNullOrEmpty(parsed.ErrorMessage))
                    await Console.Error.WriteLineAsync(parsed.ErrorMessage);
                return ExitCodes.InvalidArguments;
            }

            if (parsed.TelemetryOptions is not null)
                return await TelemetryCommandHandler.ExecuteAsync(parsed.TelemetryOptions).ConfigureAwait(false);

            // 4) Normal execution
            TelemetryHost.TrackActivation();
            return await Runner.ExecuteAsync(parsed.Options!).ConfigureAwait(false);
        }
    }
}
