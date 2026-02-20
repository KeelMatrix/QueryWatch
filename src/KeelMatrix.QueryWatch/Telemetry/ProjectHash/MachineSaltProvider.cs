// Copyright (c) KeelMatrix

using System.Security.Cryptography;
using System.Text;

namespace KeelMatrix.QueryWatch.Telemetry.ProjectHash {
    internal static class MachineSaltProvider {
        /// <summary>
        /// Best-effort read-or-create. Stored at:
        /// Path.Combine(TelemetryConfig.GetRootDirectory(), "telemetry.salt")
        /// The persisted format remains a hex string of 32 random bytes.
        /// </summary>
        public static byte[] GetOrCreateMachineSaltBytes() {
            try {
                var path = ResolveSaltPath();
                TryEnsureDirectory(path);

                if (File.Exists(path)) {
                    var existing = SafeReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(existing)) {
                        if (TryDecodeHex(existing, out var decoded) && decoded.Length > 0)
                            return decoded;

                        // Best-effort compatibility: if content isn't hex, treat the raw text as bytes.
                        return Encoding.UTF8.GetBytes(existing.Trim());
                    }
                }

                // Generate strong random 32 bytes
                var bytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create()) {
                    rng.GetBytes(bytes);
                }

                // Persist as hex (best-effort atomic write)
                var saltHex = ProjectHashCache.ToLowerHex(bytes);
                var tmp = path + ".tmp";

                try {
                    File.WriteAllText(tmp, saltHex, Encoding.UTF8);
                    try {
                        File.Move(tmp, path);
                    }
                    catch {
                        try { File.Delete(tmp); } catch { /* swallow */ }
                    }
                }
                catch {
                    try { File.Delete(tmp); } catch { /* swallow */ }
                }

                // Re-read in case of race
                if (File.Exists(path)) {
                    var persisted = SafeReadAllText(path);
                    if (!string.IsNullOrWhiteSpace(persisted) &&
                        TryDecodeHex(persisted, out var decodedPersisted) &&
                        decodedPersisted.Length > 0) {
                        return decodedPersisted;
                    }
                }

                return bytes;
            }
            catch {
                // Fallback: ephemeral per-process salt (still prevents trivial correlation)
                var bytes = new byte[32];
                using (var rng = RandomNumberGenerator.Create()) {
                    rng.GetBytes(bytes);
                }
                return bytes;
            }
        }

        private static bool TryDecodeHex(string hex, out byte[] bytes) {
            bytes = [];

            if (string.IsNullOrWhiteSpace(hex))
                return false;

            hex = hex.Trim();
            if ((hex.Length & 1) != 0)
                return false;

            int len = hex.Length / 2;
            var result = new byte[len];

            for (int i = 0; i < len; i++) {
                int hi = HexValue(hex[i * 2]);
                int lo = HexValue(hex[(i * 2) + 1]);
                if (hi < 0 || lo < 0)
                    return false;

                result[i] = (byte)((hi << 4) | lo);
            }

            bytes = result;
            return true;
        }

        private static int HexValue(char c) {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }

        private static string ResolveSaltPath() {
            return Path.Combine(TelemetryConfig.GetRootDirectory(), "telemetry.salt");
        }

        private static void TryEnsureDirectory(string path) {
            try {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
            }
            catch {
                // swallow
            }
        }

        private static string SafeReadAllText(string path) {
            try {
                return File.ReadAllText(path).Trim();
            }
            catch {
                return string.Empty;
            }
        }
    }
}
