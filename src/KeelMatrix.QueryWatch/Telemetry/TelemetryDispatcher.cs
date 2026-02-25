// Copyright (c) KeelMatrix

using System.Globalization;
using KeelMatrix.QueryWatch.Telemetry.Events;
using KeelMatrix.QueryWatch.Telemetry.Infrastructure;
using KeelMatrix.QueryWatch.Telemetry.ProjectIdentity;

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Encapsulates all telemetry policy and decision-making.
    /// Determines whether telemetry events should be emitted.
    /// </summary>
    internal sealed class TelemetryDispatcher {
        private readonly TelemetryClock clock;
        private readonly TelemetryState state;
        private readonly string projectHash;

        public TelemetryDispatcher(string projectHash) {
            this.projectHash = string.IsNullOrWhiteSpace(projectHash)
                ? ProjectIdentityProvider.ComputeUninitializedPlaceholderHash()
                : projectHash;

            clock = new TelemetryClock();
            state = new TelemetryState(this.projectHash);
        }

        /// <summary>
        /// Determines whether an activation event should be emitted and,
        /// if so, produces the corresponding event payload.
        /// </summary>
        public ActivationEvent? TryCreateActivationEvent() {
            if (TelemetryConfig.IsTelemetryDisabled())
                return null;

            if (!state.ShouldSendActivation())
                return null;

            return new ActivationEvent(
                TelemetryConfig.ToolName,
                TelemetryConfig.ToolVersion,
                TelemetryConfig.SchemaVersion,
                projectHash,
                RuntimeInfo.Runtime,
                RuntimeInfo.Os,
                RuntimeInfo.IsCi,
                clock.UtcNow.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Determines whether a heartbeat event should be emitted and,
        /// if so, produces the corresponding event payload.
        /// </summary>
        public HeartbeatEvent? TryCreateHeartbeatEvent() {
            if (TelemetryConfig.IsTelemetryDisabled())
                return null;

            var week = clock.GetCurrentIsoWeek();
            if (!state.ShouldSendHeartbeat(week))
                return null;

            return new HeartbeatEvent(
                TelemetryConfig.ToolName,
                TelemetryConfig.ToolVersion,
                TelemetryConfig.SchemaVersion,
                projectHash,
                week);
        }

        public void CommitActivation() => state.CommitActivation();
        public void CommitHeartbeat(string week) => state.CommitHeartbeat(week);
    }
}
