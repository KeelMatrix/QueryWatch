// Copyright (c) KeelMatrix

using System.Text.Json;

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Tracks local telemetry state to enforce idempotency per project.
    /// State is persisted on disk and guarded by a cross-process file lock.
    /// </summary>
    internal sealed class TelemetryState {
        private const int LockTimeoutMs = 5000;
        private const int MaxProjects = 512;
        private const int MaxCorruptFiles = 10;

        /// <summary>
        /// Absolute path to the telemetry state file on the current machine.
        /// </summary>
        private static readonly string StateFilePath = ResolveStateFilePath();

        /// <summary>
        /// Dedicated lock file used for cross-process synchronization.
        /// </summary>
        private static readonly string LockFilePath = StateFilePath + ".lock";

        /// <summary>
        /// The project-scoped identifier used as the state key.
        /// </summary>
        private readonly string projectHash;

        /// <summary>
        /// In-memory representation of the persisted state.
        /// </summary>
        private StateData data = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TelemetryState"/> class for the given project.
        /// </summary>
        /// <param name="projectHash">Stable project identifier.</param>
        public TelemetryState(string projectHash) {
            this.projectHash = projectHash;
            Load();
        }

        internal static readonly ThreadLocal<Random> JitterRandom =
            new(() => new Random(unchecked((Environment.TickCount * 31) + Environment.CurrentManagedThreadId)));

        public bool ShouldSendActivation() {
            using var _ = AcquireLock();
            LoadUnsafe();
            return !data.Activations.ContainsKey(projectHash);
        }

        public bool ShouldSendHeartbeat(string isoWeek) {
            using var _ = AcquireLock();
            LoadUnsafe();
            return !(data.Heartbeats.TryGetValue(projectHash, out var existing) && existing == isoWeek);
        }

        public void CommitActivation() {
            using var _ = AcquireLock();
            LoadUnsafe();
            data.Activations[projectHash] = true;
            PersistUnsafe();
        }

        public void CommitHeartbeat(string isoWeek) {
            using var _ = AcquireLock();
            LoadUnsafe();
            data.Heartbeats[projectHash] = isoWeek;
            PersistUnsafe();
        }

        /// <summary>
        /// Loads state from disk into memory using a cross-process lock.
        /// </summary>
        private void Load() {
            try {
                using var _ = AcquireLock();
                LoadUnsafe();
            }
            catch {
                TryRecoverCorruptStateUnsafe();
                data = new StateData();
            }
        }

        private void LoadUnsafe() {
            try {
                if (!File.Exists(StateFilePath))
                    return;

                using var stream = new FileStream(
                    StateFilePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read);

                var loaded = JsonSerializer.Deserialize<StateData>(stream);
                if (loaded != null)
                    data = loaded;
            }
            catch {
                data = new StateData();
            }
        }

        private static void TryRecoverCorruptStateUnsafe() {
            try {
                if (File.Exists(StateFilePath)) {
                    var corruptPath = GetUniqueCorruptPath(StateFilePath);

                    try {
                        File.Move(StateFilePath, corruptPath);
                    }
                    catch {
                        try {
                            File.Copy(StateFilePath, corruptPath, overwrite: false);
                            File.Delete(StateFilePath);
                        }
                        catch {
                            // swallow
                        }
                    }

                    EnforceCorruptLimit();
                }
            }
            catch {
                // swallow
            }
        }

        private static void EnforceCorruptLimit() {
            try {
                var dir = Path.GetDirectoryName(StateFilePath)!;
                var file = Path.GetFileName(StateFilePath);

                var corrupt = Directory.EnumerateFiles(dir, $"{file}.corrupt.*")
                                       .Select(p => new FileInfo(p))
                                       .OrderBy(f => f.CreationTimeUtc)
                                       .ToList();

                var excess = corrupt.Count - MaxCorruptFiles;
                if (excess <= 0)
                    return;

                foreach (var fi in corrupt.Take(excess)) {
                    try { fi.Delete(); } catch { /* swallow */ }
                }
            }
            catch {
                // swallow
            }
        }

        private static string GetUniqueCorruptPath(string originalPath) {
            var dir = Path.GetDirectoryName(originalPath)!;
            var file = Path.GetFileName(originalPath);

            // Example: telemetry.state.corrupt.20260122T134455Z.3
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmss'Z'", System.Globalization.CultureInfo.InvariantCulture);

            for (int i = 0; i < 1000; i++) {
                var suffix = i == 0 ? "" : $".{i}";
                var candidate = Path.Combine(dir, $"{file}.corrupt.{timestamp}{suffix}");
                if (!File.Exists(candidate))
                    return candidate;
            }

            // Extreme fallback (still deterministic, no exceptions)
            return Path.Combine(dir, $"{file}.corrupt.{Guid.NewGuid():N}");
        }

        /// <summary>
        /// Persists the current in-memory state to disk using atomic write semantics.
        /// </summary>
        private void PersistUnsafe() {
            try {
                EnforceLimits();
                Directory.CreateDirectory(Path.GetDirectoryName(StateFilePath)!);
                var tmpPath = StateFilePath + ".tmp";

                using (var stream = new FileStream(
                    tmpPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None)) {

                    JsonSerializer.Serialize(stream, data);
                    stream.Flush(true); // ensure durability on supported platforms
                }

                try {
                    if (File.Exists(StateFilePath)) {
                        File.Replace(tmpPath, StateFilePath, null);
                    }
                    else {
                        File.Move(tmpPath, StateFilePath);
                    }
                }
                catch {
                    try { File.Delete(tmpPath); } catch { /* ignore if missing or locked */ }
                }
            }
            catch {
                // Persistence failure must never affect application behavior
            }
        }

        private void EnforceLimits() {
            if (data.Activations.Count > MaxProjects)
                data.Activations = data.Activations
                    .Take(MaxProjects)
                    .ToDictionary(k => k.Key, v => v.Value);

            if (data.Heartbeats.Count > MaxProjects)
                data.Heartbeats = data.Heartbeats
                    .Take(MaxProjects)
                    .ToDictionary(k => k.Key, v => v.Value);
        }

        /// <summary>
        /// Acquires a cross-process exclusive lock using a dedicated lock file.
        /// </summary>
        private static FileLockHandle AcquireLock() {
            Directory.CreateDirectory(Path.GetDirectoryName(StateFilePath)!);

            var start = Environment.TickCount;
            var delayMs = 5;
            const int maxDelay = 100;

            while (Environment.TickCount - start <= LockTimeoutMs) {
                try {
                    var fs = new FileStream(
                        LockFilePath,
                        FileMode.OpenOrCreate,
                        FileAccess.ReadWrite,
                        FileShare.None);

                    return new FileLockHandle(fs);
                }
                catch {
                    Thread.Sleep(delayMs);
                    delayMs = Math.Min(delayMs * 2, maxDelay) + JitterRandom.Value!.Next(0, 5);
                }
            }

            throw new TimeoutException("Failed to acquire telemetry state lock.");
        }

        /// <summary>
        /// Resolves the absolute path of the telemetry state file in a cross-platform manner.
        /// </summary>
        /// <returns>Absolute file path for telemetry state storage.</returns>
        private static string ResolveStateFilePath() {
            return Path.Combine(TelemetryConfig.GetRootDirectory(), "telemetry.state");
        }

        /// <summary>
        /// Disposable wrapper ensuring proper release of the file lock.
        /// </summary>
        private sealed class FileLockHandle : IDisposable {
            private readonly FileStream stream;

            public FileLockHandle(FileStream stream) {
                this.stream = stream;
            }

            public void Dispose() {
                try {
                    stream.Dispose();
                }
                catch {
                    // swallow
                }
            }
        }

        /// <summary>
        /// Serializable container for persisted telemetry state.
        /// </summary>
        private sealed class StateData {
            /// <summary>
            /// Tracks which project hashes have already emitted activation.
            /// </summary>
            public Dictionary<string, bool> Activations { get; set; } = [];

            /// <summary>
            /// Tracks the last heartbeat ISO week per project hash.
            /// </summary>
            public Dictionary<string, string> Heartbeats { get; set; } = [];
        }
    }
}
