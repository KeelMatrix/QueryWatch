// Copyright (c) KeelMatrix

using System.Globalization;
using KeelMatrix.QueryWatch.Telemetry.Events;
using KeelMatrix.QueryWatch.Telemetry.Infrastructure;

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Encapsulates all telemetry policy and decision-making.
    /// Determines whether telemetry events should be emitted.
    /// </summary>
    internal sealed class TelemetryDispatcher {
        public static TelemetryDispatcher Instance { get; } = new();
        private static TelemetryClock Clock { get; } = new();
        private static string ProjectHash { get; } = ProjectHashProvider.Get();
        private readonly TelemetryState state;

        private TelemetryDispatcher() {
            state = new(ProjectHash);
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
                ProjectHash,
                RuntimeInfo.Runtime,
                RuntimeInfo.Os,
                RuntimeInfo.IsCi,
                Clock.UtcNow.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Determines whether a heartbeat event should be emitted and,
        /// if so, produces the corresponding event payload.
        /// </summary>
        public HeartbeatEvent? TryCreateHeartbeatEvent() {
            if (TelemetryConfig.IsTelemetryDisabled())
                return null;

            var week = Clock.GetCurrentIsoWeek();
            if (!state.ShouldSendHeartbeat(week))
                return null;

            return new HeartbeatEvent(
                TelemetryConfig.ToolName,
                TelemetryConfig.ToolVersion,
                TelemetryConfig.SchemaVersion,
                ProjectHash,
                week);
        }

        public void CommitActivation() => state.CommitActivation();
        public void CommitHeartbeat(string week) => state.CommitHeartbeat(week);
    }
}
