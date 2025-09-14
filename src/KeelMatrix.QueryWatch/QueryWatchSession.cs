#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;

namespace KeelMatrix.QueryWatch
{
    /// <summary>
    /// Collects query events for the lifetime of the session.
    /// Thread-safe for recording.
    /// </summary>
    public sealed class QueryWatchSession : IDisposable
    {
        private readonly List<QueryEvent> _events = new List<QueryEvent>();
        private readonly ReaderWriterLockSlim _gate = new ReaderWriterLockSlim();
        private bool _disposed;
        private int _stopped; // 0 = running, 1 = stopped

        public QueryWatchSession(QueryWatchOptions? options = null)
        {
            Options = options ?? new QueryWatchOptions();
            StartedAt = DateTimeOffset.UtcNow;
        }

        public QueryWatchOptions Options { get; }
        public DateTimeOffset StartedAt { get; }
        public DateTimeOffset? StoppedAt { get; private set; }

        /// <summary>Start a new session with options.</summary>
        public static QueryWatchSession Start(QueryWatchOptions? options = null) => new QueryWatchSession(options);

        /// <summary>Record a single query execution (manual or from an adapter).</summary>
        public void Record(string commandText, TimeSpan duration)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(QueryWatchSession));
            if (Volatile.Read(ref _stopped) == 1) throw new InvalidOperationException("This QueryWatch session has been stopped.");

            string text = string.Empty;
            if (Options.CaptureSqlText)
            {
                text = commandText ?? string.Empty;

                // REMOVE LATER. We apply redactors in-order to the captured SQL text.
                // This keeps the core neutral while enabling users/tests to mask PII,
                // dynamic values, or noisy literals that would otherwise cause churn.
                var redactors = Options.Redactors;
                if (redactors is not null)
                {
                    for (int i = 0; i < redactors.Count; i++)
                    {
                        text = redactors[i]?.Redact(text) ?? text;
                    }
                }
            }

            var ev = new QueryEvent(
                commandText: text,
                duration: duration,
                at: DateTimeOffset.UtcNow);

            _gate.EnterWriteLock();
            try { _events.Add(ev); }
            finally { _gate.ExitWriteLock(); }
        }

        /// <summary>Stop the session and get a report snapshot.</summary>
        public QueryWatchReport Stop()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(QueryWatchSession));

            // Ensure Stop is idempotent in the "throw on second call" sense.
            if (Interlocked.Exchange(ref _stopped, 1) == 1)
                throw new InvalidOperationException("This QueryWatch session is already stopped.");

            StoppedAt = DateTimeOffset.UtcNow;

            _gate.EnterReadLock();
            try
            {
                // Copy defensively
                return QueryWatchReport.CreateSnapshot(_events, Options, StartedAt, StoppedAt.Value);
            }
            finally { _gate.ExitReadLock(); }
        }

        public void Dispose()
        {
            // Mark stopped and set StoppedAt once if not set.
            if (Interlocked.Exchange(ref _stopped, 1) == 0)
            {
                StoppedAt = DateTimeOffset.UtcNow;
            }

            _disposed = true;
            _gate.Dispose();
        }
    }
}
