// Copyright (c) KeelMatrix

using System.Text.Json;

namespace KeelMatrix.QueryWatch.Telemetry.Storage {
    /// <summary>
    /// Immutable envelope representing a queued telemetry payload.
    /// Stored on disk as JSON.
    /// </summary>
    internal sealed class TelemetryEnvelope {
        public string Id { get; }
        public string PayloadJson { get; }
        public DateTimeOffset EnqueuedUtc { get; }

        public TelemetryEnvelope(string payloadJson)
            : this(Guid.NewGuid().ToString("N"), payloadJson, DateTimeOffset.UtcNow) {
        }

        private TelemetryEnvelope(string id, string payloadJson, DateTimeOffset enqueuedUtc) {
            Id = id;
            PayloadJson = payloadJson;
            EnqueuedUtc = enqueuedUtc;
        }

        public static TelemetryEnvelope Deserialize(string json) {
            return JsonSerializer.Deserialize<TelemetryEnvelope>(json)
                ?? throw new InvalidOperationException("Invalid envelope.");
        }

        public string Serialize() {
            return JsonSerializer.Serialize(this);
        }
    }
}
