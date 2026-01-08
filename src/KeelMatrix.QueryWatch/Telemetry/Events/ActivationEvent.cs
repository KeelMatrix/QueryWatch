// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry.Events {
    /// <summary>
    /// Represents a one-time activation telemetry event.
    /// </summary>
    internal sealed class ActivationEvent : TelemetryEventBase {
        public ActivationEvent(
            string tool,
            string toolVersion,
            int schemaVersion,
            string projectHash,
            string runtime,
            string os,
            bool ci,
            string timestamp)
            : base("activation", tool, toolVersion, schemaVersion, projectHash) {
            Runtime = runtime;
            Os = os;
            Ci = ci;
            Timestamp = timestamp;
        }

        /// <summary>The runtime identifier (e.g. net8.0).</summary>
        public string Runtime { get; }

        /// <summary>The operating system identifier.</summary>
        public string Os { get; }

        /// <summary>Indicates whether the tool is running in CI.</summary>
        public bool Ci { get; }

        /// <summary>The UTC timestamp of activation.</summary>
        public string Timestamp { get; }
    }
}
