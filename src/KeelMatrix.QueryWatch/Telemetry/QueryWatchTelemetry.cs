// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry {
    internal static class QueryWatchTelemetry {
        private static readonly ITelemetryClient Client = CreateClient();

        private static ITelemetryClient CreateClient() {
            try {
                return TelemetryConfig.IsTelemetryDisabled()
                    ? new NullTelemetryClient()
                    : new TelemetryClient(
                        new TelemetryDispatcher(
                            new TelemetryState(),
                            new Infrastructure.TelemetryClock()),
                        new Infrastructure.TelemetryHttpSender(
                            new Infrastructure.TelemetryEndpoint(),
                            new Serialization.TelemetrySerializer()));
            }
            catch {
                // Absolute last line of defense
                return new NullTelemetryClient();
            }
        }

        /// <summary>
        /// Records a one-time activation telemetry event for the current project.
        /// The event is emitted only once and is ignored on subsequent calls.
        /// </summary>
        /// <remarks>
        /// This method is safe to call multiple times and never throws.
        /// Telemetry emission is best-effort and may be disabled via environment configuration.
        /// </remarks>
        public static void TrackActivation() {
            Client.TrackActivation();
        }

        /// <summary>
        /// Records a weekly heartbeat telemetry event indicating continued usage.
        /// At most one heartbeat is emitted per project per ISO week.
        /// </summary>
        /// <remarks>
        /// This method is safe to call multiple times and never throws.
        /// Telemetry emission is best-effort and may be disabled via environment configuration.
        /// </remarks>
        public static void TrackHeartbeat() {
            Client.TrackHeartbeat();
        }

    }
}
