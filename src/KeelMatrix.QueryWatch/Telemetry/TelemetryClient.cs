// Copyright (c) KeelMatrix

using KeelMatrix.QueryWatch.Telemetry.Serialization;

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Facade and entry point for telemetry emission.
    /// This class orchestrates telemetry flow but does not make policy decisions.
    /// </summary>
    internal sealed class TelemetryClient : ITelemetryClient {
        /// <inheritdoc />
        public void TrackActivation() {
            try {
                var evt = TelemetryDispatcher.Instance.TryCreateActivationEvent();
                if (evt == null)
                    return;

                var json = TelemetrySerializer.Serialize(evt);
                if (json == null)
                    return;

                Infrastructure.QueueWorkerBridge.Enqueue(json);
                TelemetryDispatcher.Instance.CommitActivation();
            }
            catch {
                // telemetry must never affect application behavior
            }
        }

        /// <inheritdoc />
        public void TrackHeartbeat() {
            try {
                var evt = TelemetryDispatcher.Instance.TryCreateHeartbeatEvent();
                if (evt == null)
                    return;

                var json = TelemetrySerializer.Serialize(evt);
                if (json == null)
                    return;

                Infrastructure.QueueWorkerBridge.Enqueue(json);
                TelemetryDispatcher.Instance.CommitHeartbeat(evt.Week);
            }
            catch {
                // telemetry must never affect application behavior
            }
        }
    }
}
