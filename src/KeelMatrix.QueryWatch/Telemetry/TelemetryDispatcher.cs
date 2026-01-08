// Copyright (c) KeelMatrix

using KeelMatrix.QueryWatch.Telemetry.Events;

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Encapsulates all telemetry policy and decision-making.
    /// Determines whether telemetry events should be emitted.
    /// </summary>
    internal sealed class TelemetryDispatcher {
        private readonly TelemetryState state;
        private readonly Infrastructure.TelemetryClock clock;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryDispatcher"/>.
        /// </summary>
        public TelemetryDispatcher(
            TelemetryState state,
            Infrastructure.TelemetryClock clock) {
            this.state = state;
            this.clock = clock;
        }

        /// <summary>
        /// Determines whether an activation event should be emitted and,
        /// if so, produces the corresponding event payload.
        /// </summary>
        public ActivationEvent? TryCreateActivationEvent() {
            if (TelemetryConfig.IsTelemetryDisabled())
                return null;

            if (state.IsActivationSent)
                return null;

            var evt = new ActivationEvent(
                TelemetryConfig.ToolName,
                TelemetryConfig.ToolVersion,
                TelemetryConfig.SchemaVersion,
                ProjectHashProvider.Get(),
                RuntimeInfo.Runtime,
                RuntimeInfo.Os,
                RuntimeInfo.IsCi,
                clock.UtcNow.ToString("O"));

            state.MarkActivationSent();
            return evt;
        }

        /// <summary>
        /// Determines whether a heartbeat event should be emitted and,
        /// if so, produces the corresponding event payload.
        /// </summary>
        public HeartbeatEvent? TryCreateHeartbeatEvent() {
            if (TelemetryConfig.IsTelemetryDisabled())
                return null;

            var week = clock.GetCurrentIsoWeek();
            if (state.LastHeartbeatWeek == week)
                return null;

            var evt = new HeartbeatEvent(
                TelemetryConfig.ToolName,
                TelemetryConfig.ToolVersion,
                TelemetryConfig.SchemaVersion,
                ProjectHashProvider.Get(),
                week);

            state.MarkHeartbeatSent(week);
            return evt;
        }
    }
}
