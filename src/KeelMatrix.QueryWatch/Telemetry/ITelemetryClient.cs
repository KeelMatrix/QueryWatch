namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Defines a minimal interface for emitting anonymous telemetry events. The default implementation is a no‑op.
    /// </summary>
    public interface ITelemetryClient {
        /// <summary>
        /// Called once to indicate that the library has been successfully activated.
        /// </summary>
        void TrackActivation();

        /// <summary>
        /// Called periodically (e.g., weekly) to indicate ongoing use.
        /// </summary>
        void TrackHeartbeat();
    }

    /// <summary>
    /// No‑op implementation of <see cref="ITelemetryClient"/>.
    /// </summary>
    public sealed class NoopTelemetryClient : ITelemetryClient {
        /// <inheritdoc />
        public void TrackActivation() { }

        /// <inheritdoc />
        public void TrackHeartbeat() { }
    }
}
