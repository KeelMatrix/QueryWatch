// Copyright (c) KeelMatrix

using System.Text;

namespace KeelMatrix.QueryWatch.Telemetry.Infrastructure {
    /// <summary>
    /// Handles low-level transmission of telemetry payloads.
    /// </summary>
    internal static class TelemetryHttpSender {
        private static readonly Uri Url =
            new("https://keelmatrix-nuget-telemetry.dz-bb6.workers.dev", UriKind.Absolute);

#if NET8_0_OR_GREATER
        private readonly static HttpClient httpClient = CreateHttpClient();
#else
        private static HttpClient httpClient = CreateHttpClient();
        private static long createdAtTicks = DateTime.UtcNow.Ticks;
        // Rotate client periodically on netstandard2.0 to avoid stale DNS
        private static readonly TimeSpan ClientLifetime = TimeSpan.FromMinutes(5);

#endif

        public static async Task<bool> TrySendAsync(string json, CancellationToken token) {
            try {
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var client = GetClient();
                var response = await client
                    .PostAsync(Url, content, token)
                    .ConfigureAwait(false);

                return response.IsSuccessStatusCode;
            }
            catch {
                return false;
            }
        }

        private static HttpClient GetClient() {
#if NET8_0_OR_GREATER
            return httpClient;
#else
            // netstandard2.0: periodically rotate client to force DNS re-resolution
            var now = DateTime.UtcNow;
            var created = new DateTime(Interlocked.Read(ref createdAtTicks), DateTimeKind.Utc);

            if (now - created > ClientLifetime) {
                var newClient = CreateHttpClient();
                var old = Interlocked.Exchange(ref httpClient, newClient);
                Interlocked.Exchange(ref createdAtTicks, now.Ticks);

                try { old.Dispose(); } catch { /* swallow */ }
            }

            return httpClient;
#endif
        }

        private static HttpClient CreateHttpClient() {
#if NET8_0_OR_GREATER
            var handler = new SocketsHttpHandler {
                // Forces periodic reconnect → DNS refresh
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),

                ConnectTimeout = TimeSpan.FromSeconds(3),
                MaxConnectionsPerServer = 2
            };

            return new HttpClient(handler) {
                Timeout = TimeSpan.FromSeconds(3)
            };
#else
            return new HttpClient {
                Timeout = TimeSpan.FromSeconds(3)
            };
#endif
        }
    }
}
