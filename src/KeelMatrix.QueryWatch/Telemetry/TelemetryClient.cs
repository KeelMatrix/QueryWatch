// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Facade and entry point for telemetry emission.
    /// This class orchestrates telemetry flow but does not make policy decisions.
    /// </summary>
    internal sealed class TelemetryClient : ITelemetryClient {
        private readonly TelemetryDispatcher dispatcher;
        private readonly Infrastructure.TelemetryHttpSender sender;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryClient"/>.
        /// </summary>
        public TelemetryClient(
            TelemetryDispatcher dispatcher,
            Infrastructure.TelemetryHttpSender sender) {
            this.dispatcher = dispatcher;
            this.sender = sender;
        }

        /// <inheritdoc />
        public void TrackActivation() {
            try {
                var evt = dispatcher.TryCreateActivationEvent();
                if (evt != null) {
                    sender.Send(evt);
                }
            }
            catch {
                // telemetry must never affect application behavior
            }
        }

        /// <inheritdoc />
        public void TrackHeartbeat() {
            try {
                var evt = dispatcher.TryCreateHeartbeatEvent();
                if (evt != null) {
                    sender.Send(evt);
                }
            }
            catch {
                // telemetry must never affect application behavior
            }
        }
    }
}
