// Copyright (c) KeelMatrix

using System.Security.Cryptography;
using System.Text;

namespace KeelMatrix.QueryWatch.Telemetry.ProjectIdentity {
    internal static class MachineSaltProvider {
        /// <summary>
        /// Best-effort read-or-create. Stored at:
        /// Path.Combine(TelemetryConfig.GetRootDirectory(), "telemetry.salt")
        /// The persisted format remains a hex string of 32 random bytes.
        /// </summary>
        public static byte[] GetOrCreateMachineSaltBytes() {
            var path = ResolveSaltPath();

            // If telemetry is already disabled for this process, don't do I/O; return a valid value.
            // (Caller should still respect IsTelemetryDisabled() and not emit.)
            if (TelemetryConfig.IsTelemetryDisabled())
                return GenerateRandomSaltBytes();

            TryEnsureDirectory(path);

            // 1) Try read existing, with size cap + strict validation.
            if (TryReadPersistedSalt(path, out var existing))
                return existing;

            // 2) Regenerate once and require persistence. If persistence fails, disable telemetry.
            var newSalt = GenerateRandomSaltBytes();
            if (TryPersistSaltAtomically(path, newSalt)) {
                // Re-read for race consistency / confirm we wrote something valid.
                if (TryReadPersistedSalt(path, out var reread))
                    return reread;

                // If we can't read back what we wrote, treat as persistence failure.
                TelemetryConfig.DisableTelemetryForCurrentProcess();
                return GenerateRandomSaltBytes();
            }

            // 3) Cannot persist => disable telemetry for the current process.
            TelemetryConfig.DisableTelemetryForCurrentProcess();
            return GenerateRandomSaltBytes();
        }

        private static bool TryReadPersistedSalt(string path, out byte[] bytes) {
            bytes = [];

            try {
                var fi = new FileInfo(path);
                if (!fi.Exists)
                    return false;

                // Hard cap to avoid loading attacker/corrupt large files into memory.
                if (fi.Length <= 0 || fi.Length > TelemetryConfig.MaxSaltFileBytes)
                    return false;

                // Read as text (expected hex), but still validate strictly.
                var text = SafeReadAllText(path);
                if (string.IsNullOrWhiteSpace(text))
                    return false;

                text = text.Trim();

                // Preferred: strict hex of 32 bytes => 64 hex chars.
                if (text.Length == TelemetryConfig.ExpectedSaltBytes * 2 &&
                    TryDecodeHex(text, out var decoded) &&
                    decoded.Length == TelemetryConfig.ExpectedSaltBytes) {
                    bytes = decoded;
                    return true;
                }

                // Invalid content: treat as corrupt. Caller will regenerate and require persistence.
                return false;
            }
            catch {
                return false;
            }
        }

        private static bool TryPersistSaltAtomically(string path, byte[] saltBytes) {
            try {
                var saltHex = ProjectIdentityProvider.ToLowerHex(saltBytes);
                var tmp = path + ".tmp";

                try {
                    File.WriteAllText(tmp, saltHex, Encoding.UTF8);
#if NET8_0_OR_GREATER
                    File.Move(tmp, path, overwrite: true);
                    return true;
#else
                    // netstandard2.0: emulate overwrite safely.
                    try {
                        if (File.Exists(path))
                            File.Delete(path);

                        File.Move(tmp, path);
                        return File.Exists(path);
                    }
                    catch {
                        try { File.Delete(tmp); } catch { /* swallow */ }
                        return false;
                    }
#endif
                }
                catch {
                    try { File.Delete(tmp); } catch { /* swallow */ }
                    return false;
                }
            }
            catch {
                return false;
            }
        }

        private static byte[] GenerateRandomSaltBytes() {
            var bytes = new byte[TelemetryConfig.ExpectedSaltBytes];
            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(bytes);
            }
            return bytes;
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
