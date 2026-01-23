// Copyright (c) KeelMatrix

using System.Text;
using System.Text.Json;
using KeelMatrix.QueryWatch.Telemetry.Events;

namespace KeelMatrix.QueryWatch.Telemetry.Serialization {
    /// <summary>
    /// Serializes telemetry events into compact JSON payloads.
    /// </summary>
    internal static class TelemetrySerializer {
        private static readonly JsonSerializerOptions Options = new() {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes the given telemetry event to a JSON string.
        /// </summary>
        public static string? Serialize(TelemetryEventBase telemetryEvent) {
            if (!TelemetrySchemaValidator.IsValid(telemetryEvent))
                return null;

            var json = JsonSerializer.Serialize(telemetryEvent, telemetryEvent.GetType(), Options);
            if (Encoding.UTF8.GetByteCount(json) > TelemetryConfig.MaxPayloadBytes)
                return null;

            return json;
        }
    }
}
