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
                var getName = typeof(ProjectHashProvider).Assembly.GetName();
                var assemblyName = getName.Name ?? string.Empty;
                var salt = GetOrCreateMachineSalt();

                var publicKey = getName.GetPublicKeyToken();
                var pk = publicKey is { Length: > 0 }
                    ? ToHex(publicKey)
                    : "nopk";

                var input = $"{assemblyName}|{pk}|{TelemetryConfig.ToolName}|{salt}";

                using var sha = SHA256.Create();
#pragma warning disable CA1850 // Prefer static 'HashData' method over 'ComputeHash'
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
#pragma warning restore CA1850

                return ToHex(bytes).ToLowerInvariant();
            }
            catch {
                return "unknown";
            }
        }

        private static string GetOrCreateMachineSalt() {
            try {
                var path = ResolveSaltPath();
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);

                // Fast path
                if (File.Exists(path)) {
                    var existing = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrWhiteSpace(existing))
                        return existing;
                }

                // Generate strong random 32 bytes
                var bytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create()) {
                    rng.GetBytes(bytes);
                }
                var salt = ToHex(bytes);

                // Best-effort atomic write
                var tmp = path + ".tmp";
                File.WriteAllText(tmp, salt, Encoding.UTF8);

                try {
                    File.Move(tmp, path);
                }
                catch {
                    try { File.Delete(tmp); } catch { /* swallow */ }
                }

                // Re-read in case of race
                if (File.Exists(path)) {
                    var persisted = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrWhiteSpace(persisted))
                        return persisted;
                }

                return salt;
            }
            catch {
                // Fallback: ephemeral per-process salt (still prevents trivial correlation)
                var bytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create()) {
                    rng.GetBytes(bytes);
                }
                return ToHex(bytes);
            }
        }

        private static string ResolveSaltPath() {
            return Path.Combine(TelemetryConfig.GetRootDirectory(), "telemetry.salt");
        }

        private static string ToHex(byte[] bytes) {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
}
