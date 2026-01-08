// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// A no-op implementation of <see cref="ITelemetryClient"/> used when
    /// telemetry is disabled or unavailable.
    /// </summary>
    internal sealed class NullTelemetryClient : ITelemetryClient {
        /// <inheritdoc />
        public void TrackActivation() {
            // intentionally no-op
        }

        /// <inheritdoc />
        public void TrackHeartbeat() {
            // intentionally no-op
        }
    }
}
