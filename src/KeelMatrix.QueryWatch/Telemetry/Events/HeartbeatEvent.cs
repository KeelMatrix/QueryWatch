// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry.Events {
    /// <summary>
    /// Represents a weekly heartbeat telemetry event.
    /// </summary>
    internal sealed class HeartbeatEvent : TelemetryEventBase {
        public HeartbeatEvent(
            string tool,
            string toolVersion,
            int schemaVersion,
            string projectHash,
            string week)
            : base("heartbeat", tool, toolVersion, schemaVersion, projectHash) {
            Week = week;
        }

        /// <summary>The ISO week string (YYYY-Www).</summary>
        public string Week { get; }
    }
}
