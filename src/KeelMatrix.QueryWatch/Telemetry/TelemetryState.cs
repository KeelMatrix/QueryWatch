// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Tracks local telemetry idempotency using atomic marker files.
    /// File existence represents committed state. No locks are used.
    /// </summary>
    internal sealed class TelemetryState {
        private static readonly string MarkerDir = Path.Combine(TelemetryConfig.GetRootDirectory(), "markers");
        private readonly string projectHash;

        /// <summary>
        /// Initializes a new instance for the given project hash.
        /// </summary>
        public TelemetryState(string projectHash) {
            this.projectHash = projectHash;
            TryEnsureDirectory();
            TryCleanup();
        }

        /// <summary>
        /// Returns true if activation has not yet been recorded.
        /// </summary>
        public bool ShouldSendActivation() {
            return !File.Exists(GetActivationPath(projectHash));
        }

        /// <summary>
        /// Returns true if no heartbeat exists for the given ISO week.
        /// </summary>
        public bool ShouldSendHeartbeat(string isoWeek) {
            return !File.Exists(GetHeartbeatPath(projectHash, isoWeek));
        }

        /// <summary>
        /// Atomically records activation using CreateNew semantics.
        /// </summary>
        public void CommitActivation() {
            TryCreateMarker(GetActivationPath(projectHash));
        }

        /// <summary>
        /// Atomically records heartbeat for the given ISO week.
        /// </summary>
        public void CommitHeartbeat(string isoWeek) {
            TryCreateMarker(GetHeartbeatPath(projectHash, isoWeek));
        }

        /// <summary>
        /// Attempts to create a marker file atomically.
        /// </summary>
        private static void TryCreateMarker(string path) {
            try {
                using var _ = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }
            catch {
                // already exists or filesystem failure → ignore
            }
        }

        /// <summary>
        /// Deletes oldest marker files when count exceeds limit.
        /// </summary>
        private static void TryCleanup() {
            try {
                var files = Directory.EnumerateFiles(MarkerDir, "*.json")
                                     .Select(p => new FileInfo(p))
                                     .OrderBy(f => f.CreationTimeUtc)
                                     .ToList();

                var excess = files.Count - TelemetryConfig.MaxMarkerFiles;
                if (excess <= 0)
                    return;

                foreach (var f in files.Take(excess)) {
                    try { f.Delete(); } catch { /* swallow */ }
                }
            }
            catch {
                // swallow
            }
        }

        /// <summary>
        /// Ensures marker directory exists.
        /// </summary>
        private static void TryEnsureDirectory() {
            try {
                Directory.CreateDirectory(MarkerDir);
            }
            catch {
                // swallow
            }
        }

        /// <summary>
        /// Resolves activation marker path.
        /// </summary>
        private static string GetActivationPath(string projectHash) {
            return Path.Combine(MarkerDir, $"activation.{projectHash}.json");
        }

        /// <summary>
        /// Resolves heartbeat marker path.
        /// </summary>
        private static string GetHeartbeatPath(string projectHash, string week) {
            return Path.Combine(MarkerDir, $"heartbeat.{projectHash}.{week}.json");
        }
    }
}
