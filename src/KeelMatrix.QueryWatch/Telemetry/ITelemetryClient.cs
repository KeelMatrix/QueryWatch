namespace KeelMatrix.QueryWatch.Telemetry
{
    /// <summary>
    /// Defines a minimal interface for emitting anonymous telemetry events.
    /// By default, telemetry is disabled via the no‑op implementation. Implement
    /// this interface to send activation and heartbeat events to your own backend.
    /// </summary>
    public interface ITelemetryClient
    {
        /// <summary>
        /// Called once to indicate that the library has been successfully activated.
        /// </summary>
        void TrackActivation();

        /// <summary>
        /// Called periodically (for example, once a week) to indicate that the library is still in use.
        /// </summary>
        void TrackHeartbeat();
    }

    /// <summary>
    /// A no‑op telemetry client used when telemetry is disabled. Implements
    /// <see cref="ITelemetryClient"/> but performs no operations. Replace with
    /// your own implementation to collect usage data. Be transparent about what
    /// you collect and provide a clear opt‑out mechanism.
    /// </summary>
    public sealed class NoopTelemetryClient : ITelemetryClient
    {
        /// <inheritdoc />
        public void TrackActivation() { }

        /// <inheritdoc />
        public void TrackHeartbeat() { }
    }
}