// Copyright (c) KeelMatrix

using System.Text;
using KeelMatrix.QueryWatch.Telemetry.Storage;

namespace KeelMatrix.QueryWatch.Telemetry.Infrastructure {
    /// <summary>
    /// Filesystem-backed durable queue using one JSON file per entry.
    /// Safe across crashes and multiple processes.
    /// </summary>
    internal sealed class DurableTelemetryQueue {
        public static readonly DurableTelemetryQueue Instance = new();
        private const int MaxQueueItems = 128;

        private readonly string pendingDir;
        private readonly string processingDir;

        private DurableTelemetryQueue() {
            string root = ResolveQueueRoot();
            pendingDir = Path.Combine(root, "pending");
            processingDir = Path.Combine(root, "processing");

            Directory.CreateDirectory(pendingDir);
            Directory.CreateDirectory(processingDir);

            CleanupTmpFiles(pendingDir);
            CleanupTmpFiles(processingDir);

            CrashRecovery();
        }

        private static void CleanupTmpFiles(string dir) {
            try {
                foreach (var file in Directory.EnumerateFiles(dir, "*.tmp")) {
                    SafeDelete(file);
                }
            }
            catch {
                // swallow
            }
        }

        private void CrashRecovery() {
            foreach (var file in Directory.EnumerateFiles(processingDir, "*.json")) {
                try {
                    var target = Path.Combine(pendingDir, Path.GetFileName(file));
                    try { File.Delete(target); } catch { /* file does not exist, that's fine. */ }
                    File.Move(file, target);
                }
                catch { /* swallow */ }
            }
        }

        /// <summary>
        /// Enqueues a payload to disk using atomic tmp + rename.
        /// </summary>
        public void Enqueue(string payloadJson) {
            try {
                EnforceLimit();

                var envelope = new TelemetryEnvelope(payloadJson);
                var finalPath = Path.Combine(pendingDir, $"{envelope.Id}.json");
                var tmpPath = finalPath + ".tmp";

                using var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None);
                using var sw = new StreamWriter(fs);
                sw.Write(envelope.Serialize());
                sw.Flush();
                fs.Flush(true);

                try {
                    File.Move(tmpPath, finalPath);
                }
                catch {
                    // If destination somehow exists or race occurred, just drop tmp
                    try { File.Delete(tmpPath); } catch { /* swallow */ }
                }
            }
            catch {
                // Must never affect caller
            }
        }

        /// <summary>
        /// Attempts to claim up to maxItems for processing.
        /// Claimed items are atomically moved into processing.
        /// </summary>
        public IEnumerable<ClaimedItem> TryClaim(int maxItems) {
            var results = new List<ClaimedItem>();

            try {
                foreach (var file in Directory.EnumerateFiles(pendingDir, "*.json")
                                              .OrderBy(File.GetCreationTimeUtc)
                                              .Take(maxItems)) {
                    var name = Path.GetFileName(file);
                    var claimedPath = Path.Combine(processingDir, name);

                    try {
                        File.Move(file, claimedPath);
                    }
                    catch {
                        continue; // another process claimed it
                    }

                    string json;
                    try {
                        json = File.ReadAllText(claimedPath);
                    }
                    catch {
                        SafeDelete(claimedPath);
                        continue;
                    }

                    TelemetryEnvelope envelope;
                    if (json.Length > 4096) {
                        SafeDelete(claimedPath);
                        continue;
                    }

                    try {
                        envelope = TelemetryEnvelope.Deserialize(json);
                    }
                    catch {
                        SafeDelete(claimedPath);
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(envelope.PayloadJson) ||
                        Encoding.UTF8.GetByteCount(envelope.PayloadJson) > TelemetryConfig.MaxPayloadBytes) {
                        SafeDelete(claimedPath);
                        continue;
                    }

                    results.Add(new ClaimedItem(claimedPath, envelope));
                }
            }
            catch {
                // ignore
            }

            return results;
        }

        /// <summary>
        /// Permanently deletes a successfully delivered item.
        /// </summary>
        public static void Complete(ClaimedItem item) {
            SafeDelete(item.Path);
        }

        /// <summary>
        /// Returns a failed item back to pending.
        /// </summary>
        public void Abandon(ClaimedItem item) {
            try {
                var target = Path.Combine(pendingDir, Path.GetFileName(item.Path));
                File.Move(item.Path, target);
            }
            catch {
                // swallow
            }
        }

        /// <summary>
        /// Deletes oldest items when size limit exceeded.
        /// </summary>
        private void EnforceLimit() {
            try {
                EnforceLimitOnDirectory(pendingDir);
                EnforceLimitOnDirectory(processingDir);
            }
            catch {
                // swallow
            }
        }

        private static void EnforceLimitOnDirectory(string dir) {
            try {
                var files = Directory.EnumerateFiles(dir, "*.json")
                                     .OrderBy(File.GetCreationTimeUtc)
                                     .ToList();

                var excess = files.Count - MaxQueueItems;
                if (excess <= 0)
                    return;

                foreach (var file in files.Take(excess)) {
                    SafeDelete(file);
                }
            }
            catch {
                // swallow
            }
        }

        private static void SafeDelete(string path) {
            try {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch {
                // swallow
            }
        }

        private static string ResolveQueueRoot() {
            return Path.Combine(TelemetryConfig.GetRootDirectory(), "telemetry.queue");
        }

        internal readonly struct ClaimedItem {
            public string Path { get; }
            public TelemetryEnvelope Envelope { get; }

            public ClaimedItem(string path, TelemetryEnvelope envelope) {
                Path = path;
                Envelope = envelope;
            }
        }
    }
}
