// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry.ProjectHash {
    internal static class IdentityFingerprintPipeline {
        /// <summary>
        /// Part 2: CI + Git identity sources.
        /// Part 3: content-based identity sources.
        /// </summary>
        public static bool TryComputeIdentityFingerprintBytes(out byte[] fingerprintBytes) {
            if (ProjectHashCache.TryComputeIdentityFingerprintFromCiOrGit(out fingerprintBytes))
                return true;

            if (ProjectFileIdentityFingerprint.TryComputeIdentityFingerprintFromProjectFiles(out fingerprintBytes))
                return true;

            fingerprintBytes = [];
            return false;
        }
    }
}
