// Copyright (c) KeelMatrix

using System.Text.Json;
using FluentAssertions;
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
            _ = stdout.Should().Contain($"Repo: {repo.Root}");
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
            _ = stdout.Should().Contain(Path.Combine(repo.Root, "keelmatrix.telemetry.json"));
            _ = stdout.Should().Contain("Scope: repo-local");
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
            _ = stdout.Should().Contain("Note: repo-local config is ignored while this process-level override is present");
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
            _ = doc.RootElement.GetProperty("isEnabled").GetBoolean().Should().BeFalse();
            _ = doc.RootElement.GetProperty("winningSource").GetString().Should().Be(".env.local");
            _ = doc.RootElement.GetProperty("winningPathOrVariable").GetString().Should().Be(Path.Combine(repo.Root, ".env.local"));
            _ = doc.RootElement.GetProperty("winningVariableName").GetString().Should().Be("KEELMATRIX_NO_TELEMETRY");
        }

        [Fact]
        public void Telemetry_Disable_Writes_Repository_Config() {
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
        }

        [Fact]
        public void Telemetry_Enable_Removes_Qwatch_Managed_Config_File() {
            using RepoScope repo = RepoScope.Create();
            string configPath = repo.WriteFile("{" + Environment.NewLine + "  \"disabled\": true" + Environment.NewLine + "}" + Environment.NewLine, "keelmatrix.telemetry.json");

            (int code, string stdout, string stderr) = CliRunner.Run(
                ["telemetry", "enable"],
                env: RepoScope.TelemetryEnvironment,
                workingDirectory: repo.WorkingDirectory);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
            _ = File.Exists(configPath).Should().BeFalse();
            _ = stdout.Should().Contain("Repo-local telemetry opt-out removed");
            _ = stdout.Should().Contain("Telemetry: enabled");
        }

        [Fact]
        public void Telemetry_Enable_Rewrites_Config_To_Enabled_When_Preserving_Other_Content() {
            using RepoScope repo = RepoScope.Create();
            string configPath = repo.WriteFile(
                "{" + Environment.NewLine +
                "  \"disabled\": true," + Environment.NewLine +
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
            _ = doc.RootElement.GetProperty("disabled").GetBoolean().Should().BeFalse();
            _ = doc.RootElement.GetProperty("channel").GetString().Should().Be("dev");
        }

        [Fact]
        public void Telemetry_Commands_Do_Not_Call_TrackActivation() {
            using RepoScope repo = RepoScope.Create();
            string sentinelPath = Path.Combine(repo.Root, "activation-sentinel.txt");

            (int telemetryCode, string telemetryOut, string telemetryErr) = CliRunner.Run(
                ["telemetry", "status"],
                env: [.. RepoScope.TelemetryEnvironment, ("QWATCH_CLI_TRACK_ACTIVATION_SENTINEL", sentinelPath)],
                workingDirectory: repo.WorkingDirectory);

            _ = telemetryCode.Should().Be(0, telemetryOut + Environment.NewLine + telemetryErr);
            _ = File.Exists(sentinelPath).Should().BeFalse();

            string inputPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "current_ok.json");
            (int normalCode, string normalOut, string normalErr) = CliRunner.Run(
                ["--input", inputPath],
                env: [.. RepoScope.TelemetryEnvironment, ("QWATCH_CLI_TRACK_ACTIVATION_SENTINEL", sentinelPath)]);

            _ = normalCode.Should().Be(0, normalOut + Environment.NewLine + normalErr);
            _ = File.Exists(sentinelPath).Should().BeTrue();
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
                ("DO_NOT_TRACK", null),
                ("QWATCH_CLI_TRACK_ACTIVATION_SENTINEL", null)
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
    }
}
