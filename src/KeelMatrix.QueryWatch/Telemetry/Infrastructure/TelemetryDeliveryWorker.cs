// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry.Infrastructure {
    /// <summary>
    /// Single background worker responsible for durable telemetry delivery.
    /// </summary>
    internal sealed class TelemetryDeliveryWorker : IDisposable {
        private static readonly TelemetryDeliveryWorker Instance = new();

        private readonly SemaphoreSlim signal = new(0, int.MaxValue);
        private readonly CancellationTokenSource cts = new();
        private volatile int hasPendingWork; // 0 = false, 1 = true

        private readonly object backoffLock = new();
        private TimeSpan currentBackoff = TimeSpan.Zero;

        private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan InitialBackoff = TimeSpan.FromSeconds(1);

        private static readonly ThreadLocal<Random> JitterRandom =
            new(() => new Random(unchecked((Environment.TickCount * 31) + Environment.CurrentManagedThreadId)));

        private TelemetryDeliveryWorker() {
            _ = Task.Run(RunAsync);

            AppDomain.CurrentDomain.ProcessExit += (_, _) => Dispose();
#if NET8_0_OR_GREATER
            AppDomain.CurrentDomain.DomainUnload += (_, _) => Dispose();
#endif

            // Wake immediately to process any backlog from previous crashes or runs.
            Signal();
        }

        public static void EnsureStarted() {
            _ = Instance;
        }

        public static void NotifyNewItem() {
            // Only signal if we transitioned from no-work -> work
            if (Interlocked.Exchange(ref Instance.hasPendingWork, 1) == 0) {
                Instance.Signal();
            }
        }

        private void Signal() {
            try {
                signal.Release();
            }
            catch {
                // swallow
            }
        }

        private async Task RunAsync() {
            var token = cts.Token;

            while (!token.IsCancellationRequested) {
                try {
                    await signal.WaitAsync(token).ConfigureAwait(false);
                }
                catch {
                    break;
                }

                while (!token.IsCancellationRequested) {
                    bool anyAttempted = false;
                    bool anyFailed = false;

                    foreach (var item in DurableTelemetryQueue.Instance.TryClaim(4)) {
                        anyAttempted = true;

                        try {
                            if (await TelemetryHttpSender.TrySendAsync(item.Envelope.PayloadJson, token)) {
                                DurableTelemetryQueue.Complete(item);
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
