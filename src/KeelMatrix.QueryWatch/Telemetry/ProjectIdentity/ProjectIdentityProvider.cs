// Copyright (c) KeelMatrix

using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace KeelMatrix.QueryWatch.Telemetry.ProjectIdentity {
    /// <summary>
    /// Computes and caches a stable, anonymous per-project hash for the current process.
    /// All I/O and identity detection MUST run on the telemetry worker thread.
    /// </summary>
    internal sealed class ProjectIdentityProvider {
        private int isComputed; // 0 = not computed, 1 = computed
        private string? cachedProjectHash;

        public static ProjectIdentityProvider Shared { get; } = new();

        /// <summary>
        /// Ensures the project hash is computed and cached.
        /// MUST be called only from the telemetry worker thread.
        /// </summary>
        public string EnsureComputedOnWorkerThread() {
            if (Volatile.Read(ref isComputed) == 1)
                return cachedProjectHash ?? ComputeUninitializedPlaceholderHash();

            try {
                var machineSaltBytes = MachineSaltProvider.GetOrCreateMachineSaltBytes();

                bool identityFromSources;
                byte[] identityFingerprintBytes;
                try {
                    identityFromSources = IdentityFingerprintPipeline.TryComputeIdentityFingerprintBytes(out identityFingerprintBytes);
                }
                catch {
                    identityFromSources = false;
                    identityFingerprintBytes = [];
                }

                if (!identityFromSources) {
                    identityFingerprintBytes = ComputeFallbackFingerprintBytes();
                }

                // ProjectHash = SHA256( MachineSaltBytes || IdentityFingerprintBytes ) => lowercase hex, 64 chars
                var final = Sha256(Concat(machineSaltBytes, identityFingerprintBytes));
                cachedProjectHash = ToLowerHex(final);
            }
            catch {
                // Absolute last resort: deterministic, non-I/O placeholder (should never be used for emission)
                cachedProjectHash = ComputeUninitializedPlaceholderHash();
            }
            finally {
                Volatile.Write(ref isComputed, 1);
            }

            return cachedProjectHash ?? ComputeUninitializedPlaceholderHash();
        }

        /// <summary>
        /// Deterministic, non-I/O placeholder hash for cases where callers attempt to access a project hash
        /// before the worker computed it. This is NOT the final ProjectHash formula and should not be emitted.
        /// </summary>
        internal static string ComputeUninitializedPlaceholderHash() {
            try {
                var fallbackFingerprint = ComputeFallbackFingerprintBytes();
                var bytes = Concat(Encoding.UTF8.GetBytes("uninitialized.v1"), fallbackFingerprint);
                return ToLowerHex(Sha256(bytes));
            }
            catch {
                // Must never throw; return a valid sha256 hex string.
                return new string('0', 64);
            }
        }

        /// <summary>
        /// FallbackFingerprint = SHA256( "fallback.v1" || EntryAssemblyNameOrUnknown || EntryAssemblyPublicKeyTokenOrNopk )
        /// </summary>
        private static byte[] ComputeFallbackFingerprintBytes() {
            var entry = Assembly.GetEntryAssembly();
            var name = entry?.GetName().Name ?? "unknown";

            string pk;
            try {
                var pkt = entry?.GetName().GetPublicKeyToken();
                pk = pkt is { Length: > 0 } ? ToLowerHex(pkt) : "nopk";
            }
            catch {
                pk = "nopk";
            }

            // Concatenation semantics: UTF8("fallback.v1") + UTF8(name) + UTF8(pk)
            var input = Encoding.UTF8.GetBytes("fallback.v1" + name + pk);
            return Sha256(input);
        }

        private static byte[] Sha256(byte[] input) {
            using var sha = SHA256.Create();
            return sha.ComputeHash(input);
        }

        private static byte[] Concat(byte[] a, byte[] b) {
            var combined = new byte[a.Length + b.Length];
            Buffer.BlockCopy(a, 0, combined, 0, a.Length);
            Buffer.BlockCopy(b, 0, combined, a.Length, b.Length);
            return combined;
        }

        internal static string ToLowerHex(byte[] bytes) {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
}
