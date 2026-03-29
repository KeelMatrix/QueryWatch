// Copyright (c) KeelMatrix

using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using KeelMatrix.QueryWatch.Cli.Core;
using KeelMatrix.QueryWatch.Cli.Options;

namespace KeelMatrix.QueryWatch.Cli.Telemetry {
    internal static class TelemetryCommandHandler {
        // Modified copy of KeelMatrix.Telemetry TelemetryDisableResolver precedence and file/env parsing
        // so the CLI status surface stays aligned with the package's source-of-truth behavior.
        private const string RepositoryConfigFileName = "keelmatrix.telemetry.json";
        private const string DotEnvFileName = ".env";
        private const string DotEnvLocalFileName = ".env.local";
        private const int MaxRepositoryConfigBytes = 16 * 1024;
        private const int MaxRepositoryEnvFileBytes = 16 * 1024;

        private static readonly string[] OptOutVariableNames = [
            "KEELMATRIX_NO_TELEMETRY",
            "DOTNET_CLI_TELEMETRY_OPTOUT",
            "DO_NOT_TRACK"
        ];

        private static readonly JsonSerializerOptions JsonOptions = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        public static async Task<int> ExecuteAsync(TelemetryCommandLineOptions options) {
            string currentDirectory = Environment.CurrentDirectory;
            if (!RepoRootResolver.TryFindRepositoryRootFromCurrentDirectory(out string repoRoot)) {
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
            string path = Path.Combine(repoRoot, RepositoryConfigFileName);
            await WriteDisabledConfigAsync(path).ConfigureAwait(false);

            TelemetryStatusResult status = GetStatus(repoRoot);
            await Console.Out.WriteLineAsync($"Repo-local telemetry opt-out written: {path}").ConfigureAwait(false);
            await WriteEffectiveStatusAsync(status).ConfigureAwait(false);
            return ExitCodes.Ok;
        }

        private static async Task<int> ExecuteEnableAsync(string repoRoot) {
            string path = Path.Combine(repoRoot, RepositoryConfigFileName);
            TelemetryConfigMutation mutation = await EnableConfigAsync(path).ConfigureAwait(false);
            TelemetryStatusResult status = GetStatus(repoRoot);

            string message = mutation switch {
                TelemetryConfigMutation.Removed => $"Repo-local telemetry opt-out removed: {path}",
                TelemetryConfigMutation.RewrittenEnabled => $"Repo-local telemetry config set to enabled: {path}",
                _ => "No qwatch-managed repo-local telemetry opt-out was found."
            };

            await Console.Out.WriteLineAsync(message).ConfigureAwait(false);
            await WriteEffectiveStatusAsync(status).ConfigureAwait(false);
            return ExitCodes.Ok;
        }

        private static async Task WriteEffectiveStatusAsync(TelemetryStatusResult status) {
            await Console.Out.WriteLineAsync($"Telemetry: {(status.IsEnabled ? "enabled" : "disabled")}").ConfigureAwait(false);

            if (string.Equals(status.WinningSource, "none", StringComparison.Ordinal))
                return;

            if (string.Equals(status.WinningSource, "process environment", StringComparison.Ordinal)) {
                await Console.Out
                    .WriteLineAsync($"Source: process environment variable {status.WinningVariableName}")
                    .ConfigureAwait(false);
            }
            else if (!string.IsNullOrEmpty(status.WinningPathOrVariable)) {
                await Console.Out
                    .WriteLineAsync($"Source: {status.WinningSource} ({status.WinningPathOrVariable})")
                    .ConfigureAwait(false);
            }
            else {
                await Console.Out.WriteLineAsync($"Source: {status.WinningSource}").ConfigureAwait(false);
            }

            if (!string.IsNullOrWhiteSpace(status.Note))
                await Console.Out.WriteLineAsync($"Note: {status.Note}").ConfigureAwait(false);
        }

        private static string FormatHumanStatus(TelemetryStatusResult status) {
            StringBuilder sb = new();
            _ = sb.AppendLine($"Telemetry: {(status.IsEnabled ? "enabled" : "disabled")}");
            _ = sb.AppendLine($"Source: {status.WinningSource}");

            if (!string.IsNullOrEmpty(status.WinningPathOrVariable)) {
                string label = string.Equals(status.Scope, "process-level", StringComparison.Ordinal)
                    ? "Variable"
                    : "Path";
                _ = sb.AppendLine($"{label}: {status.WinningPathOrVariable}");
            }

            if (!string.IsNullOrEmpty(status.WinningVariableName) &&
                !string.Equals(status.Scope, "process-level", StringComparison.Ordinal)) {
                _ = sb.AppendLine($"Variable: {status.WinningVariableName}");
            }

            _ = sb.AppendLine($"Scope: {status.Scope}");
            _ = sb.AppendLine($"Repo: {status.RepoRoot}");

            if (!string.IsNullOrWhiteSpace(status.Note))
                _ = sb.AppendLine($"Note: {status.Note}");

            return sb.ToString().TrimEnd();
        }

        private static TelemetryStatusResult GetStatus(string repoRoot) {
            TelemetryDecision? repositoryDecision = EvaluateRepository(repoRoot);
            TelemetryDecision? processDecision = EvaluateProcessEnvironment();
            TelemetryDecision? winningDecision = processDecision ?? repositoryDecision;

            if (winningDecision is null) {
                return new TelemetryStatusResult {
                    IsEnabled = true,
                    WinningSource = "none",
                    Scope = "repo-local default",
                    RepoRoot = repoRoot,
                    Message = "Telemetry is enabled; no process or repo-local override was found."
                };
            }

            string? note = processDecision is not null && repositoryDecision is not null
                ? "repo-local config is ignored while this process-level override is present"
                : null;
            TelemetryDecision resolvedDecision = winningDecision.Value;

            return new TelemetryStatusResult {
                IsEnabled = resolvedDecision.IsEnabled,
                WinningSource = resolvedDecision.Source,
                WinningPathOrVariable = resolvedDecision.PathOrVariable,
                WinningVariableName = resolvedDecision.VariableName,
                Scope = resolvedDecision.Scope,
                RepoRoot = repoRoot,
                Message = resolvedDecision.Message,
                Note = note
            };
        }

        private static TelemetryDecision? EvaluateProcessEnvironment() {
            bool anyPresent = false;
            string? firstPresentVariable = null;

            foreach (string variableName in OptOutVariableNames) {
                string? value;
                try {
                    value = Environment.GetEnvironmentVariable(variableName);
                }
                catch {
                    continue;
                }

                if (value is null)
                    continue;

                anyPresent = true;
                firstPresentVariable ??= variableName;

                if (IsTruthyValue(value)) {
                    return new TelemetryDecision(
                        false,
                        "process environment",
                        "process-level",
                        variableName,
                        variableName,
                        $"Telemetry is disabled by process environment variable {variableName}.");
                }
            }

            if (!anyPresent || firstPresentVariable is null)
                return null;

            return new TelemetryDecision(
                true,
                "process environment",
                "process-level",
                firstPresentVariable,
                firstPresentVariable,
                $"Telemetry is enabled by process environment variable {firstPresentVariable}.");
        }

        private static TelemetryDecision? EvaluateRepository(string repoRoot) {
            return EvaluateRepositoryConfig(Path.Combine(repoRoot, RepositoryConfigFileName))
                ?? EvaluateDotEnvFile(Path.Combine(repoRoot, DotEnvLocalFileName), DotEnvLocalFileName)
                ?? EvaluateDotEnvFile(Path.Combine(repoRoot, DotEnvFileName), DotEnvFileName);
        }

        private static TelemetryDecision? EvaluateRepositoryConfig(string path) {
            if (!TryReadTextFileCapped(path, MaxRepositoryConfigBytes, out string text))
                return null;

            try {
                using JsonDocument doc = JsonDocument.Parse(text);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                    return null;

                if (!TryGetPropertyCaseInsensitive(doc.RootElement, "disabled", out JsonElement disabledElement))
                    return null;

                bool isDisabled = IsTruthyJsonValue(disabledElement);
                return new TelemetryDecision(
                    !isDisabled,
                    RepositoryConfigFileName,
                    "repo-local",
                    path,
                    null,
                    isDisabled
                        ? $"Telemetry is disabled by {RepositoryConfigFileName}."
                        : $"Telemetry is enabled by {RepositoryConfigFileName}.");
            }
            catch {
                return null;
            }
        }

        private static TelemetryDecision? EvaluateDotEnvFile(string path, string sourceName) {
            if (!TryReadTextFileCapped(path, MaxRepositoryEnvFileBytes, out string text))
                return null;

            bool anyRecognizedAssignment = false;
            string? firstRecognizedVariable = null;

            using StringReader reader = new(text);
            string? line;
            while ((line = reader.ReadLine()) is not null) {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed[0] == '#')
                    continue;

                if (trimmed.StartsWith("export ", StringComparison.OrdinalIgnoreCase))
                    trimmed = trimmed["export ".Length..].TrimStart();

                int equalsIndex = trimmed.IndexOf('=');
                if (equalsIndex <= 0)
                    continue;

                string key = trimmed[..equalsIndex].Trim();
                if (!IsRecognizedOptOutKey(key))
                    continue;

                anyRecognizedAssignment = true;
                firstRecognizedVariable ??= key;

                string value = NormalizeDotEnvValue(trimmed[(equalsIndex + 1)..]);
                if (IsTruthyValue(value)) {
                    return new TelemetryDecision(
                        false,
                        sourceName,
                        "repo-local",
                        path,
                        key,
                        $"Telemetry is disabled by {sourceName} ({key}).");
                }
            }

            if (!anyRecognizedAssignment || firstRecognizedVariable is null)
                return null;

            return new TelemetryDecision(
                true,
                sourceName,
                "repo-local",
                path,
                firstRecognizedVariable,
                $"Telemetry is enabled by {sourceName} ({firstRecognizedVariable}).");
        }

        private static async Task WriteDisabledConfigAsync(string path) {
            JsonObject config = ReadConfigObject(path);
            RemovePropertyCaseInsensitive(config, "disabled");
            config["disabled"] = true;

            string json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(path, json + Environment.NewLine, Encoding.UTF8).ConfigureAwait(false);
        }

        private static async Task<TelemetryConfigMutation> EnableConfigAsync(string path) {
            if (!File.Exists(path))
                return TelemetryConfigMutation.None;

            JsonObject? config = TryReadExistingConfigObject(path);
            if (config is null)
                return TelemetryConfigMutation.None;

            if (!TryGetPropertyNameCaseInsensitive(config, "disabled", out string? propertyName))
                return TelemetryConfigMutation.None;

            if (config.Count == 1) {
                File.Delete(path);
                return TelemetryConfigMutation.Removed;
            }

            config.Remove(propertyName!);
            config["disabled"] = false;

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

        private static bool TryGetPropertyCaseInsensitive(JsonElement element, string name, out JsonElement value) {
            JsonProperty property = element.EnumerateObject()
                .FirstOrDefault(property => property.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (!property.Equals(default(JsonProperty))) {
                value = property.Value;
                return true;
            }

            value = default;
            return false;
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

        private static string NormalizeDotEnvValue(string value) {
            value = value.Trim();

            if (value.Length >= 2) {
                char first = value[0];
                char last = value[^1];
                if ((first == '"' && last == '"') || (first == '\'' && last == '\'')) {
                    value = value[1..^1].Trim();
                }
            }

            return value;
        }

        private static bool IsTruthyJsonValue(JsonElement element) {
            return element.ValueKind switch {
                JsonValueKind.True => true,
                JsonValueKind.String => IsTruthyValue(element.GetString()),
                JsonValueKind.Number => IsTruthyValue(element.GetRawText()),
                _ => false
            };
        }

        private static bool IsRecognizedOptOutKey(string key) {
            return OptOutVariableNames.Contains(key, StringComparer.Ordinal);
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

        private readonly record struct TelemetryDecision(
            bool IsEnabled,
            string Source,
            string Scope,
            string? PathOrVariable,
            string? VariableName,
            string Message);

        private enum TelemetryConfigMutation {
            None,
            Removed,
            RewrittenEnabled
        }
    }

    internal sealed class TelemetryStatusResult {
        public bool IsEnabled { get; init; }
        public string WinningSource { get; init; } = string.Empty;
        public string? WinningPathOrVariable { get; init; }
        public string? WinningVariableName { get; init; }
        public string Scope { get; init; } = string.Empty;
        public string RepoRoot { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string? Note { get; init; }
    }
}
