// Copyright (c) KeelMatrix

using System.Text.Json;

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Tracks local telemetry state to enforce idempotency.
    /// Responsible for remembering which events have already been sent.
    /// </summary>
    internal sealed class TelemetryState {
        private static readonly string StateFilePath = ResolveStateFilePath();

        /// <summary>
        /// Indicates whether an activation event has already been recorded.
        /// </summary>
        public bool IsActivationSent { get; private set; }

        /// <summary>
        /// Gets the ISO week string of the last recorded heartbeat.
        /// </summary>
        public string? LastHeartbeatWeek { get; private set; }

        public TelemetryState() {
            Load();
        }

        /// <summary>
        /// Marks activation as successfully sent.
        /// </summary>
        public void MarkActivationSent() {
            IsActivationSent = true;
            Persist();
        }

        /// <summary>
        /// Marks the given ISO week as having a recorded heartbeat.
        /// </summary>
        public void MarkHeartbeatSent(string isoWeek) {
            LastHeartbeatWeek = isoWeek;
            Persist();
        }

        private void Load() {
            try {
                if (!File.Exists(StateFilePath))
                    return;

                var json = File.ReadAllText(StateFilePath);
                var data = JsonSerializer.Deserialize<StateData>(json);

                if (data != null) {
                    IsActivationSent = data.IsActivationSent;
                    LastHeartbeatWeek = data.LastHeartbeatWeek;
                }
            }
            catch {
                // corrupt or unreadable state → start fresh
                IsActivationSent = false;
                LastHeartbeatWeek = null;
            }
        }

        private void Persist() {
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(StateFilePath)!);

                var data = new StateData {
                    IsActivationSent = IsActivationSent,
                    LastHeartbeatWeek = LastHeartbeatWeek
                };

                var json = JsonSerializer.Serialize(data);
                File.WriteAllText(StateFilePath, json);
            }
            catch {
                // persistence failure must not affect application behavior
            }
        }

        private static string ResolveStateFilePath() {
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(
                baseDir,
                "KeelMatrix",
                "QueryWatch",
                "telemetry.state");
        }

        private sealed class StateData {
            public bool IsActivationSent { get; set; }
            public string? LastHeartbeatWeek { get; set; }
        }
    }
}
