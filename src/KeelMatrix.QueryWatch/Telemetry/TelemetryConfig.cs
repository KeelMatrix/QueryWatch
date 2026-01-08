// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Central configuration for telemetry constants and limits.
    /// Acts as the single source of truth for schema values.
    /// </summary>
    internal static class TelemetryConfig {
        /// <summary>The telemetry tool identifier.</summary>
        public static string ToolName { get; } = "querywatch";

        /// <summary>The current tool version.</summary>
        public static string ToolVersion { get; }
            = typeof(TelemetryConfig).Assembly.GetName().Version?.ToString() ?? "unknown";

        /// <summary>The supported telemetry schema version.</summary>
        public static int SchemaVersion { get; } = 1;

        /// <summary>Maximum allowed payload size in bytes.</summary>
        public static int MaxPayloadBytes { get; } = 512;

        /// <summary>
        /// Determines whether telemetry is globally disabled via environment variables.
        /// </summary>
        public static bool IsTelemetryDisabled() {
            var value = Environment.GetEnvironmentVariable("KeelMatrix.QueryWatch_NO_TELEMETRY");
            if (string.IsNullOrWhiteSpace(value))
                return false;

            return value == "1"
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
        }
    }
}
