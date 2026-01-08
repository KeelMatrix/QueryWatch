// Copyright (c) KeelMatrix

using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace KeelMatrix.QueryWatch.Telemetry {
    internal static class ProjectHashProvider {
        private static readonly string CachedHash = ComputeHash();

        public static string Get() => CachedHash;

        private static string ComputeHash() {
            try {
                var baseDir = AppContext.BaseDirectory ?? string.Empty;
                var assemblyName = typeof(ProjectHashProvider).Assembly.GetName().Name ?? string.Empty;

                var input = $"{baseDir}|{assemblyName}|querywatch";

                using var sha = SHA256.Create();
#pragma warning disable CA1850 // Prefer static 'HashData' method over 'ComputeHash'
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
#pragma warning restore CA1850 // Prefer static 'HashData' method over 'ComputeHash'

                // hex, lowercase, no separators
                return ToHex(bytes).ToLowerInvariant();
            }
            catch {
                // extremely defensive fallback
                return "unknown";
            }
        }

        private static string ToHex(byte[] bytes) {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

    }
}
