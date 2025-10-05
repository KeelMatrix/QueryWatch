// Copyright (c) KeelMatrix
#nullable enable
using System;
using System.Collections.Generic;
using System.Threading;

namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Collects query events for the lifetime of a session. Thread‑safe for recording.
    /// This version uses a simple <c>lock</c> (monitor) for minimal overhead on write‑heavy workloads,
    /// and snapshots the list on <see cref="Stop"/>.
    /// </summary>
    public sealed class QueryWatchSession : IDisposable {
        private readonly List<QueryEvent> _events = new List<QueryEvent>();
        private readonly object _sync = new object();
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

        /// <summary>Options for this session.</summary>
        public QueryWatchOptions Options { get; }

        /// <summary>UTC timestamp when the session started.</summary>
        public DateTimeOffset StartedAt { get; }

        /// <summary>UTC timestamp when the session stopped, or <c>null</c> if still running.</summary>
        public DateTimeOffset? StoppedAt { get; private set; }

        /// <summary>Starts a new session.</summary>
        public static QueryWatchSession Start(QueryWatchOptions? options = null) => new QueryWatchSession(options);

        /// <summary>Records a query execution event.</summary>
        public void Record(string commandText, TimeSpan duration) => Record(commandText, duration, meta: null);

        /// <summary>Records a query execution event with optional metadata.</summary>
        public void Record(string commandText, TimeSpan duration, IReadOnlyDictionary<string, object?>? meta) {
            if (_disposed) throw new ObjectDisposedException(nameof(QueryWatchSession));

            // Fast path: if already stopped, throw as tests expect.
            if (Volatile.Read(ref _stopped) != 0)
                throw new InvalidOperationException("Session has been stopped; cannot record new events.");

            lock (_sync) {
                if (_stopped != 0)
                    throw new InvalidOperationException("Session has been stopped; cannot record new events.");

                // Early‑out: if CaptureSqlText=false, avoid any redactor passes and store empty string.
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
        }

        /// <summary>Stops the session and returns a snapshot report.</summary>
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

            // Snapshot under the same lock that protects writes to maintain no-post-stop recording guarantee.
            List<QueryEvent> snapshot;
            lock (_sync) {
                snapshot = new List<QueryEvent>(_events);
            }

            return QueryWatchReport.CreateSnapshot(snapshot, Options, StartedAt, StoppedAt ?? now);
        }

        /// <summary>Disposes session resources and marks it as stopped.</summary>
        public void Dispose() {
            // Mark stopped and set StoppedAt once if not set.
            if (Interlocked.Exchange(ref _stopped, 1) == 0) {
                StoppedAt = DateTimeOffset.UtcNow;
            }
            _disposed = true;
        }
    }
}
