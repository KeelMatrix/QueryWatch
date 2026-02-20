// Copyright (c) KeelMatrix

using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace KeelMatrix.QueryWatch.Telemetry.ProjectHash {
    /// <summary>
    /// Computes and caches a stable, anonymous per-project hash for the current process.
    /// All I/O and identity detection MUST run on the telemetry worker thread.
    /// </summary>
    internal sealed class ProjectHashCache {
        private int isComputed; // 0 = not computed, 1 = computed
        private string? cachedProjectHash;

        public static ProjectHashCache Shared { get; } = new();

        /// <summary>
        /// Ensures the project hash is computed and cached.
        /// MUST be called only from the telemetry worker thread.
        /// </summary>
        public string EnsureComputedOnWorkerThread() {
            if (Volatile.Read(ref isComputed) == 1)
                return cachedProjectHash ?? ComputeUninitializedPlaceholderHash();

            try {
                var machineSaltBytes = MachineSaltProvider.GetOrCreateMachineSaltBytes();
                bool identityFromSources = IdentityFingerprintPipeline.TryComputeIdentityFingerprintBytes(out byte[] identityFingerprintBytes);

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
        /// Worker-thread identity discovery for Part 2:
        /// CI repo identity → Git origin remote → Git root commit (best-effort) → false (Part 3).
        /// </summary>
        internal static bool TryComputeIdentityFingerprintFromCiOrGit(out byte[] fingerprintBytes) {
            if (TryComputeIdentityFingerprintFromCi(out fingerprintBytes))
                return true;

            if (TryComputeIdentityFingerprintFromGit(out fingerprintBytes))
                return true;

            fingerprintBytes = [];
            return false;
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

        private static bool TryComputeIdentityFingerprintFromCi(out byte[] fingerprintBytes) {
            fingerprintBytes = [];

            // CI identity is attempted only if CI is detected (fast path; no disk I/O).
            if (!RuntimeInfo.IsCi)
                return false;

            if (!TryGetCiIdentityString(out var identityRaw))
                return false;

            if (!RepoKeyNormalizer.TryNormalize(identityRaw, out var normalizedRepoKey))
                return false;

            var prefix = Encoding.UTF8.GetBytes("ci.v1");
            var key = Encoding.UTF8.GetBytes(normalizedRepoKey);
            fingerprintBytes = Sha256(Concat(prefix, key));
            return true;
        }

        private static bool TryGetCiIdentityString(out string identity) {
            identity = string.Empty;

            // GitHub Actions: GITHUB_SERVER_URL + GITHUB_REPOSITORY => "${server}/${owner/repo}"
            if (TryGetEnv("GITHUB_SERVER_URL", out var ghServer) &&
                TryGetEnv("GITHUB_REPOSITORY", out var ghRepo)) {
                identity = CombineUrlLike(ghServer, ghRepo);
                return true;
            }

            // GitLab: CI_PROJECT_URL else CI_SERVER_URL + CI_PROJECT_PATH
            if (TryGetEnv("CI_PROJECT_URL", out var glProjectUrl)) {
                identity = glProjectUrl;
                return true;
            }

            if (TryGetEnv("CI_SERVER_URL", out var glServer) &&
                TryGetEnv("CI_PROJECT_PATH", out var glPath)) {
                identity = CombineUrlLike(glServer, glPath);
                return true;
            }

            // Azure DevOps: SYSTEM_COLLECTIONURI + BUILD_REPOSITORY_NAME
            if (TryGetEnv("SYSTEM_COLLECTIONURI", out var azCollection) &&
                TryGetEnv("BUILD_REPOSITORY_NAME", out var azRepoName)) {
                identity = CombineUrlLike(azCollection, azRepoName);
                return true;
            }

            // Azure DevOps fallback (minimal canonical var): BUILD_REPOSITORY_URI
            if (TryGetEnv("BUILD_REPOSITORY_URI", out var azRepoUri)) {
                identity = azRepoUri;
                return true;
            }

            // Bitbucket: BITBUCKET_GIT_HTTP_ORIGIN + slug (fixed vars supported)
            // - Prefer BITBUCKET_REPO_FULL_NAME (workspace/repo)
            if (TryGetEnv("BITBUCKET_GIT_HTTP_ORIGIN", out var bbOrigin) &&
                TryGetEnv("BITBUCKET_REPO_FULL_NAME", out var bbFullName)) {
                identity = CombineUrlLike(bbOrigin, bbFullName);
                return true;
            }

            // - Fallback: BITBUCKET_WORKSPACE + BITBUCKET_REPO_SLUG
            if (TryGetEnv("BITBUCKET_GIT_HTTP_ORIGIN", out bbOrigin) &&
                TryGetEnv("BITBUCKET_WORKSPACE", out var bbWorkspace) &&
                TryGetEnv("BITBUCKET_REPO_SLUG", out var bbSlug)) {
                identity = CombineUrlLike(bbOrigin, bbWorkspace.TrimEnd('/') + "/" + bbSlug.TrimStart('/'));
                return true;
            }

            return false;
        }

        private static bool TryComputeIdentityFingerprintFromGit(out byte[] fingerprintBytes) {
            fingerprintBytes = [];

            foreach (var start in GitDiscovery.GetStartingPoints()) {
                if (string.IsNullOrWhiteSpace(start))
                    continue;

                if (!GitDiscovery.TryFindGitDirectory(start, out var gitDir))
                    continue;

                // Remote origin identity (preferred for repos).
                if (GitDiscovery.TryReadOriginRemoteUrl(gitDir, out var originUrlRaw) &&
                    RepoKeyNormalizer.TryNormalize(originUrlRaw, out var normalizedOrigin)) {
                    var prefix = Encoding.UTF8.GetBytes("git-remote.v1");
                    var key = Encoding.UTF8.GetBytes(normalizedOrigin);
                    fingerprintBytes = Sha256(Concat(prefix, key));
                    return true;
                }

                // Root commit identity (best-effort; no external processes).
                if (GitDiscovery.TryComputeRootCommitHashBestEffort(gitDir, out var rootCommitHashLowerAscii)) {
                    var prefix = Encoding.UTF8.GetBytes("git-root.v1");
                    var key = Encoding.ASCII.GetBytes(rootCommitHashLowerAscii);
                    fingerprintBytes = Sha256(Concat(prefix, key));
                    return true;
                }

                // If .git was found but neither remote nor root commit identity is available,
                // fall through to Part 3 (not implemented yet). Continue searching other starting points.
            }

            return false;
        }

        private static bool TryGetEnv(string name, out string value) {
            value = string.Empty;
            try {
                var v = Environment.GetEnvironmentVariable(name);
                if (string.IsNullOrWhiteSpace(v))
                    return false;

                value = v.Trim();
                return value.Length > 0;
            }
            catch {
                return false;
            }
        }

        private static string CombineUrlLike(string left, string right) {
            left = (left ?? string.Empty).Trim().TrimEnd('/');
            right = (right ?? string.Empty).Trim().TrimStart('/');
            if (left.Length == 0) return right;
            if (right.Length == 0) return left;
            return left + "/" + right;
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

        public static string ToLowerHex(byte[] bytes) {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            return sb.ToString();
        }
    }
}
