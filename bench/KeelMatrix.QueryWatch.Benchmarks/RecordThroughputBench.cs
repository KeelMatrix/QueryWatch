// Microbenchmarks for write-path contention in QueryWatchSession.
// Compares production "lock + copy-on-stop" vs an alternative "ConcurrentQueue + snapshot".
#nullable enable
using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;

namespace KeelMatrix.QueryWatch.Benchmarks {

    [MemoryDiagnoser]
#if DEBUG
    [SimpleJob(warmupCount: 1, iterationCount: 2)]
#else
    [SimpleJob] // default job in Release
#endif
    public class RecordThroughputBench {
        [Params(1, 2, 4, 8, 16)]
        public int Threads { get; set; } = 4;

        [Params(2_000)]
        public int EventsPerThread { get; set; } = 2_000;

        private KeelMatrix.QueryWatch.QueryWatchOptions _optsNoText = null!;

        [GlobalSetup]
        public void Setup() {
            _optsNoText = new KeelMatrix.QueryWatch.QueryWatchOptions {
                CaptureSqlText = false // exercise the hot path without redaction overhead
            };
        }

        [Benchmark(Baseline = true, Description = "lock[List] + copy-on-stop (production)")]
        public KeelMatrix.QueryWatch.QueryWatchReport LockList() {
            var session = new KeelMatrix.QueryWatch.QueryWatchSession(_optsNoText);
            RunWorkers(Threads, EventsPerThread, () => session.Record("SELECT 1", TimeSpan.FromMilliseconds(1)));
            return session.Stop();
        }

        [Benchmark(Description = "ConcurrentQueue + snapshot (alternative)")]
        public KeelMatrix.QueryWatch.QueryWatchReport ConcurrentQueueSnapshot() {
            var session = new ConcurrentQueueSession(_optsNoText);
            RunWorkers(Threads, EventsPerThread, () => session.Record("SELECT 1", TimeSpan.FromMilliseconds(1)));
            return session.Stop();
        }

        private static void RunWorkers(int threads, int eventsPerThread, Action action) {
            var tasks = new Task[threads];
            for (int t = 0; t < threads; t++) {
                tasks[t] = Task.Run(() => {
                    for (int i = 0; i < eventsPerThread; i++) action();
                });
            }
            Task.WaitAll(tasks);
        }
    }

    // Minimal alternative session used only for benchmarking.
    internal sealed class ConcurrentQueueSession {
        private readonly ConcurrentQueue<KeelMatrix.QueryWatch.QueryEvent> _q = new();
        private readonly KeelMatrix.QueryWatch.QueryWatchOptions _options;
        private int _stopped; // 0=running,1=stopped
        private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;
        private DateTimeOffset? _stoppedAt;

        public ConcurrentQueueSession(KeelMatrix.QueryWatch.QueryWatchOptions options) {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public void Record(string commandText, TimeSpan duration) {
            if (Volatile.Read(ref _stopped) != 0)
                throw new InvalidOperationException("Session has been stopped; cannot record new events.");

            // early-out if CaptureSqlText=false
            string text = string.Empty;
            if (_options.CaptureSqlText) {
                text = commandText ?? string.Empty;
                foreach (var r in _options.Redactors) text = r.Redact(text);
            }

            // second check to minimize recording after stop (not strictly needed for the benchmark)
            if (Volatile.Read(ref _stopped) != 0)
                throw new InvalidOperationException("Session has been stopped; cannot record new events.");

            // FIX: use the public 3-arg ctor; the 4-arg (with meta) is internal
            var ev = new KeelMatrix.QueryWatch.QueryEvent(text, duration, DateTimeOffset.UtcNow);
            _q.Enqueue(ev);
        }

        public KeelMatrix.QueryWatch.QueryWatchReport Stop() {
            var now = DateTimeOffset.UtcNow;
            if (Interlocked.CompareExchange(ref _stopped, 1, 0) == 0) {
                _stoppedAt = now;
            }
            else {
                throw new InvalidOperationException("Session has already been stopped.");
            }

            var arr = _q.ToArray();
            var list = new List<KeelMatrix.QueryWatch.QueryEvent>(arr);
            return KeelMatrix.QueryWatch.QueryWatchReport.CreateSnapshot(list, _options, _startedAt, _stoppedAt ?? now);
        }
    }
}
