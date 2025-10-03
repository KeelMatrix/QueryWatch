// Copyright (c) KeelMatrix
using KeelMatrix.QueryWatch.Cli.Core;
using KeelMatrix.QueryWatch.Cli.Options;

namespace KeelMatrix.QueryWatch.Cli {
    static class Program {
        static async Task<int> Main(string[] args) {
            var parsed = CommandLineOptions.Parse(args);

            // If parsing failed, print help and/or the error, and:
            // - return 0 ONLY when this was a pure --help invocation (no error text)
            // - otherwise return InvalidArguments (1), even if help was printed
            if (!parsed.Success) {
                if (parsed.ShowHelp)
                    Console.WriteLine(CommandLineOptions.HelpText);

                if (!string.IsNullOrEmpty(parsed.ErrorMessage))
                    await Console.Error.WriteLineAsync(parsed.ErrorMessage);

                return string.IsNullOrEmpty(parsed.ErrorMessage)
                    ? ExitCodes.Ok                      // pure --help
                    : ExitCodes.InvalidArguments;       // help + error → 1
            }

            return await Runner.ExecuteAsync(parsed.Options);
        }
    }
}
