// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Immutable snapshot of a session used for assertions and inspection.
    /// </summary>
    public sealed class QueryWatchReport {
        private QueryWatchReport(IReadOnlyList<QueryEvent> events,
                                 QueryWatchOptions options,
                                 DateTimeOffset startedAt,
                                 DateTimeOffset stoppedAt) {
            Events = events;
            Options = options;
            StartedAt = startedAt;
            StoppedAt = stoppedAt;
        }

        /// <summary>
        /// Options used by the originating session.
        /// </summary>
        public QueryWatchOptions Options { get; }

        /// <summary>
        /// UTC timestamp when the session started.
        /// </summary>
        public DateTimeOffset StartedAt { get; }

        /// <summary>
        /// UTC timestamp when the session stopped.
        /// </summary>
        public DateTimeOffset StoppedAt { get; }

        /// <summary>
        /// Recorded query events in chronological order.
        /// </summary>
        public IReadOnlyList<QueryEvent> Events { get; }

        /// <summary>
        /// Number of recorded queries.
        /// </summary>
        public int TotalQueries => Events.Count;

        /// <summary>
        /// Total duration across all recorded queries.
        /// </summary>
        public TimeSpan TotalDuration => TimeSpan.FromMilliseconds(Events.Sum(e => e.Duration.TotalMilliseconds));

        /// <summary>
        /// Average duration per query across all recorded queries.
        /// </summary>
        public TimeSpan AverageDuration => TotalQueries == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(Events.Average(e => e.Duration.TotalMilliseconds));

        /// <summary>
        /// Creates a snapshot report from raw events and timing.
        /// </summary>
        /// <param name="events">Events to include.</param>
        /// <param name="options">Options in effect during capture.</param>
        /// <param name="startedAt">Session start (UTC).</param>
        /// <param name="stoppedAt">Session stop (UTC).</param>
        /// <returns>A new report.</returns>
        internal static QueryWatchReport CreateSnapshot(IReadOnlyList<QueryEvent> events, QueryWatchOptions options, DateTimeOffset startedAt, DateTimeOffset stoppedAt)
            => new([.. events], options, startedAt, stoppedAt);
    }

    /// <summary>
    /// Thrown when QueryWatch detects configured or asserted violations.
    /// </summary>
    public sealed class QueryWatchViolationException : InvalidOperationException {
        /// <summary>
        /// Initializes a new <see cref="QueryWatchViolationException"/>.
        /// </summary>
        public QueryWatchViolationException() : base() { }

        /// <summary>
        /// Initializes a new <see cref="QueryWatchViolationException"/>.
        /// </summary>
        /// <param name="message">The violation message.</param>
        public QueryWatchViolationException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new <see cref="QueryWatchViolationException"/>.
        /// </summary>
        /// <param name="message">The violation message.</param>
        /// <param name="innerException">The inner exception.</param>
        public QueryWatchViolationException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
