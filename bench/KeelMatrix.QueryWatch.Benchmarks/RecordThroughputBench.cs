// Microbenchmarks for write-path contention in QueryWatchSession.
// Compares production "lock + copy-on-stop" vs an alternative "ConcurrentQueue + snapshot".
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

        private QueryWatchOptions _optsNoText = null!;

        [GlobalSetup]
        public void Setup() {
            _optsNoText = new QueryWatchOptions {
                CaptureSqlText = false // exercise the hot path without redaction overhead
            };
        }

        [Benchmark(Baseline = true, Description = "lock[List] + copy-on-stop (production)")]
        public QueryWatchReport LockList() {
            QueryWatchSession session = new(_optsNoText);
            RunWorkers(Threads, EventsPerThread, () => session.Record("SELECT 1", TimeSpan.FromMilliseconds(1)));
            return session.Stop();
        }

        [Benchmark(Description = "ConcurrentQueue + snapshot (alternative)")]
        public QueryWatchReport ConcurrentQueueSnapshot() {
            ConcurrentQueueSession session = new(_optsNoText);
            RunWorkers(Threads, EventsPerThread, () => session.Record("SELECT 1", TimeSpan.FromMilliseconds(1)));
            return session.Stop();
        }

        private static void RunWorkers(int threads, int eventsPerThread, Action action) {
            Task[] tasks = new Task[threads];
            for (int t = 0; t < threads; t++) {
                tasks[t] = Task.Run(() => {
                    for (int i = 0; i < eventsPerThread; i++) action();
                });
            }
            Task.WaitAll(tasks);
        }
    }

    // Minimal alternative session used only for benchmarking.
    internal sealed class ConcurrentQueueSession(QueryWatchOptions options) {
        private readonly ConcurrentQueue<QueryEvent> _q = new();
        private readonly QueryWatchOptions _options = options ?? throw new ArgumentNullException(nameof(options));
        private int _stopped; // 0=running,1=stopped
        private readonly DateTimeOffset _startedAt = DateTimeOffset.UtcNow;

        public void Record(string commandText, TimeSpan duration) {
            if (Volatile.Read(ref _stopped) != 0)
                throw new InvalidOperationException("Session has been stopped; cannot record new events.");

            // early-out if CaptureSqlText=false
            string text = string.Empty;
            if (_options.CaptureSqlText) {
                text = commandText ?? string.Empty;
                foreach (IQueryTextRedactor r in _options.Redactors) text = r.Redact(text);
            }

            // second check to minimize recording after stop (not strictly needed for the benchmark)
            if (Volatile.Read(ref _stopped) != 0)
                throw new InvalidOperationException("Session has been stopped; cannot record new events.");

            // FIX: use the public 3-arg ctor; the 4-arg (with meta) is internal
            QueryEvent ev = new(text, duration, DateTimeOffset.UtcNow);
            _q.Enqueue(ev);
        }

        public QueryWatchReport Stop() {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset? _stoppedAt = Interlocked.CompareExchange(ref _stopped, 1, 0) == 0
                ? (DateTimeOffset?)now
                : throw new InvalidOperationException("Session has already been stopped.");

            QueryEvent[] arr = [.. _q];
            List<QueryEvent> list = [.. arr];
            return QueryWatchReport.CreateSnapshot(list, _options, _startedAt, _stoppedAt ?? now);
        }
    }
}
