#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;

namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Collects query events for the lifetime of the session.
    /// Thread-safe for recording.
    /// </summary>
    public sealed class QueryWatchSession : IDisposable {
        private readonly List<QueryEvent> _events = new List<QueryEvent>();
        private readonly ReaderWriterLockSlim _gate = new ReaderWriterLockSlim();
        private bool _disposed;
        private int _stopped; // 0 = running, 1 = stopped

        public QueryWatchSession(QueryWatchOptions? options = null) {
            Options = options ?? new QueryWatchOptions();
            StartedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>Effective options for this session.</summary>
        public QueryWatchOptions Options { get; }

        /// <summary>Session start time (UTC).</summary>
        public DateTimeOffset StartedAt { get; }

        /// <summary>Session stop time when <see cref="Stop"/> was first called; otherwise null.</summary>
        public DateTimeOffset? StoppedAt { get; private set; }

        /// <summary>Start a new session with options.</summary>
        public static QueryWatchSession Start(QueryWatchOptions? options = null) => new QueryWatchSession(options);

        /// <summary>
        /// Record a single query execution (manual or from an adapter).
        /// </summary>
        public void Record(string commandText, TimeSpan duration) {
            Record(commandText, duration, meta: null);
        }

        /// <summary>
        /// Record a single query execution with optional metadata.
        /// This overload is used by adapters (ADO wrapper) to attach non-PII details,
        /// such as parameter names and types.
        /// </summary>
        public void Record(string commandText, TimeSpan duration, IReadOnlyDictionary<string, object?>? meta) {
            if (_disposed) throw new ObjectDisposedException(nameof(QueryWatchSession));

            // Fast path: if already stopped, throw as tests expect.
            if (Volatile.Read(ref _stopped) != 0)
                throw new InvalidOperationException("Session has been stopped; cannot record new events.");

            _gate.EnterWriteLock();
            try {
                if (_stopped != 0)
                    throw new InvalidOperationException("Session has been stopped; cannot record new events.");

                // Optionally redact or drop SQL text.
                string text = string.Empty;
                if (Options.CaptureSqlText) {
                    text = commandText ?? string.Empty;
                    foreach (var r in Options.Redactors) {
                        text = r.Redact(text);
                    }
                }

                var ev = new QueryEvent(text, duration, DateTimeOffset.UtcNow, meta);
                _events.Add(ev);
            }
            finally {
                _gate.ExitWriteLock();
            }
        }

        /// <summary>
        /// Stop the session and return an immutable snapshot report.
        /// Further calls must throw to honor lifecycle contracts in tests.
        /// </summary>
        public QueryWatchReport Stop() {
            if (_disposed) throw new ObjectDisposedException(nameof(QueryWatchSession));

            var now = DateTimeOffset.UtcNow;
            if (Interlocked.CompareExchange(ref _stopped, 1, 0) == 0) {
                StoppedAt = now;
            }
            else {
                // Enforce single-stop semantics
                throw new InvalidOperationException("Session has already been stopped.");
            }

            // Snapshot under read lock
            _gate.EnterReadLock();
            try {
                return QueryWatchReport.CreateSnapshot(_events, Options, StartedAt, StoppedAt ?? now);
            }
            finally {
                _gate.ExitReadLock();
            }
        }

        public void Dispose() {
            // Mark stopped and set StoppedAt once if not set.
            if (Interlocked.Exchange(ref _stopped, 1) == 0) {
                StoppedAt = DateTimeOffset.UtcNow;
            }

            _disposed = true;
            _gate.Dispose();
        }
    }
}
