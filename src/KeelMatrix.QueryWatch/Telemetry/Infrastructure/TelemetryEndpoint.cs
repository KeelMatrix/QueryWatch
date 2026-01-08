// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry.Infrastructure {
    /// <summary>
    /// Represents the telemetry ingestion endpoint configuration.
    /// </summary>
    internal sealed class TelemetryEndpoint {
        /// <summary>
        /// Gets the absolute URI of the telemetry endpoint.
        /// </summary>
        public Uri Url { get; }

        public TelemetryEndpoint() {
#pragma warning disable S1075 // URIs should not be hardcoded
            Url = new Uri(
                "https://keelmatrix-nuget-telemetry.dz-bb6.workers.dev",
                UriKind.Absolute);
#pragma warning restore S1075 // URIs should not be hardcoded
        }
    }
}
