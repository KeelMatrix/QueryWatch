#nullable enable
namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Collects query events for the lifetime of a session. Thread‑safe for recording.
    /// </summary>
    public sealed class QueryWatchSession : IDisposable {
        private readonly List<QueryEvent> _events = new List<QueryEvent>();
        private readonly ReaderWriterLockSlim _gate = new ReaderWriterLockSlim();
        private bool _disposed;
        private int _stopped; // 0 = running, 1 = stopped

        /// <summary>
        /// Initializes a new session.
        /// </summary>
        /// <param name="options">Optional session options.</param>
        public QueryWatchSession(QueryWatchOptions? options = null) {
            Options = options ?? new QueryWatchOptions();
            StartedAt = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Options for this session.
        /// </summary>
        public QueryWatchOptions Options { get; }

        /// <summary>
        /// UTC timestamp when the session started.
        /// </summary>
        public DateTimeOffset StartedAt { get; }

        /// <summary>
        /// UTC timestamp when the session stopped, or <c>null</c> if still running.
        /// </summary>
        public DateTimeOffset? StoppedAt { get; private set; }

        /// <summary>
        /// Starts a new session.
        /// </summary>
        /// <param name="options">Optional session options.</param>
        /// <returns>The started session.</returns>
        public static QueryWatchSession Start(QueryWatchOptions? options = null) => new QueryWatchSession(options);

        /// <summary>
        /// Records a query execution event.
        /// </summary>
        /// <param name="commandText">Executed SQL or provider command text.</param>
        /// <param name="duration">Execution duration.</param>
        public void Record(string commandText, TimeSpan duration) {
            Record(commandText, duration, meta: null);
        }

        /// <summary>
        /// Records a query execution event with optional metadata.
        /// </summary>
        /// <param name="commandText">Executed SQL or provider command text.</param>
        /// <param name="duration">Execution duration.</param>
        /// <param name="meta">Optional metadata bag for provider‑specific details.</param>
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
        /// Stops the session and returns a snapshot report.
        /// </summary>
        /// <returns>A report representing the recorded data.</returns>
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

        /// <summary>
        /// Disposes session resources and marks it as stopped.
        /// </summary>
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
