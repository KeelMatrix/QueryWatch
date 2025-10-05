// Copyright (c) KeelMatrix
using KeelMatrix.QueryWatch.Cli.Core;
using KeelMatrix.QueryWatch.Cli.Options;

namespace KeelMatrix.QueryWatch.Cli {
    static class Program {
        static async Task<int> Main(string[] args) {
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

            var parsed = CommandLineOptions.Parse(args);

            // 2) If the caller asked for help, always print it and return:
            //    - 0 when parse succeeded (pure help)
            //    - 1 when parse failed (help + error)
            if (parsed.ShowHelp) {
                Console.WriteLine(CommandLineOptions.HelpText);
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

            // 4) Normal execution
            return await Runner.ExecuteAsync(parsed.Options);
        }
    }
}
