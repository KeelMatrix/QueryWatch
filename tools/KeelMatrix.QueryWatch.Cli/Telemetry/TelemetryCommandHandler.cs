// Copyright (c) KeelMatrix

using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using KeelMatrix.QueryWatch.Cli.Core;
using KeelMatrix.QueryWatch.Cli.Options;
using KeelMatrix.Telemetry;

namespace KeelMatrix.QueryWatch.Cli.Telemetry {
    internal static class TelemetryCommandHandler {
        private const string RepositoryConfigFileName = "keelmatrix.telemetry.json";
        private const int MaxRepositoryConfigBytes = 16 * 1024;
        private const string QwatchManagedPropertyName = "qwatchManaged";

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
            RepositoryTelemetryStatus status = GetStatus(repoRoot);

            if (json) {
                await Console.Out.WriteLineAsync(JsonSerializer.Serialize(status, JsonOptions)).ConfigureAwait(false);
            }
            else {
                await Console.Out.WriteLineAsync(FormatHumanStatus(status)).ConfigureAwait(false);
            }

            return ExitCodes.Ok;
        }

        private static async Task<int> ExecuteDisableAsync(string repoRoot) {
            string path = Path.Combine(repoRoot, RepositoryConfigFileName);
            await WriteDisabledConfigAsync(path).ConfigureAwait(false);

            RepositoryTelemetryStatus status = GetStatus(repoRoot);
            await Console.Out.WriteLineAsync($"Repo-local qwatch-managed telemetry opt-out written: {path}").ConfigureAwait(false);
            await WriteEffectiveStatusAsync(status).ConfigureAwait(false);
            return ExitCodes.Ok;
        }

        private static async Task<int> ExecuteEnableAsync(string repoRoot) {
            string path = Path.Combine(repoRoot, RepositoryConfigFileName);
            TelemetryConfigMutation mutation = await EnableConfigAsync(path).ConfigureAwait(false);
            RepositoryTelemetryStatus status = GetStatus(repoRoot);

            string message = mutation switch {
                TelemetryConfigMutation.Removed => $"Repo-local qwatch-managed telemetry opt-out removed: {path}",
                TelemetryConfigMutation.RewrittenEnabled => $"Repo-local qwatch-managed telemetry config updated: {path}",
                TelemetryConfigMutation.NotQwatchManaged => $"Existing repo-local telemetry config is not qwatch-managed and was left unchanged: {path}",
                _ => "No qwatch-managed repo-local telemetry opt-out was found."
            };

            await Console.Out.WriteLineAsync(message).ConfigureAwait(false);
            await WriteEffectiveStatusAsync(status).ConfigureAwait(false);
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

        private static string FormatHumanStatus(RepositoryTelemetryStatus status) {
            StringBuilder sb = new();
            _ = sb.AppendLine($"Telemetry: {(status.IsEnabled ? "enabled" : "disabled")}");
            _ = sb.AppendLine($"Source: {FormatSource(status.WinningSourceKind)}");

            if (!string.IsNullOrEmpty(status.WinningPath))
                _ = sb.AppendLine($"Path: {status.WinningPath}");

            if (!string.IsNullOrEmpty(status.WinningVariableName))
                _ = sb.AppendLine($"Variable: {status.WinningVariableName}");

            _ = sb.AppendLine($"Scope: {FormatScope(status.Scope)}");
            _ = sb.AppendLine($"Repo: {status.RepoRoot}");
            return sb.ToString().TrimEnd();
        }

        private static RepositoryTelemetryStatus GetStatus(string repoRoot) {
            return RepositoryTelemetry.GetEffectiveStatus(repoRoot);
        }

        private static async Task WriteDisabledConfigAsync(string path) {
            JsonObject config = ReadConfigObject(path);
            RemovePropertyCaseInsensitive(config, "disabled");
            RemovePropertyCaseInsensitive(config, QwatchManagedPropertyName);
            config["disabled"] = true;
            config[QwatchManagedPropertyName] = true;

            string json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(path, json + Environment.NewLine, Encoding.UTF8).ConfigureAwait(false);
        }

        private static async Task<TelemetryConfigMutation> EnableConfigAsync(string path) {
            if (!File.Exists(path))
                return TelemetryConfigMutation.None;

            JsonObject? config = TryReadExistingConfigObject(path);
            if (config is null || !IsQwatchManaged(config))
                return TelemetryConfigMutation.NotQwatchManaged;

            bool hadDisabled = TryGetPropertyNameCaseInsensitive(config, "disabled", out string? propertyName);
            if (hadDisabled)
                config.Remove(propertyName!);

            if (HasOnlyQwatchManagedMarker(config)) {
                File.Delete(path);
                return TelemetryConfigMutation.Removed;
            }

            if (!hadDisabled)
                return TelemetryConfigMutation.None;

            string json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(path, json + Environment.NewLine, Encoding.UTF8).ConfigureAwait(false);
            return TelemetryConfigMutation.RewrittenEnabled;
        }

        private static JsonObject ReadConfigObject(string path) {
            JsonObject? existing = TryReadExistingConfigObject(path);
            return existing ?? [];
        }

        private static JsonObject? TryReadExistingConfigObject(string path) {
            if (!TryReadTextFileCapped(path, MaxRepositoryConfigBytes, out string text))
                return null;

            try {
                return JsonNode.Parse(text) as JsonObject;
            }
            catch {
                return null;
            }
        }

        private static bool TryReadTextFileCapped(string path, int maxBytes, out string text) {
            text = string.Empty;

            try {
                FileInfo fileInfo = new(path);
                if (!fileInfo.Exists || fileInfo.Length <= 0 || fileInfo.Length > maxBytes)
                    return false;

                text = File.ReadAllText(path);
                return text.Length > 0;
            }
            catch {
                return false;
            }
        }

        private static bool TryGetPropertyNameCaseInsensitive(JsonObject obj, string name, out string? propertyName) {
            KeyValuePair<string, JsonNode?> property = obj
                .FirstOrDefault(property => property.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (property.Key is not null) {
                propertyName = property.Key;
                return true;
            }

            propertyName = null;
            return false;
        }

        private static void RemovePropertyCaseInsensitive(JsonObject obj, string name) {
            if (TryGetPropertyNameCaseInsensitive(obj, name, out string? propertyName))
                obj.Remove(propertyName!);
        }

        private static bool IsQwatchManaged(JsonObject config) {
            if (!TryGetPropertyNameCaseInsensitive(config, QwatchManagedPropertyName, out string? propertyName))
                return false;

            return IsTruthyJsonNode(config[propertyName!]);
        }

        private static bool HasOnlyQwatchManagedMarker(JsonObject config) {
            return config.Count == 1 && IsQwatchManaged(config);
        }

        private static bool IsTruthyJsonNode(JsonNode? node) {
            if (node is null)
                return false;

            try {
                using JsonDocument doc = JsonDocument.Parse(node.ToJsonString());
                return doc.RootElement.ValueKind switch {
                    JsonValueKind.True => true,
                    JsonValueKind.String => IsTruthyValue(doc.RootElement.GetString()),
                    JsonValueKind.Number => IsTruthyValue(doc.RootElement.GetRawText()),
                    _ => false
                };
            }
            catch {
                return false;
            }
        }

        private static bool IsTruthyValue(string? value) {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string normalized = value.Trim();
            return normalized == "1"
                || normalized.Equals("true", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("y", StringComparison.OrdinalIgnoreCase)
                || normalized.Equals("on", StringComparison.OrdinalIgnoreCase);
        }

        private static string FormatSource(RepositoryTelemetrySourceKind sourceKind) {
            return sourceKind switch {
                RepositoryTelemetrySourceKind.None => "none",
                RepositoryTelemetrySourceKind.ProcessEnvironment => "process environment",
                RepositoryTelemetrySourceKind.RepositoryConfig => RepositoryConfigFileName,
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

        private enum TelemetryConfigMutation {
            None,
            Removed,
            RewrittenEnabled,
            NotQwatchManaged
        }
    }
}
