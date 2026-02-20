// Copyright (c) KeelMatrix

using System.Globalization;
using System.Text.RegularExpressions;
using KeelMatrix.QueryWatch.Telemetry.Events;

namespace KeelMatrix.QueryWatch.Telemetry.Serialization {
    /// <summary>
    /// Validates telemetry events against the client-side schema rules.
    /// </summary>
    internal static class TelemetrySchemaValidator {
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
        private static readonly Regex IsoWeekRegex = new(@"^\d{4}-W\d{2}$", RegexOptions.Compiled);
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.

        /// <summary>
        /// Validates the given telemetry event.
        /// </summary>
        public static bool IsValid(TelemetryEventBase telemetryEvent) {
            if (telemetryEvent.SchemaVersion != TelemetryConfig.SchemaVersion)
                return false;

            if (!string.Equals(telemetryEvent.Tool, TelemetryConfig.ToolName, StringComparison.Ordinal))
                return false;

            if (telemetryEvent.ToolVersion.Length > TelemetryConfig.ToolVersionMaxLength)
                return false;

            if (telemetryEvent.ProjectHash.Length > TelemetryConfig.ProjectHashMaxLength)
                return false;

            return telemetryEvent switch {
                ActivationEvent a => ValidateActivation(a),
                HeartbeatEvent h => ValidateHeartbeat(h),
                _ => false
            };
        }

        private static bool ValidateActivation(ActivationEvent a) {
            if (a.Runtime.Length > TelemetryConfig.RuntimeMaxLength)
                return false;

            if (a.Os.Length > TelemetryConfig.OsMaxLength)
                return false;

            if (!DateTimeOffset.TryParse(a.Timestamp, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
                return false;

            return true;
        }

        private static bool ValidateHeartbeat(HeartbeatEvent h) {
            return IsoWeekRegex.IsMatch(h.Week);
        }
    }
}
