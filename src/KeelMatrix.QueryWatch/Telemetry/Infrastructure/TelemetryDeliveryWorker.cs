// Copyright (c) KeelMatrix

using KeelMatrix.QueryWatch.Telemetry.ProjectIdentity;
using KeelMatrix.QueryWatch.Telemetry.Serialization;

namespace KeelMatrix.QueryWatch.Telemetry.Infrastructure {
    /// <summary>
    /// Single background worker responsible for telemetry planning (markers/events)
    /// and durable delivery (queue + HTTP).
    /// </summary>
    internal sealed class TelemetryDeliveryWorker : IDisposable {
        private static readonly TelemetryDeliveryWorker Instance = new();

        private readonly SemaphoreSlim signal = new(0, int.MaxValue);
        private readonly CancellationTokenSource cts = new();

        // Signals set from calling threads (must be non-blocking to set).
        private int activationRequested; // 0/1
        private int heartbeatRequested;  // 0/1

        // Worker state (only touched on worker thread)
        private TelemetryDispatcher? dispatcher;

        private int hasPendingWork; // 0 = false, 1 = true

        private readonly object backoffLock = new();
        private TimeSpan currentBackoff = TimeSpan.Zero;

        private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan InitialBackoff = TimeSpan.FromSeconds(1);

        private static readonly ThreadLocal<Random> JitterRandom =
            new(() => new Random(unchecked((Environment.TickCount * 31) + Environment.CurrentManagedThreadId)));

#pragma warning disable S4487 // Unread "private" fields should be removed
        private readonly Task _workerTask; // left for observing a potential exception during debug
#pragma warning restore S4487

        private TelemetryDeliveryWorker() {
            _workerTask = Task.Run(RunAsync);

            AppDomain.CurrentDomain.ProcessExit += (_, _) => Dispose();
#if NET8_0_OR_GREATER
            AppDomain.CurrentDomain.DomainUnload += (_, _) => Dispose();
#endif

            // Wake immediately to process backlog (disk queue) even without requests.
            Signal();
        }

        /// <summary>
        /// Ensures the singleton worker exists. Must not block.
        /// </summary>
        public static void EnsureStarted() {
            _ = Instance;
        }

        /// <summary>
        /// Requests activation emission. Must not block.
        /// </summary>
        public static void RequestActivation() {
            // Set flag; no I/O.
            Interlocked.Exchange(ref Instance.activationRequested, 1);
            Instance.Signal();
        }

        /// <summary>
        /// Requests heartbeat emission. Must not block.
        /// </summary>
        public static void RequestHeartbeat() {
            // Set flag; no I/O.
            Interlocked.Exchange(ref Instance.heartbeatRequested, 1);
            Instance.Signal();
        }

        /// <summary>
        /// Signals the worker to wake up. Must not block.
        /// </summary>
        private void Signal() {
            try {
                signal.Release();
            }
            catch {
                // swallow (SemaphoreFullException etc.)
            }
        }

        private async Task RunAsync() {
            var token = cts.Token;
            bool projectHashComputed = false;

            while (!token.IsCancellationRequested) {
                try {
                    await signal.WaitAsync(token).ConfigureAwait(false);
                }
                catch {
                    break;
                }

                // 0) Compute project identity/hash once on the worker thread (best-effort).
                if (!projectHashComputed && !TelemetryConfig.IsTelemetryDisabled()) {
                    try {
                        _ = ProjectIdentityProvider.Shared.EnsureComputedOnWorkerThread();
                    }
                    catch {
                        // swallow
                    }

                    projectHashComputed = true;
                }

                // 1) Plan & enqueue new telemetry based on requests (marker I/O happens here, not on caller)
                try {
                    ProcessRequestsOnWorkerThread();
                }
                catch {
                    // swallow; telemetry must never impact host
                }

                // 2) Deliver any queued items (including backlog from previous runs)
                if (TelemetryConfig.IsTelemetryDisabled()) {
                    // Policy: if disabled, do not send anything, including backlog.
                    // Also avoid touching the queue to prevent unintended I/O.
                    Interlocked.Exchange(ref hasPendingWork, 0);
                    ResetBackoff();
                    continue;
                }

                while (!token.IsCancellationRequested) {
                    bool anyAttempted = false;
                    bool anyFailed = false;

                    try {
                        foreach (var item in DurableTelemetryQueue.Instance.TryClaim(4)) {
                            anyAttempted = true;

                            try {
                                if (await TelemetryHttpSender.TrySendAsync(item.Envelope.PayloadJson, token).ConfigureAwait(false)) {
                                    DurableTelemetryQueue.Instance.Complete(item);
                                }
                                else {
                                    DurableTelemetryQueue.Instance.Abandon(item);
                                    anyFailed = true;
                                }
                            }
                            catch {
                                DurableTelemetryQueue.Instance.Abandon(item);
                                anyFailed = true;
                            }
                        }
                    }
                    catch { /* swallow */ }

                    if (!anyAttempted) {
                        Interlocked.Exchange(ref hasPendingWork, 0);
                        break;
                    }

                    if (anyFailed) {
                        await ApplyBackoff(token).ConfigureAwait(false);
                        Signal(); // ensure retry even without new enqueue
                        break;
                    }

                    ResetBackoff();
                }
            }
        }

        /// <summary>
        /// Runs only on the worker thread.
        /// Performs all I/O needed to decide whether to emit activation/heartbeat,
        /// serializes events, enqueues them durably, then commits marker files.
        /// </summary>
        private void ProcessRequestsOnWorkerThread() {
            // Fast exit if telemetry disabled (avoid all other work).
            if (TelemetryConfig.IsTelemetryDisabled())
                return;

            // Drain request flags first to avoid any compute when nothing was requested.
            var doActivation = Interlocked.Exchange(ref activationRequested, 0) == 1;
            var doHeartbeat = Interlocked.Exchange(ref heartbeatRequested, 0) == 1;

            if (!doActivation && !doHeartbeat)
                return;

            // Compute/lock project hash on the worker thread (cached for process lifetime).
            string projectHash;
            try {
                projectHash = ProjectIdentityProvider.Shared.EnsureComputedOnWorkerThread();
            }
            catch {
                projectHash = ProjectIdentityProvider.ComputeUninitializedPlaceholderHash();
            }

            // Create dispatcher/state on worker thread (marker I/O happens inside TelemetryState).
            dispatcher ??= new TelemetryDispatcher(projectHash);

            if (doActivation) {
                try {
                    var evt = dispatcher.TryCreateActivationEvent();
                    if (evt != null) {
                        var json = TelemetrySerializer.Serialize(evt);
                        if (json != null) {
                            // Durable queue write + marker commit are I/O; safe here.
                            DurableTelemetryQueue.Instance.Enqueue(json);
                            dispatcher.CommitActivation();
                            Interlocked.Exchange(ref hasPendingWork, 1);
                        }
                    }
                }
                catch {
                    // swallow
                }
            }

            if (doHeartbeat) {
                try {
                    var evt = dispatcher.TryCreateHeartbeatEvent();
                    if (evt != null) {
                        var json = TelemetrySerializer.Serialize(evt);
                        if (json != null) {
                            DurableTelemetryQueue.Instance.Enqueue(json);
                            dispatcher.CommitHeartbeat(evt.Week);
                            Interlocked.Exchange(ref hasPendingWork, 1);
                        }
                    }
                }
                catch {
                    // swallow
                }
            }

            // If we just enqueued something, ensure delivery runs promptly.
            if (Volatile.Read(ref hasPendingWork) == 1) {
                Signal();
            }
        }

        private async Task ApplyBackoff(CancellationToken token) {
            TimeSpan delay;

            lock (backoffLock) {
                currentBackoff = currentBackoff == TimeSpan.Zero
                    ? InitialBackoff
                    : TimeSpan.FromMilliseconds(
                        Math.Min(currentBackoff.TotalMilliseconds * 2, MaxBackoff.TotalMilliseconds));

                delay = currentBackoff;

                // Add a jitter to prevent multiple processes from hammering the endpoint at exactly the same time.
                Random rnd = JitterRandom.Value!;
                var jitter = TimeSpan.FromMilliseconds(rnd.Next(0, 300));
                delay += jitter;
            }

            try {
                await Task.Delay(delay, token).ConfigureAwait(false);
            }
            catch {
                // swallow
            }
        }

        private void ResetBackoff() {
            lock (backoffLock) {
                currentBackoff = TimeSpan.Zero;
            }
        }

        public void Dispose() {
            try {
                cts.Cancel();
                cts.Dispose();
                signal.Release();
            }
            catch (SemaphoreFullException) { /* swallow */ }
            catch { /* swallow */ }
        }
    }
}
