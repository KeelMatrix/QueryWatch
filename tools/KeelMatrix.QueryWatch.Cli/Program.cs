// Copyright (c) KeelMatrix
#nullable enable
using KeelMatrix.QueryWatch.Cli.Core;
using KeelMatrix.QueryWatch.Cli.Options;

return await new App().RunAsync(args);

namespace KeelMatrix.QueryWatch.Cli.Core {
    internal sealed class App {
        public async Task<int> RunAsync(string[] args) {
            var parse = CommandLineOptions.Parse(args);
            if (!parse.Success) {
                Console.Error.WriteLine(parse.ErrorMessage);
                if (parse.ShowHelp) { Console.WriteLine(CommandLineOptions.HelpText); }
                return ExitCodes.InvalidArguments;
            }

            if (parse.Options.ShowHelp) {
                Console.WriteLine(CommandLineOptions.HelpText);
                return 0;
            }

            var runner = new Runner();
            return await runner.ExecuteAsync(parse.Options);
        }
    }
}
