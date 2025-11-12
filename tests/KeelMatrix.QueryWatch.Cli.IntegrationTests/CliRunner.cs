using System.Diagnostics;
using System.Text;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    internal static class CliRunner {
        private static readonly object _gate = new();
        private static string? _publishedDir;
        private static string? _publishedDll;

        public static (int ExitCode, string StdOut, string StdErr) Run(string[] args, (string Key, string Value)[]? env = null) {
            string repoRoot = FindRepoRoot();
            EnsurePublished(repoRoot, out string? dllPath, out string? workDir);

            ProcessStartInfo psi = new("dotnet") {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workDir
            };

            // Run the published app: dotnet <dll> -- <args>
            psi.ArgumentList.Add(dllPath);
            foreach (string a in args) psi.ArgumentList.Add(a);

            if (env is not null) {
                foreach (var (k, v) in env)
                    psi.Environment[k] = v;
            }

            using var proc = Process.Start(psi)!;
            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
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

                string cliProj = Path.Combine(repoRoot, "tools", "KeelMatrix.QueryWatch.Cli", "KeelMatrix.QueryWatch.Cli.csproj");
                if (!File.Exists(cliProj))
                    throw new FileNotFoundException($"CLI project not found: {cliProj}");

                string configuration = GuessConfiguration();
                const string tfm = "net8.0";
                string outDir = Path.Combine(repoRoot, "artifacts", "cli-publish", configuration, tfm);
                _ = Directory.CreateDirectory(outDir);

                ProcessStartInfo psi = new("dotnet") {
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
                string pubOut = proc.StandardOutput.ReadToEnd();
                string pubErr = proc.StandardError.ReadToEnd();
                proc.WaitForExit();

                if (proc.ExitCode != 0) {
                    StringBuilder sb = new();
                    _ = sb.AppendLine("dotnet publish failed:");
                    _ = sb.AppendLine(pubOut);
                    _ = sb.AppendLine(pubErr);
                    throw new InvalidOperationException(sb.ToString());
                }

                string dll = Path.Combine(outDir, "KeelMatrix.QueryWatch.Cli.dll");
                string runtimeConfig = Path.Combine(outDir, "KeelMatrix.QueryWatch.Cli.runtimeconfig.json");
                if (!File.Exists(dll) || !File.Exists(runtimeConfig))
                    throw new FileNotFoundException($"Published outputs not found under {outDir}");

                _publishedDll = dll;
                _publishedDir = outDir;
                dllPath = _publishedDll!;
                workDir = _publishedDir!;
            }
        }

        private static string GuessConfiguration() {
            string? envCfg = Environment.GetEnvironmentVariable("Configuration");
            if (!string.IsNullOrWhiteSpace(envCfg)) return envCfg!;

            // Derive from test binary path
            string baseDir = AppContext.BaseDirectory ?? "";
            if (baseDir.Contains($"{Path.DirectorySeparatorChar}Debug{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)) {
                return "Debug";
            }
            else if (baseDir.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)) {
                return "Release";
            }
            else {
                return "Release";
            }
        }

        private static string FindRepoRoot() {
            string dir = AppContext.BaseDirectory;
            for (int i = 0; i < 12; i++) {
                string sln = Path.Combine(dir, "KeelMatrix.QueryWatch.sln");
                string cliProj = Path.Combine(dir, "tools", "KeelMatrix.QueryWatch.Cli", "KeelMatrix.QueryWatch.Cli.csproj");
                if (File.Exists(sln) || File.Exists(cliProj)) return dir;
                dir = Path.GetFullPath(Path.Combine(dir, ".."));
            }
            return AppContext.BaseDirectory;
        }
    }
}
