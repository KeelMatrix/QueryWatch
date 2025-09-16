using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    internal static class CliRunner {
        private static readonly object _gate = new();
        private static string? _publishedDir;
        private static string? _publishedDll;

        public static (int ExitCode, string StdOut, string StdErr) Run(string[] args, (string Key, string Value)[]? env = null) {
            var repoRoot = FindRepoRoot();
            EnsurePublished(repoRoot, out var dllPath, out var workDir);

            var psi = new ProcessStartInfo("dotnet") {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDir
            };

            // Run the published app: dotnet <dll> -- <args>
            psi.ArgumentList.Add(dllPath);
            foreach (var a in args) psi.ArgumentList.Add(a);

            if (env is not null) {
                foreach (var (k, v) in env)
                    psi.Environment[k] = v;
            }

            using var proc = Process.Start(psi)!;
            var stdout = proc.StandardOutput.ReadToEnd();
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();
            return (proc.ExitCode, stdout, stderr);
        }

        private static void EnsurePublished(string repoRoot, out string dllPath, out string workDir) {
            lock (_gate) {
                if (!string.IsNullOrEmpty(_publishedDll) && !string.IsNullOrEmpty(_publishedDir) && File.Exists(_publishedDll)) {
                    dllPath = _publishedDll!;
                    workDir = _publishedDir!;
                    return;
                }

                var cliProj = Path.Combine(repoRoot, "tools", "KeelMatrix.QueryWatch.Cli", "KeelMatrix.QueryWatch.Cli.csproj");
                if (!File.Exists(cliProj))
                    throw new FileNotFoundException($"CLI project not found: {cliProj}");

                var configuration = GuessConfiguration();
                var tfm = "net8.0";
                var outDir = Path.Combine(repoRoot, "artifacts", "cli-publish", configuration, tfm);
                Directory.CreateDirectory(outDir);

                var psi = new ProcessStartInfo("dotnet") {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = repoRoot
                };
                // Publish the CLI: dotnet publish <proj> -c <cfg> -f net8.0 -o <outDir> --nologo -v minimal
                psi.ArgumentList.Add("publish");
                psi.ArgumentList.Add(cliProj);
                psi.ArgumentList.Add("--configuration");
                psi.ArgumentList.Add(configuration);
                psi.ArgumentList.Add("--framework");
                psi.ArgumentList.Add(tfm);
                psi.ArgumentList.Add("--output");
                psi.ArgumentList.Add(outDir);
                psi.ArgumentList.Add("--nologo");
                psi.ArgumentList.Add("--verbosity");
                psi.ArgumentList.Add("minimal");

                using var proc = Process.Start(psi)!;
                var pubOut = proc.StandardOutput.ReadToEnd();
                var pubErr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0) {
                    var sb = new StringBuilder();
                    sb.AppendLine("dotnet publish failed:");
                    sb.AppendLine(pubOut);
                    sb.AppendLine(pubErr);
                    throw new InvalidOperationException(sb.ToString());
                }

                var dll = Path.Combine(outDir, "KeelMatrix.QueryWatch.Cli.dll");
                var runtimeConfig = Path.Combine(outDir, "KeelMatrix.QueryWatch.Cli.runtimeconfig.json");
                if (!File.Exists(dll) || !File.Exists(runtimeConfig))
                    throw new FileNotFoundException($"Published outputs not found under {outDir}");

                _publishedDll = dll;
                _publishedDir = outDir;
                dllPath = _publishedDll!;
                workDir = _publishedDir!;
            }
        }

        private static string GuessConfiguration() {
            var envCfg = Environment.GetEnvironmentVariable("Configuration");
            if (!string.IsNullOrWhiteSpace(envCfg)) return envCfg!;

            // Derive from test binary path
            var baseDir = AppContext.BaseDirectory ?? "";
            if (baseDir.IndexOf($"{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) >= 0) return "Debug";
            if (baseDir.IndexOf($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) >= 0) return "Release";
            return "Release";
        }

        private static string FindRepoRoot() {
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 12; i++) {
                var sln = Path.Combine(dir, "KeelMatrix.QueryWatch.sln");
                var cliProj = Path.Combine(dir, "tools", "KeelMatrix.QueryWatch.Cli", "KeelMatrix.QueryWatch.Cli.csproj");
                if (File.Exists(sln) || File.Exists(cliProj)) return dir;
                dir = Path.GetFullPath(Path.Combine(dir, ".."));
            }
            return AppContext.BaseDirectory;
        }
    }
}
