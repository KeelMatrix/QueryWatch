// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Defines a minimal internal interface for emitting anonymous telemetry events.
    /// Implementations must never throw or block the calling thread.
    /// </summary>
    internal interface ITelemetryClient {
        /// <summary>
        /// Attempts to emit an activation event if telemetry is enabled
        /// and activation has not yet been recorded.
        /// </summary>
        void TrackActivation();

        /// <summary>
        /// Attempts to emit a heartbeat event if telemetry is enabled
        /// and a heartbeat has not yet been recorded for the current week.
        /// </summary>
        void TrackHeartbeat();
    }
}
