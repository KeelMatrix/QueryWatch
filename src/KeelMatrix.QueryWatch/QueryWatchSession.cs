// Copyright (c) KeelMatrix

using KeelMatrix.Redaction;

namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Collects query execution events for the lifetime of a session.
    /// Thread-safe for concurrent recording.
    /// </summary>
    /// <remarks>
    /// A session begins on construction and ends when <see cref="Complete"/> or <see cref="Dispose"/> is called.
    /// Use <see cref="Complete"/> to explicitly stop the session and retrieve a snapshot report.
    /// If <see cref="Dispose"/> is called first (for example via a <c>using</c> scope), the session
    /// is still stopped safely, but no report is returned.
    /// </remarks>
    public sealed class QueryWatchSession : IDisposable {
        private readonly List<QueryEvent> _events = [];
        private readonly object _sync = new();
        private bool _disposed;
        private int _stopped; // 0 = running, 1 = stopped
        private QueryWatchReport? _report;

        /// <summary>
        /// Starts a new query watch session.
        /// </summary>
        /// <param name="options">
        /// Optional configuration controlling capture behavior (for example text capture and redaction).
        /// If <c>null</c>, default options are used.
        /// </param>
        public QueryWatchSession(QueryWatchOptions? options = null) {
            Options = options ?? new QueryWatchOptions();
            StartedAt = DateTimeOffset.UtcNow;

            QueryWatchTelemetry.TrackActivation();
        }

        /// <summary>Options for this session.</summary>
        public QueryWatchOptions Options { get; }

        /// <summary>UTC timestamp when the session started.</summary>
        public DateTimeOffset StartedAt { get; }

        /// <summary>UTC timestamp when the session stopped, or <c>null</c> if still running.</summary>
        public DateTimeOffset? StoppedAt { get; private set; }

        /// <summary>
        /// Records a query execution event.
        /// </summary>
        /// <param name="commandText">The SQL command text (may be redacted depending on options).</param>
        /// <param name="duration">The execution duration.</param>
        /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The session has already been completed.</exception>
        public void Record(string commandText, TimeSpan duration) => Record(commandText, duration, meta: null);

        /// <summary>
        /// Records a query execution event with optional metadata.
        /// </summary>
        /// <param name="commandText">The SQL command text (may be redacted depending on options).</param>
        /// <param name="duration">The execution duration.</param>
        /// <param name="meta">Optional metadata associated with the event.</param>
        /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
        /// <exception cref="InvalidOperationException">The session has already been completed.</exception>
        internal void Record(string commandText, TimeSpan duration, IReadOnlyDictionary<string, object?>? meta) {
            if (_disposed) throw new ObjectDisposedException(nameof(QueryWatchSession));

            // Fast path: if already stopped, throw as tests expect.
            if (Volatile.Read(ref _stopped) != 0)
                throw new InvalidOperationException("Session has been stopped; cannot record new events.");

            lock (_sync) {
                if (_stopped != 0)
                    throw new InvalidOperationException("Session has been stopped; cannot record new events.");

                // Early-out: if CaptureSqlText=false, avoid any redactor passes and store empty string.
                string text = string.Empty;
                if (Options.CaptureSqlText) {
                    text = commandText ?? string.Empty;
                    foreach (ITextRedactor r in Options.Redactors) {
                        text = r.Redact(text);
                    }
                }

                QueryEvent ev = new(text, duration, DateTimeOffset.UtcNow, meta);
                _events.Add(ev);
            }
        }

        /// <summary>
        /// Completes the session and returns a snapshot report.
        /// </summary>
        /// <remarks>
        /// This is the intended way to end a session and retrieve its results.
        /// Calling this method stops further recording. Subsequent calls return the same cached report.
        /// </remarks>
        /// <returns>A snapshot report representing all recorded events.</returns>
        /// <exception cref="ObjectDisposedException">
        /// The session has already been disposed. Dispose indicates the session lifetime has ended
        /// without producing a report.
        /// </exception>
        public QueryWatchReport Complete() {
            if (_disposed)
                throw new ObjectDisposedException(nameof(QueryWatchSession));

            return _report ??= StopInternal();
        }

        /// <summary>
        /// Ends the session and releases resources.
        /// </summary>
        /// <remarks>
        /// If the session has not yet been completed, disposal will still stop the session safely,
        /// but no report will be returned. To retrieve a report, call <see cref="Complete"/> before disposing.
        /// </remarks>
        public void Dispose() {
            if (_disposed)
                return;

            if (_report is null) {
                // Ensure session is still stopped even if user forgot Complete()
                StopInternal();
            }

            _disposed = true;
        }

        private QueryWatchReport StopInternal() {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            if (now <= StartedAt) {
                now = StartedAt.AddTicks(1);
            }

            if (Interlocked.CompareExchange(ref _stopped, 1, 0) == 0) {
                StoppedAt = now;
                QueryWatchTelemetry.TrackHeartbeat();
            }

            List<QueryEvent> snapshot;
            lock (_sync) {
                snapshot = [.. _events];
            }

            return QueryWatchReport.CreateSnapshot(
                snapshot,
                Options,
                StartedAt,
                StoppedAt ?? now);
        }
    }
}
