// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Non-blocking facade for telemetry emission.
    /// Must never perform I/O or block the calling thread.
    /// </summary>
    internal sealed class TelemetryClient : ITelemetryClient {
        public void TrackActivation() {
            try {
                Infrastructure.TelemetryDeliveryWorker.RequestActivation();
            }
            catch {
                // must never throw
            }
        }

        public void TrackHeartbeat() {
            try {
                Infrastructure.TelemetryDeliveryWorker.RequestHeartbeat();
            }
            catch {
                // must never throw
            }
        }
    }
}
