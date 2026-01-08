// Copyright (c) KeelMatrix

using System.Text;
using KeelMatrix.QueryWatch.Telemetry.Events;

namespace KeelMatrix.QueryWatch.Telemetry.Infrastructure {
    /// <summary>
    /// Handles low-level transmission of telemetry payloads.
    /// </summary>
    internal sealed class TelemetryHttpSender {

        private static readonly HttpClient HttpClient = CreateHttpClient();

        private readonly TelemetryEndpoint endpoint;
        private readonly Serialization.TelemetrySerializer serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryHttpSender"/>.
        /// </summary>
        public TelemetryHttpSender(
            TelemetryEndpoint endpoint,
            Serialization.TelemetrySerializer serializer) {
            this.endpoint = endpoint;
            this.serializer = serializer;
        }

        /// <summary>
        /// Sends the given telemetry event using best-effort semantics.
        /// </summary>
        public void Send(TelemetryEventBase telemetryEvent) {
            try {
                var json = serializer.Serialize(telemetryEvent);

                var content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                // Fire-and-forget by design
                _ = HttpClient.PostAsync(endpoint.Url, content);
            }
            catch {
                // Intentionally swallowed:
                // telemetry must never affect application behavior
            }
        }

        private static HttpClient CreateHttpClient() {
            return new HttpClient {
                Timeout = TimeSpan.FromSeconds(2)
            };
        }
    }
}
