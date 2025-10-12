using Xunit;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests {
    // Ensures env vars exist for all tests in this assembly.
    public sealed class SmokeEnv : ICollectionFixture<SmokeEnv.Setup> {
        public sealed class Setup : IDisposable {
            private readonly bool _startedCompose;
            public Setup() {
                const string passwordSQLSERVER = "SqlServer!Passw0rd";
                const string passwordPOSTGRES = "postgres";

                // 1) If running under VS (no MSBuild VSTest targets), ensure DBs are up.
                //    This is idempotent and cheap when already up (CLI path).
                var composeFile = FindComposeFile();
                if (composeFile is not null && DockerAvailable()
                    && (!PortOpen("localhost", 14333) || !PortOpen("localhost", 5433))) {
                    // docker compose up -d --wait
                    _startedCompose = RunDockerCompose(composeFile, $"up -d --wait");
                }

                // 2) Provide default connection strings (used by smoke tests).
                SetIfEmpty("QWATCH__SQLSERVER__CS",
                    $"Server=localhost,14333;User Id=sa;Password={passwordSQLSERVER};Encrypt=True;TrustServerCertificate=True;Initial Catalog=master");
                SetIfEmpty("QWATCH__POSTGRES__CS",
                    $"Host=localhost;Port=5433;Username=postgres;Password={passwordPOSTGRES};Database=postgres");
            }

            private static void SetIfEmpty(string name, string value) {
                var cur = Environment.GetEnvironmentVariable(name);
                if (string.IsNullOrWhiteSpace(cur)) Environment.SetEnvironmentVariable(name, value);
            }

            public void Dispose() {
                // docker compose down only if we were the ones to start it
                var composeFile = FindComposeFile();
                if (_startedCompose && composeFile is not null && DockerAvailable()) {
                    RunDockerCompose(composeFile, $"down -v --remove-orphans");
                }
            }

            private static bool DockerAvailable() => Run("docker", "compose version") == 0;

            private static string? FindComposeFile() {
                // repo-relative: tests/docker-compose.db.yml
                var f = "tests/docker-compose.db.yml";
                var dir = new DirectoryInfo(AppContext.BaseDirectory);
                for (var i = 0; i < 8 && dir is not null; i++, dir = dir.Parent) {
                    var path = Path.Combine(dir.FullName, f);
                    if (File.Exists(path)) return path;
                }
                return null;
            }

            private static bool PortOpen(string host, int port) {
                try {
                    using var c = new System.Net.Sockets.TcpClient();
                    var t = c.ConnectAsync(host, port);
                    return t.Wait(TimeSpan.FromMilliseconds(500)) && c.Connected;
                }
                catch { return false; }
            }

            private static bool RunDockerCompose(string composeFile, string args) =>
                Run("docker", $"compose -f \"{composeFile}\" {args}") == 0;

            private static int Run(string file, string args) {
                using var p = new System.Diagnostics.Process {
                    StartInfo = new System.Diagnostics.ProcessStartInfo {
                        FileName = file,
                        Arguments = args,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                p.Start(); p.WaitForExit();
                return p.ExitCode;
            }
        }
    }
}
