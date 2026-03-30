// Copyright (c) KeelMatrix

using System.Text.Json;
using System.Text.Json.Nodes;

namespace KeelMatrix.QueryWatch.Cli.Telemetry {
    internal static class RepoLocalTelemetryConfigInspector {
        internal const string RepositoryConfigFileName = "keelmatrix.telemetry.json";
        internal const string ManagedByPropertyName = "managedBy";
        internal const string ManagedByQwatchValue = "qwatch";
        internal const int MaxRepositoryConfigBytes = 16 * 1024;

        internal static RepoLocalTelemetryConfigInspection Inspect(string repoRoot) {
            return InspectPath(Path.Combine(repoRoot, RepositoryConfigFileName));
        }

        internal static RepoLocalTelemetryConfigInspection InspectPath(string path) {
            FileInfo fileInfo;
            try {
                fileInfo = new FileInfo(path);
            }
            catch {
                return RepoLocalTelemetryConfigInspection.Unreadable(path);
            }

            try {
                if (!fileInfo.Exists)
                    return RepoLocalTelemetryConfigInspection.Missing(path);

                if (fileInfo.Length > MaxRepositoryConfigBytes)
                    return RepoLocalTelemetryConfigInspection.TooLarge(path);

                string text = File.ReadAllText(path);
                if (text.Length == 0)
                    return RepoLocalTelemetryConfigInspection.InvalidJson(path);

                JsonNode? node = JsonNode.Parse(text);
                if (node is not JsonObject config)
                    return RepoLocalTelemetryConfigInspection.InvalidJson(path);

                return IsManagedByQueryWatch(config)
                    ? RepoLocalTelemetryConfigInspection.QueryWatchManaged(path, config)
                    : RepoLocalTelemetryConfigInspection.NotQueryWatchManaged(path, config);
            }
            catch (JsonException) {
                return RepoLocalTelemetryConfigInspection.InvalidJson(path);
            }
            catch {
                return RepoLocalTelemetryConfigInspection.Unreadable(path);
            }
        }

        internal static JsonObject CreateNewManagedOptOutConfig() {
            return new JsonObject {
                ["disabled"] = true,
                [ManagedByPropertyName] = ManagedByQwatchValue
            };
        }

        internal static void ApplyManagedOptOut(JsonObject config) {
            RemovePropertyCaseInsensitive(config, "disabled");
            RemovePropertyCaseInsensitive(config, ManagedByPropertyName);
            config["disabled"] = true;
            config[ManagedByPropertyName] = ManagedByQwatchValue;
        }

        internal static void NormalizeManagedMarker(JsonObject config) {
            if (!IsManagedByQueryWatch(config))
                return;

            RemovePropertyCaseInsensitive(config, ManagedByPropertyName);
            config[ManagedByPropertyName] = ManagedByQwatchValue;
        }

        internal static bool IsManagedByQueryWatch(JsonObject config) {
            if (!TryGetPropertyNameCaseInsensitive(config, ManagedByPropertyName, out string? propertyName))
                return false;

            return config[propertyName!] is JsonValue value
                && value.TryGetValue(out string? managedBy)
                && string.Equals(managedBy, ManagedByQwatchValue, StringComparison.Ordinal);
        }

        internal static bool TryGetPropertyNameCaseInsensitive(JsonObject obj, string name, out string? propertyName) {
            KeyValuePair<string, JsonNode?> property = obj
                .FirstOrDefault(property => property.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (property.Key is not null) {
                propertyName = property.Key;
                return true;
            }

            propertyName = null;
            return false;
        }

        internal static void RemovePropertyCaseInsensitive(JsonObject obj, string name) {
            if (TryGetPropertyNameCaseInsensitive(obj, name, out string? propertyName))
                obj.Remove(propertyName!);
        }
    }

    internal enum RepoLocalTelemetryConfigStateKind {
        Missing = 0,
        QueryWatchManaged = 1,
        NotQueryWatchManaged = 2,
        Unreadable = 3,
        InvalidJson = 4,
        TooLarge = 5
    }

    internal sealed class RepoLocalTelemetryConfigInspection {
        private RepoLocalTelemetryConfigInspection(string path, RepoLocalTelemetryConfigStateKind stateKind, JsonObject? config) {
            Path = path;
            StateKind = stateKind;
            Config = config;
        }

        internal string Path { get; }
        internal RepoLocalTelemetryConfigStateKind StateKind { get; }
        internal JsonObject? Config { get; }

        internal static RepoLocalTelemetryConfigInspection Missing(string path) {
            return new RepoLocalTelemetryConfigInspection(path, RepoLocalTelemetryConfigStateKind.Missing, config: null);
        }

        internal static RepoLocalTelemetryConfigInspection QueryWatchManaged(string path, JsonObject config) {
            return new RepoLocalTelemetryConfigInspection(path, RepoLocalTelemetryConfigStateKind.QueryWatchManaged, config);
        }

        internal static RepoLocalTelemetryConfigInspection NotQueryWatchManaged(string path, JsonObject config) {
            return new RepoLocalTelemetryConfigInspection(path, RepoLocalTelemetryConfigStateKind.NotQueryWatchManaged, config);
        }

        internal static RepoLocalTelemetryConfigInspection Unreadable(string path) {
            return new RepoLocalTelemetryConfigInspection(path, RepoLocalTelemetryConfigStateKind.Unreadable, config: null);
        }

        internal static RepoLocalTelemetryConfigInspection InvalidJson(string path) {
            return new RepoLocalTelemetryConfigInspection(path, RepoLocalTelemetryConfigStateKind.InvalidJson, config: null);
        }

        internal static RepoLocalTelemetryConfigInspection TooLarge(string path) {
            return new RepoLocalTelemetryConfigInspection(path, RepoLocalTelemetryConfigStateKind.TooLarge, config: null);
        }
    }
}
