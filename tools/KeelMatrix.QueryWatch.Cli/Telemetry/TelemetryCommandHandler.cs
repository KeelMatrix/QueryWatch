// Copyright (c) KeelMatrix

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using KeelMatrix.QueryWatch.Cli.Core;
using KeelMatrix.QueryWatch.Cli.Options;
using KeelMatrix.Telemetry;

namespace KeelMatrix.QueryWatch.Cli.Telemetry {
    internal static class TelemetryCommandHandler {
        private static readonly JsonSerializerOptions JsonOptions = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };

        public static async Task<int> ExecuteAsync(TelemetryCommandLineOptions options) {
            string currentDirectory = Environment.CurrentDirectory;
            if (!RepositoryTelemetry.TryResolveRepositoryRoot(currentDirectory, out string repoRoot)) {
                await Console.Error.WriteLineAsync(
                    $"No repository root could be resolved from the current working directory '{currentDirectory}'.")
                    .ConfigureAwait(false);
                return ExitCodes.InvalidArguments;
            }

            return options.Command switch {
                TelemetryCommandKind.Status => await ExecuteStatusAsync(repoRoot, options.Json).ConfigureAwait(false),
                TelemetryCommandKind.Disable => await ExecuteDisableAsync(repoRoot).ConfigureAwait(false),
                TelemetryCommandKind.Enable => await ExecuteEnableAsync(repoRoot).ConfigureAwait(false),
                _ => ExitCodes.InvalidArguments
            };
        }

        private static async Task<int> ExecuteStatusAsync(string repoRoot, bool json) {
            TelemetryStatusResult status = GetStatus(repoRoot);

            if (json) {
                await Console.Out.WriteLineAsync(JsonSerializer.Serialize(status, JsonOptions)).ConfigureAwait(false);
            }
            else {
                await Console.Out.WriteLineAsync(FormatHumanStatus(status)).ConfigureAwait(false);
            }

            return ExitCodes.Ok;
        }

        private static async Task<int> ExecuteDisableAsync(string repoRoot) {
            RepoLocalTelemetryConfigInspection configInspection = RepoLocalTelemetryConfigInspector.Inspect(repoRoot);
            string? failureMessage = GetDisableFailureMessage(configInspection);
            if (failureMessage is not null) {
                await Console.Error.WriteLineAsync(failureMessage).ConfigureAwait(false);
                return ExitCodes.InvalidArguments;
            }

            await WriteDisabledConfigAsync(configInspection).ConfigureAwait(false);

            TelemetryStatusResult status = GetStatus(repoRoot);
            await Console.Out.WriteLineAsync(
                $"Repo-local qwatch-managed telemetry opt-out written: {configInspection.Path}")
                .ConfigureAwait(false);
            await WriteEffectiveStatusAsync(status.EffectiveStatus).ConfigureAwait(false);
            return ExitCodes.Ok;
        }

        private static async Task<int> ExecuteEnableAsync(string repoRoot) {
            RepoLocalTelemetryConfigInspection configInspection = RepoLocalTelemetryConfigInspector.Inspect(repoRoot);
            string? failureMessage = GetEnableFailureMessage(configInspection);
            if (failureMessage is not null) {
                await Console.Error.WriteLineAsync(failureMessage).ConfigureAwait(false);
                return ExitCodes.InvalidArguments;
            }

            string message = await EnableConfigAsync(configInspection).ConfigureAwait(false);
            TelemetryStatusResult status = GetStatus(repoRoot);

            await Console.Out.WriteLineAsync(message).ConfigureAwait(false);
            await WriteEffectiveStatusAsync(status.EffectiveStatus).ConfigureAwait(false);
            return ExitCodes.Ok;
        }

        private static async Task WriteEffectiveStatusAsync(RepositoryTelemetryStatus status) {
            await Console.Out.WriteLineAsync($"Telemetry: {(status.IsEnabled ? "enabled" : "disabled")}").ConfigureAwait(false);
            await Console.Out.WriteLineAsync($"Source: {FormatSource(status.WinningSourceKind)}").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(status.WinningPath))
                await Console.Out.WriteLineAsync($"Path: {status.WinningPath}").ConfigureAwait(false);

            if (!string.IsNullOrEmpty(status.WinningVariableName))
                await Console.Out.WriteLineAsync($"Variable: {status.WinningVariableName}").ConfigureAwait(false);
        }

        private static string FormatHumanStatus(TelemetryStatusResult status) {
            RepositoryTelemetryStatus effectiveStatus = status.EffectiveStatus;
            StringBuilder sb = new();
            _ = sb.AppendLine($"Telemetry: {(effectiveStatus.IsEnabled ? "enabled" : "disabled")}");
            _ = sb.AppendLine($"Source: {FormatSource(effectiveStatus.WinningSourceKind)}");

            if (!string.IsNullOrEmpty(effectiveStatus.WinningPath))
                _ = sb.AppendLine($"Path: {effectiveStatus.WinningPath}");

            if (!string.IsNullOrEmpty(effectiveStatus.WinningVariableName))
                _ = sb.AppendLine($"Variable: {effectiveStatus.WinningVariableName}");

            _ = sb.AppendLine($"Scope: {FormatScope(effectiveStatus.Scope)}");
            _ = sb.AppendLine($"Repo: {effectiveStatus.RepoRoot}");
            _ = sb.AppendLine($"Repo-local config: {FormatRepoLocalConfigState(status.RepoLocalConfigState)}");
            _ = sb.AppendLine($"Repo-local config path: {status.RepoLocalConfigPath}");
            return sb.ToString().TrimEnd();
        }

        private static TelemetryStatusResult GetStatus(string repoRoot) {
            RepoLocalTelemetryConfigInspection configInspection = RepoLocalTelemetryConfigInspector.Inspect(repoRoot);
            return new TelemetryStatusResult(
                RepositoryTelemetry.GetEffectiveStatus(repoRoot),
                configInspection.StateKind,
                configInspection.Path);
        }

        private static async Task WriteDisabledConfigAsync(RepoLocalTelemetryConfigInspection configInspection) {
            JsonObject config = configInspection.StateKind switch {
                RepoLocalTelemetryConfigStateKind.Missing => RepoLocalTelemetryConfigInspector.CreateNewManagedOptOutConfig(),
                RepoLocalTelemetryConfigStateKind.QueryWatchManaged => configInspection.Config!.DeepClone().AsObject(),
                _ => throw new InvalidOperationException("Disable should only write when the config is missing or qwatch-managed.")
            };

            RepoLocalTelemetryConfigInspector.ApplyManagedOptOut(config);

            string json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(configInspection.Path, json + Environment.NewLine, Encoding.UTF8).ConfigureAwait(false);
        }

        private static async Task<string> EnableConfigAsync(RepoLocalTelemetryConfigInspection configInspection) {
            if (configInspection.StateKind == RepoLocalTelemetryConfigStateKind.Missing)
                return $"No repo-local telemetry config exists: {configInspection.Path}";

            JsonObject config = configInspection.Config!.DeepClone().AsObject();
            RepoLocalTelemetryConfigInspector.NormalizeManagedMarker(config);
            bool hadDisabled = RepoLocalTelemetryConfigInspector.TryGetPropertyNameCaseInsensitive(config, "disabled", out string? propertyName);
            if (hadDisabled)
                config.Remove(propertyName!);

            if (hadDisabled && HasOnlyQueryWatchManagedMarker(config)) {
                File.Delete(configInspection.Path);
                return $"Repo-local qwatch-managed telemetry opt-out removed: {configInspection.Path}";
            }

            if (!hadDisabled)
                return $"No qwatch-managed repo-local telemetry opt-out was found: {configInspection.Path}";

            string json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(configInspection.Path, json + Environment.NewLine, Encoding.UTF8).ConfigureAwait(false);
            return $"Repo-local qwatch-managed telemetry config updated: {configInspection.Path}";
        }

        private static string? GetDisableFailureMessage(RepoLocalTelemetryConfigInspection configInspection) {
            return configInspection.StateKind switch {
                RepoLocalTelemetryConfigStateKind.NotQueryWatchManaged =>
                    $"Refusing to overwrite existing repo-local telemetry config because it is not qwatch-managed: {configInspection.Path}",
                RepoLocalTelemetryConfigStateKind.Unreadable =>
                    $"Refusing to overwrite existing repo-local telemetry config because it could not be read: {configInspection.Path}",
                RepoLocalTelemetryConfigStateKind.InvalidJson =>
                    $"Refusing to overwrite existing repo-local telemetry config because it contains invalid JSON: {configInspection.Path}",
                RepoLocalTelemetryConfigStateKind.TooLarge =>
                    $"Refusing to overwrite existing repo-local telemetry config because it exceeds the {RepoLocalTelemetryConfigInspector.MaxRepositoryConfigBytes}-byte inspection limit: {configInspection.Path}",
                _ => null
            };
        }

        private static string? GetEnableFailureMessage(RepoLocalTelemetryConfigInspection configInspection) {
            return configInspection.StateKind switch {
                RepoLocalTelemetryConfigStateKind.NotQueryWatchManaged =>
                    $"Existing repo-local telemetry config is not qwatch-managed and was left unchanged: {configInspection.Path}",
                RepoLocalTelemetryConfigStateKind.Unreadable =>
                    $"Existing repo-local telemetry config could not be read and was left unchanged: {configInspection.Path}",
                RepoLocalTelemetryConfigStateKind.InvalidJson =>
                    $"Existing repo-local telemetry config contains invalid JSON and was left unchanged: {configInspection.Path}",
                RepoLocalTelemetryConfigStateKind.TooLarge =>
                    $"Existing repo-local telemetry config exceeds the {RepoLocalTelemetryConfigInspector.MaxRepositoryConfigBytes}-byte inspection limit and was left unchanged: {configInspection.Path}",
                _ => null
            };
        }

        private static bool HasOnlyQueryWatchManagedMarker(JsonObject config) {
            return config.Count == 1 && RepoLocalTelemetryConfigInspector.IsManagedByQueryWatch(config);
        }

        private static string FormatSource(RepositoryTelemetrySourceKind sourceKind) {
            return sourceKind switch {
                RepositoryTelemetrySourceKind.None => "none",
                RepositoryTelemetrySourceKind.ProcessEnvironment => "process environment",
                RepositoryTelemetrySourceKind.RepositoryConfig => RepoLocalTelemetryConfigInspector.RepositoryConfigFileName,
                RepositoryTelemetrySourceKind.DotEnvLocal => ".env.local",
                RepositoryTelemetrySourceKind.DotEnv => ".env",
                _ => "none"
            };
        }

        private static string FormatScope(RepositoryTelemetryScope scope) {
            return scope switch {
                RepositoryTelemetryScope.RepoLocalDefault => "repo-local default",
                RepositoryTelemetryScope.ProcessEnvironment => "process-level",
                RepositoryTelemetryScope.RepoLocal => "repo-local",
                _ => "repo-local default"
            };
        }

        private static string FormatRepoLocalConfigState(RepoLocalTelemetryConfigStateKind stateKind) {
            return stateKind switch {
                RepoLocalTelemetryConfigStateKind.Missing => "missing",
                RepoLocalTelemetryConfigStateKind.QueryWatchManaged => "qwatch-managed",
                RepoLocalTelemetryConfigStateKind.NotQueryWatchManaged => "not qwatch-managed",
                RepoLocalTelemetryConfigStateKind.Unreadable => "unreadable",
                RepoLocalTelemetryConfigStateKind.InvalidJson => "invalid JSON",
                RepoLocalTelemetryConfigStateKind.TooLarge => "exceeds inspection size cap",
                _ => "missing"
            };
        }
    }

    internal sealed class TelemetryStatusResult(
        RepositoryTelemetryStatus effectiveStatus,
        RepoLocalTelemetryConfigStateKind repoLocalConfigState,
        string repoLocalConfigPath) {
        public RepositoryTelemetryStatus EffectiveStatus { get; } = effectiveStatus;
        public RepoLocalTelemetryConfigStateKind RepoLocalConfigState { get; } = repoLocalConfigState;
        public string RepoLocalConfigPath { get; } = repoLocalConfigPath;
    }
}
