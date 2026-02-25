// Copyright (c) KeelMatrix

using System.Security.Cryptography;
using System.Text;

namespace KeelMatrix.QueryWatch.Telemetry.ProjectIdentity {
    /// <summary>
    /// Identity sources: CI-provided repo identity → local Git origin remote → Git root commit (best-effort).
    /// No external processes; best-effort only.
    /// </summary>
    internal static class CiGitIdentityFingerprint {
        public static bool TryCompute(out byte[] fingerprintBytes) {
            try {
                if (TryComputeFromCi(out fingerprintBytes))
                    return true;
            }
            catch { /* swallow */ }

            try {
                if (TryComputeFromGit(out fingerprintBytes))
                    return true;
            }
            catch { /* swallow */ }

            fingerprintBytes = [];
            return false;
        }

        private static bool TryComputeFromCi(out byte[] fingerprintBytes) {
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

            // Bitbucket:
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

        private static bool TryComputeFromGit(out byte[] fingerprintBytes) {
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
    }
}
