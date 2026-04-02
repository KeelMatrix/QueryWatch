// Copyright (c) KeelMatrix

using System.Text.Json;
using FluentAssertions;
using KeelMatrix.QueryWatch.Cli;
using KeelMatrix.QueryWatch.Cli.Telemetry;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    [CollectionDefinition(CollectionName)]
    public sealed class TelemetryCommandCollection : ICollectionFixture<TelemetryTestCleanupFixture> {
        public const string CollectionName = "Telemetry CLI";
    }

    public sealed class TelemetryTestCleanupFixture : IDisposable {
        public void Dispose() {
            CleanupTempDirectories("qwatch-telemetry-*");
            CleanupTempDirectories("qwatch-no-repo-*");
        }

        private static void CleanupTempDirectories(string searchPattern) {
            try {
                foreach (DirectoryInfo directory in new DirectoryInfo(Path.GetTempPath()).EnumerateDirectories(searchPattern)) {
                    try {
                        directory.Delete(recursive: true);
                    }
                    catch {
                        // Best-effort cleanup after the collection finishes.
                    }
                }
            }
            catch {
                // Ignore temp-folder enumeration failures in test cleanup.
            }
        }
    }

    [Collection(TelemetryCommandCollection.CollectionName)]
    public sealed class TelemetryCommandTests {
        [Fact]
        public void Telemetry_Status_Shows_Enabled_When_No_Overrides_Exist() {
            using RepoScope repo = RepoScope.Create();

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "status"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
            _ = stdout.Should().Contain("Telemetry: enabled");
            _ = stdout.Should().Contain("Source: none");
            _ = NormalizeForAssertion(stdout).Should().Contain($"Repo: {NormalizeForAssertion(repo.Root)}");
            _ = stdout.Should().Contain("Repo-local config: missing");
        }

        [Fact]
        public void Telemetry_Status_Shows_Disabled_When_Process_Environment_Disables() {
            using RepoScope repo = RepoScope.Create();

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "status"],
                env: [.. RepoScope.TelemetryEnvironment, ("KEELMATRIX_NO_TELEMETRY", "1")],
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
            _ = stdout.Should().Contain("Telemetry: disabled");
            _ = stdout.Should().Contain("Source: process environment");
            _ = stdout.Should().Contain("Variable: KEELMATRIX_NO_TELEMETRY");
            _ = stdout.Should().Contain("Scope: process-level");
        }

        [Fact]
        public void Telemetry_Status_Shows_Disabled_When_Repository_Config_Disables() {
            using RepoScope repo = RepoScope.Create();
            repo.WriteFile("{" + Environment.NewLine + "  \"disabled\": true" + Environment.NewLine + "}" + Environment.NewLine, "keelmatrix.telemetry.json");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "status"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
            _ = stdout.Should().Contain("Telemetry: disabled");
            _ = stdout.Should().Contain("Source: keelmatrix.telemetry.json");
            _ = NormalizeForAssertion(stdout).Should().Contain(NormalizeForAssertion(Path.Combine(repo.Root, "keelmatrix.telemetry.json")));
            _ = stdout.Should().Contain("Scope: repo-local");
            _ = stdout.Should().Contain("Repo-local config: not qwatch-managed");
        }

        [Fact]
        public void Telemetry_Status_Shows_Disabled_When_DotEnvLocal_Disables() {
            using RepoScope repo = RepoScope.Create();
            repo.WriteFile("KEELMATRIX_NO_TELEMETRY=1" + Environment.NewLine, ".env.local");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "status"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
            _ = stdout.Should().Contain("Telemetry: disabled");
            _ = stdout.Should().Contain("Source: .env.local");
            _ = stdout.Should().Contain("Variable: KEELMATRIX_NO_TELEMETRY");
        }

        [Fact]
        public void Telemetry_Status_Shows_Disabled_When_DotEnv_Disables() {
            using RepoScope repo = RepoScope.Create();
            repo.WriteFile("KEELMATRIX_NO_TELEMETRY=1" + Environment.NewLine, ".env");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "status"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
            _ = stdout.Should().Contain("Telemetry: disabled");
            _ = stdout.Should().Contain("Source: .env");
            _ = stdout.Should().Contain("Variable: KEELMATRIX_NO_TELEMETRY");
        }

        [Fact]
        public void Telemetry_Status_Shows_Process_Override_When_Process_Environment_Masks_Repo_Config() {
            using RepoScope repo = RepoScope.Create();
            repo.WriteFile("{" + Environment.NewLine + "  \"disabled\": true" + Environment.NewLine + "}" + Environment.NewLine, "keelmatrix.telemetry.json");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "status"],
                env: [.. RepoScope.TelemetryEnvironment, ("KEELMATRIX_NO_TELEMETRY", "0")],
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
            _ = stdout.Should().Contain("Telemetry: enabled");
            _ = stdout.Should().Contain("Source: process environment");
            _ = stdout.Should().Contain("Variable: KEELMATRIX_NO_TELEMETRY");
            _ = stdout.Should().Contain("Repo-local config: not qwatch-managed");
        }

        [Fact]
        public void Telemetry_Status_Shows_QueryWatchManaged_Config_State() {
            using RepoScope repo = RepoScope.Create();
            repo.WriteFile(
                "{" + Environment.NewLine +
                "  \"disabled\": true," + Environment.NewLine +
                "  \"managedBy\": \"qwatch\"" + Environment.NewLine +
                "}" + Environment.NewLine,
                "keelmatrix.telemetry.json");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "status"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
            _ = stdout.Should().Contain("Repo-local config: qwatch-managed");
        }

        [Fact]
        public void Telemetry_Status_Json_Uses_Status_Model() {
            using RepoScope repo = RepoScope.Create();
            repo.WriteFile("KEELMATRIX_NO_TELEMETRY=1" + Environment.NewLine, ".env.local");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "status", "--json"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);

            using JsonDocument doc = JsonDocument.Parse(stdout);
            JsonElement effectiveStatus = doc.RootElement.GetProperty("effectiveStatus");
            _ = effectiveStatus.GetProperty("isEnabled").GetBoolean().Should().BeFalse();
            _ = effectiveStatus.GetProperty("winningSourceKind").GetString().Should().Be("dotEnvLocal");
            _ = NormalizeForAssertion(effectiveStatus.GetProperty("winningPath").GetString()).Should().Be(NormalizeForAssertion(Path.Combine(repo.Root, ".env.local")));
            _ = effectiveStatus.GetProperty("winningVariableName").GetString().Should().Be("KEELMATRIX_NO_TELEMETRY");
            _ = effectiveStatus.GetProperty("scope").GetString().Should().Be("repoLocal");
            _ = NormalizeForAssertion(effectiveStatus.GetProperty("repoRoot").GetString()).Should().Be(NormalizeForAssertion(repo.Root));
            _ = doc.RootElement.GetProperty("repoLocalConfigState").GetString().Should().Be("missing");
            _ = NormalizeForAssertion(doc.RootElement.GetProperty("repoLocalConfigPath").GetString()).Should().Be(NormalizeForAssertion(Path.Combine(repo.Root, "keelmatrix.telemetry.json")));
        }

        [Fact]
        public void Telemetry_Status_Json_Reports_Invalid_RepoLocal_Config_State() {
            using RepoScope repo = RepoScope.Create();
            repo.WriteFile("{ not-json", "keelmatrix.telemetry.json");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "status", "--json"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);

            using JsonDocument doc = JsonDocument.Parse(stdout);
            _ = doc.RootElement.GetProperty("repoLocalConfigState").GetString().Should().Be("invalidJson");
        }

        [Fact]
        public void Telemetry_Status_Json_Reports_Oversized_RepoLocal_Config_State() {
            using RepoScope repo = RepoScope.Create();
            repo.WriteFile(new string('x', RepoLocalTelemetryConfigInspector.MaxRepositoryConfigBytes + 1), "keelmatrix.telemetry.json");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "status", "--json"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);

            using JsonDocument doc = JsonDocument.Parse(stdout);
            _ = doc.RootElement.GetProperty("repoLocalConfigState").GetString().Should().Be("tooLarge");
        }

        [Fact]
        public void Telemetry_Disable_Creates_Qwatch_Managed_Repository_Config_When_None_Exists() {
            using RepoScope repo = RepoScope.Create();

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "disable"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            string configPath = Path.Combine(repo.Root, "keelmatrix.telemetry.json");
            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
            _ = File.Exists(configPath).Should().BeTrue();

            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(configPath));
            _ = doc.RootElement.GetProperty("disabled").GetBoolean().Should().BeTrue();
            _ = doc.RootElement.GetProperty("managedBy").GetString().Should().Be("qwatch");
        }

        [Fact]
        public void Telemetry_Disable_Refuses_To_Overwrite_Non_Qwatch_Managed_Config() {
            using RepoScope repo = RepoScope.Create();
            string configPath = repo.WriteFile(
                "{" + Environment.NewLine +
                "  \"disabled\": false," + Environment.NewLine +
                "  \"channel\": \"dev\"" + Environment.NewLine +
                "}" + Environment.NewLine,
                "keelmatrix.telemetry.json");
            string originalConfig = File.ReadAllText(configPath);

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "disable"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(1, stdout + Environment.NewLine + stderr);
            _ = stdout.Should().BeEmpty();
            _ = stderr.Should().Contain("Refusing to overwrite existing repo-local telemetry config because it is not qwatch-managed");
            _ = File.ReadAllText(configPath).Should().Be(originalConfig);
        }

        [Fact]
        public void Telemetry_Disable_Refuses_Invalid_Json_Config() {
            using RepoScope repo = RepoScope.Create();
            string configPath = repo.WriteFile("{ not-json", "keelmatrix.telemetry.json");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "disable"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(1, stdout + Environment.NewLine + stderr);
            _ = stdout.Should().BeEmpty();
            _ = stderr.Should().Contain("contains invalid JSON");
            _ = File.ReadAllText(configPath).Should().Be("{ not-json");
        }

        [Fact]
        public void Telemetry_Disable_Refuses_Oversized_Config() {
            using RepoScope repo = RepoScope.Create();
            string oversized = new string('x', RepoLocalTelemetryConfigInspector.MaxRepositoryConfigBytes + 1);
            string configPath = repo.WriteFile(oversized, "keelmatrix.telemetry.json");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "disable"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(1, stdout + Environment.NewLine + stderr);
            _ = stdout.Should().BeEmpty();
            _ = stderr.Should().Contain("exceeds the");
            _ = File.ReadAllText(configPath).Should().Be(oversized);
        }

        [Fact]
        public void Telemetry_Enable_Removes_Qwatch_Managed_Config_File() {
            using RepoScope repo = RepoScope.Create();
            string configPath = repo.WriteFile(
                "{" + Environment.NewLine +
                "  \"disabled\": true," + Environment.NewLine +
                "  \"managedBy\": \"qwatch\"" + Environment.NewLine +
                "}" + Environment.NewLine,
                "keelmatrix.telemetry.json");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "enable"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
            _ = File.Exists(configPath).Should().BeFalse();
            _ = stdout.Should().Contain("Repo-local qwatch-managed telemetry opt-out removed");
            _ = stdout.Should().Contain("Telemetry: enabled");
        }

        [Fact]
        public void Telemetry_Enable_Rewrites_Config_To_Enabled_When_Preserving_Other_Content() {
            using RepoScope repo = RepoScope.Create();
            string configPath = repo.WriteFile(
                "{" + Environment.NewLine +
                "  \"disabled\": true," + Environment.NewLine +
                "  \"managedBy\": \"qwatch\"," + Environment.NewLine +
                "  \"channel\": \"dev\"" + Environment.NewLine +
                "}" + Environment.NewLine,
                "keelmatrix.telemetry.json");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "enable"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
            _ = File.Exists(configPath).Should().BeTrue();

            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(configPath));
            _ = doc.RootElement.GetProperty("channel").GetString().Should().Be("dev");
            _ = doc.RootElement.GetProperty("managedBy").GetString().Should().Be("qwatch");
            _ = doc.RootElement.TryGetProperty("disabled", out _).Should().BeFalse();
            _ = stdout.Should().Contain("Repo-local qwatch-managed telemetry config updated");
        }

        [Fact]
        public void Telemetry_Enable_Does_Not_Modify_Non_Qwatch_Managed_Config() {
            using RepoScope repo = RepoScope.Create();
            string originalConfig =
                "{" + Environment.NewLine +
                "  \"disabled\": true," + Environment.NewLine +
                "  \"channel\": \"dev\"" + Environment.NewLine +
                "}" + Environment.NewLine;
            string configPath = repo.WriteFile(originalConfig, "keelmatrix.telemetry.json");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "enable"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(1, stdout + Environment.NewLine + stderr);
            _ = stdout.Should().BeEmpty();
            _ = File.ReadAllText(configPath).Should().Be(originalConfig);
            _ = stderr.Should().Contain("not qwatch-managed and was left unchanged");
        }

        [Fact]
        public async Task Telemetry_Commands_Do_Not_Call_TrackActivation() {
            using RepoScope repo = RepoScope.Create();
            using EnvironmentVariableSnapshot envSnapshot = new("KEELMATRIX_NO_TELEMETRY", "DOTNET_CLI_TELEMETRY_OPTOUT", "DO_NOT_TRACK");
            using CurrentDirectoryScope __ = new(repo.WorkingDirectory);
            using ConsoleCaptureScope console = new();
            RecordingCliTelemetry telemetry = new();
            ITelemetryHost previousTelemetry = TelemetryHost.Current;
            TelemetryHost.Current = telemetry;

            try {
                Environment.SetEnvironmentVariable("KEELMATRIX_NO_TELEMETRY", null);
                Environment.SetEnvironmentVariable("DOTNET_CLI_TELEMETRY_OPTOUT", null);
                Environment.SetEnvironmentVariable("DO_NOT_TRACK", null);

                int telemetryCode = await Program.RunAsync(["telemetry", "status"]);
                _ = telemetryCode.Should().Be(0, console.StdOut + Environment.NewLine + console.StdErr);
                _ = telemetry.ActivationCalls.Should().Be(0);

                console.Clear();

                string inputPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "current_ok.json");
                int normalCode = await Program.RunAsync(["--input", inputPath]);
                _ = normalCode.Should().Be(0, console.StdOut + Environment.NewLine + console.StdErr);
                _ = telemetry.ActivationCalls.Should().Be(1);
            }
            finally {
                TelemetryHost.Current = previousTelemetry;
            }
        }

        [Fact]
        public void Telemetry_Enable_Refuses_Invalid_Json_Config() {
            using RepoScope repo = RepoScope.Create();
            string configPath = repo.WriteFile("{ not-json", "keelmatrix.telemetry.json");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "enable"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(1, stdout + Environment.NewLine + stderr);
            _ = stdout.Should().BeEmpty();
            _ = stderr.Should().Contain("contains invalid JSON");
            _ = File.ReadAllText(configPath).Should().Be("{ not-json");
        }

        [Fact]
        public void Telemetry_Help_And_Flag_Docs_Include_Command_Group() {
            (int helpCode, string helpOut, string helpErr) = CliRunner.Run(["--help"]);
            _ = helpCode.Should().Be(0, helpOut + Environment.NewLine + helpErr);
            _ = helpOut.Should().Contain("qwatch telemetry <status|disable|enable> [options]");
            _ = helpOut.Should().Contain("telemetry status [--json]");

            (int flagsCode, string flagsOut, string flagsErr) = CliRunner.Run(["--print-flags-md"]);
            _ = flagsCode.Should().Be(0, flagsOut + Environment.NewLine + flagsErr);
            _ = flagsOut.Should().Contain("qwatch telemetry <status|disable|enable> [options]");
            _ = flagsOut.Should().Contain("telemetry enable");
        }

        [Fact]
        public void Telemetry_Status_Fails_When_No_Repository_Root_Is_Found() {
            string workingDirectory = Path.Combine(Path.GetTempPath(), "qwatch-no-repo-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(workingDirectory);

            try {
                (int code, string stdout, string stderr) = CliRunner.Run(
                    ["telemetry", "status"],
                    env: RepoScope.EmptyTelemetryEnvironment,
                    workingDirectory: workingDirectory);

                _ = code.Should().Be(1, stdout + Environment.NewLine + stderr);
                _ = stderr.Should().Contain("No repository root could be resolved");
            }
            finally {
                try { Directory.Delete(workingDirectory, recursive: true); } catch { /* best-effort */ }
            }
        }

        private sealed class RepoScope : IDisposable {
            internal static readonly (string Key, string? Value)[] EmptyTelemetryEnvironment = [
                ("KEELMATRIX_NO_TELEMETRY", null),
                ("DOTNET_CLI_TELEMETRY_OPTOUT", null),
                ("DO_NOT_TRACK", null)
            ];

            private RepoScope(string root) {
                Root = root;
                WorkingDirectory = Path.Combine(root, "src", "tests");
                Directory.CreateDirectory(Path.Combine(root, ".git"));
                Directory.CreateDirectory(WorkingDirectory);
            }

            internal string Root { get; }
            internal string WorkingDirectory { get; }
            internal static (string Key, string? Value)[] TelemetryEnvironment => EmptyTelemetryEnvironment;

            internal static RepoScope Create() {
                string root = Path.Combine(Path.GetTempPath(), "qwatch-telemetry-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(root);
                return new RepoScope(root);
            }

            internal string WriteFile(string content, params string[] segments) {
                string path = Path.Combine([Root, .. segments]);
                string? directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(path, content);
                return path;
            }

            public void Dispose() {
                try {
                    Directory.Delete(Root, recursive: true);
                }
                catch {
                    // Best-effort cleanup for temp test repos.
                }
            }
        }

        private sealed class ConsoleCaptureScope : IDisposable {
            private readonly TextWriter originalOut;
            private readonly TextWriter originalErr;
            private readonly StringWriter stdoutWriter = new();
            private readonly StringWriter stderrWriter = new();

            public ConsoleCaptureScope() {
                originalOut = Console.Out;
                originalErr = Console.Error;
                Console.SetOut(stdoutWriter);
                Console.SetError(stderrWriter);
            }

            public string StdOut => stdoutWriter.ToString();
            public string StdErr => stderrWriter.ToString();

            public void Clear() {
                stdoutWriter.GetStringBuilder().Clear();
                stderrWriter.GetStringBuilder().Clear();
            }

            public void Dispose() {
                Console.SetOut(originalOut);
                Console.SetError(originalErr);
                stdoutWriter.Dispose();
                stderrWriter.Dispose();
            }
        }

        private sealed class CurrentDirectoryScope : IDisposable {
            private readonly string originalCurrentDirectory;

            public CurrentDirectoryScope(string currentDirectory) {
                originalCurrentDirectory = Environment.CurrentDirectory;
                Environment.CurrentDirectory = currentDirectory;
            }

            public void Dispose() {
                Environment.CurrentDirectory = originalCurrentDirectory;
            }
        }

        private static string NormalizeForAssertion(string? value) {
            value.Should().NotBeNull();

            if (!OperatingSystem.IsMacOS())
                return value!;

            return value!.Replace("/private/var/", "/var/", StringComparison.Ordinal);
        }

        private sealed class EnvironmentVariableSnapshot : IDisposable {
            private readonly (string Name, string? Value)[] snapshot;

            public EnvironmentVariableSnapshot(params string[] names) {
                snapshot = new (string, string?)[names.Length];
                for (int i = 0; i < names.Length; i++) {
                    string name = names[i];
                    snapshot[i] = (name, Environment.GetEnvironmentVariable(name));
                }
            }

            public void Dispose() {
                foreach (var (name, value) in snapshot)
                    Environment.SetEnvironmentVariable(name, value);
            }
        }

        private sealed class RecordingCliTelemetry : ITelemetryHost {
            public int ActivationCalls { get; private set; }

            public void TrackActivation() {
                ActivationCalls++;
            }
        }

    }
}
