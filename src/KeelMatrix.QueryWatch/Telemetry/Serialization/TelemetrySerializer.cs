// Copyright (c) KeelMatrix

using System.Text.Json;
using KeelMatrix.QueryWatch.Telemetry.Events;

namespace KeelMatrix.QueryWatch.Telemetry.Serialization {
    /// <summary>
    /// Serializes telemetry events into compact JSON payloads.
    /// </summary>
    internal sealed class TelemetrySerializer {
        private static readonly JsonSerializerOptions Options = new() {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes the given telemetry event to a JSON string.
        /// </summary>
#pragma warning disable CA1822 // Mark members as static
#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
        public string Serialize(TelemetryEventBase telemetryEvent) {
            // ERROR (5): CA1822 Member 'Serialize' does not access instance data and can be marked as static
            // ERROR (6): S2325 Make 'Serialize' a static method.
            if (!TelemetrySchemaValidator.IsValid(telemetryEvent))
                throw new InvalidOperationException("Invalid telemetry payload.");

            return JsonSerializer.Serialize(telemetryEvent, Options);
        }
#pragma warning restore S2325 // Methods and properties that don't access instance data should be static
#pragma warning restore CA1822 // Mark members as static
    }
}
