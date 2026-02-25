// Copyright (c) KeelMatrix

using System.Text;
using KeelMatrix.QueryWatch.Telemetry.Storage;

namespace KeelMatrix.QueryWatch.Telemetry.Infrastructure {
    /// <summary>
    /// Filesystem-backed durable queue using one JSON file per entry.
    /// Safe across crashes and multiple processes.
    /// </summary>
    internal sealed class DurableTelemetryQueue : ITelemetryQueue {
        public static ITelemetryQueue Instance { get; } = CreateSafe();

        private readonly string pendingDir;
        private readonly string processingDir;
        private readonly string deadLetterDir;
        private static ITelemetryQueue CreateSafe() {
            try { return new DurableTelemetryQueue(); }
            catch { return new NullTelemetryQueue(); }
        }

        private DurableTelemetryQueue() {
            string root = ResolveQueueRoot();
            pendingDir = Path.Combine(root, "pending");
            processingDir = Path.Combine(root, "processing");
            deadLetterDir = Path.Combine(root, "dead");

            Directory.CreateDirectory(pendingDir);
            Directory.CreateDirectory(processingDir);
            Directory.CreateDirectory(deadLetterDir);

            CleanupTmpFiles(pendingDir);
            CleanupTmpFiles(processingDir);
            CleanupTmpFiles(deadLetterDir);

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
            var nowUtc = DateTime.UtcNow;

            foreach (var file in Directory.EnumerateFiles(processingDir, "*.json")) {
                try {
                    DateTime lastWriteUtc;
                    try {
                        lastWriteUtc = File.GetLastWriteTimeUtc(file);
                    }
                    catch {
                        // If we can't read the timestamp, we can't safely decide it's stale.
                        continue;
                    }

                    // Defensive: LastWriteTimeUtc can sometimes be default/invalid or in the future due to clock skew.
                    if (lastWriteUtc == DateTime.MinValue || lastWriteUtc > nowUtc)
                        continue;

                    var age = nowUtc - lastWriteUtc;
                    if (age < TelemetryConfig.ProcessingStaleThreshold)
                        continue;

                    var target = Path.Combine(pendingDir, Path.GetFileName(file));
                    try { File.Delete(target); } catch { /* file may not exist; ignore */ }
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
                        try { File.SetLastWriteTimeUtc(claimedPath, DateTime.UtcNow); }
                        catch { /* swallow */ }
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
        public void Complete(ClaimedItem item) {
            SafeDelete(item.Path);
        }

        /// <summary>
        /// Returns a failed item back to pending.
        /// Ensures we only delete the processing item after the updated entry is safely persisted,
        /// or after we successfully moved it to dead-letter.
        /// </summary>
        public void Abandon(ClaimedItem item) {
            try {
                var env = item.Envelope;

                // If max attempts reached, try to dead-letter; do not delete unless move succeeds.
                if (env.Attempts + 1 >= TelemetryConfig.MaxSendAttempts) {
                    MoveToDeadLetterBestEffort(item.Path);
                    return;
                }

                var updated = new TelemetryEnvelope(
                    env.Id,
                    env.PayloadJson,
                    env.EnqueuedUtc
                ) {
                    Attempts = env.Attempts + 1
                };

                var target = Path.Combine(pendingDir, Path.GetFileName(item.Path));

                // Persist updated envelope into pending atomically-ish:
                // write temp in pending dir, then move into place.
                // Only after this succeeds do we delete the processing file.
                if (!DurableTelemetryQueue.TryWritePendingAtomically(target, updated.Serialize())) {
                    // Requeue failed; keep processing file so it can be retried later
                    // (CrashRecovery will move it back to pending on next start).
                    return;
                }

                // Requeue succeeded; now safe to delete the processing item.
                SafeDelete(item.Path);
            }
            catch {
                // swallow
            }
        }

        /// <summary>
        /// Attempts to write a pending item using tmp + move semantics.
        /// Returns true only if the final file is known to exist with the written content.
        /// </summary>
        private static bool TryWritePendingAtomically(string target, string content) {
            string tmp = target + ".tmp";

            try {
                File.WriteAllText(tmp, content);

#if NET8_0_OR_GREATER
                // On modern runtimes, overwrite is supported directly.
                File.Move(tmp, target, overwrite: true);
                return true;
#else
        // netstandard2.0: emulate overwrite safely.
        // Important: we must not claim success unless the final file exists.
        try {
            if (File.Exists(target))
                File.Delete(target);

            File.Move(tmp, target);
            return File.Exists(target);
        }
        catch {
            // If move fails, do not delete processing file; just cleanup tmp best-effort.
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

        /// <summary>
        /// Moves a processing item to dead-letter.
        /// Never deletes the processing file unless the move succeeded.
        /// </summary>
        private void MoveToDeadLetterBestEffort(string processingPath) {
            try {
                var target = Path.Combine(deadLetterDir, Path.GetFileName(processingPath));
#if NET8_0_OR_GREATER
                File.Move(processingPath, target, overwrite: true);
#else
        // netstandard2.0: best-effort overwrite emulation
        try {
            if (File.Exists(target))
                File.Delete(target);

            File.Move(processingPath, target);
        }
        catch {
            // If we can't move to dead-letter, leave the processing file in place.
            // (CrashRecovery will return it to pending on next start.)
        }
#endif
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
                EnforceLimitOnDirectory(pendingDir, TelemetryConfig.MaxPendingItems);
                EnforceLimitOnDirectory(deadLetterDir, TelemetryConfig.MaxDeadLetterItems);
            }
            catch {
                // swallow
            }
        }

        private static void EnforceLimitOnDirectory(string dir, int maxQueueItems) {
            try {
                var files = Directory.EnumerateFiles(dir, "*.json")
                                     .OrderBy(File.GetCreationTimeUtc)
                                     .ToList();

                var excess = files.Count - maxQueueItems;
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
