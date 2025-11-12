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
        public static QueryWatchReport CreateSnapshot(IReadOnlyList<QueryEvent> events, QueryWatchOptions options, DateTimeOffset startedAt, DateTimeOffset stoppedAt)
            => new([.. events], options, startedAt, stoppedAt);

        /// <summary>
        /// Validates configured thresholds and throws on violations.
        /// </summary>
        /// <exception cref="QueryWatchViolationException">
        /// Thrown when <see cref="TotalQueries"/>, <see cref="AverageDuration"/>, or <see cref="TotalDuration"/> exceed configured limits.
        /// </exception>
        public void ThrowIfViolations() {
            List<string> problems = [];

            if (Options.MaxQueries.HasValue && TotalQueries > Options.MaxQueries.Value)
                problems.Add($"MaxQueries={Options.MaxQueries.Value} but executed {TotalQueries}.");

            if (Options.MaxAverageDuration.HasValue && AverageDuration > Options.MaxAverageDuration.Value)
                problems.Add($"MaxAverageDuration={Options.MaxAverageDuration} but actual {AverageDuration}.");

            if (Options.MaxTotalDuration.HasValue && TotalDuration > Options.MaxTotalDuration.Value)
                problems.Add($"MaxTotalDuration={Options.MaxTotalDuration} but actual {TotalDuration}.");

            if (problems.Count > 0) {
                const string header = "Summary: QueryWatch detected one or more performance/query budget violations.";
                string details = string.Join(" ", problems);
                string message = $"{header} {details}";
                throw new QueryWatchViolationException(message);
            }
        }

        /// <summary>
        /// Asserts that at most <paramref name="maxQueries"/> were executed.
        /// </summary>
        /// <param name="maxQueries">Maximum allowed queries.</param>
        /// <returns>The same report for chaining.</returns>
        /// <exception cref="QueryWatchViolationException">Thrown when the assertion fails.</exception>
        public QueryWatchReport ShouldHaveExecutedAtMost(int maxQueries) {
            return TotalQueries > maxQueries
                ? throw new QueryWatchViolationException($"Expected ≤{maxQueries} queries, but executed {TotalQueries}.")
                : this;
        }

        /// <summary>
        /// Asserts that the average query time does not exceed <paramref name="maxAverage"/>.
        /// </summary>
        /// <param name="maxAverage">Maximum allowed average time.</param>
        /// <returns>The same report for chaining.</returns>
        /// <exception cref="QueryWatchViolationException">Thrown when the assertion fails.</exception>
        public QueryWatchReport ShouldHaveMaxAverageTime(TimeSpan maxAverage) {
            return AverageDuration > maxAverage
                ? throw new QueryWatchViolationException($"Expected average ≤{maxAverage}, actual {AverageDuration}.")
                : this;
        }

        /// <summary>
        /// Asserts that the total query time does not exceed <paramref name="maxTotal"/>.
        /// </summary>
        /// <param name="maxTotal">Maximum allowed total time.</param>
        /// <returns>The same report for chaining.</returns>
        /// <exception cref="QueryWatchViolationException">Thrown when the assertion fails.</exception>
        public QueryWatchReport ShouldHaveMaxTotalTime(TimeSpan maxTotal) {
            return TotalDuration > maxTotal
                ? throw new QueryWatchViolationException($"Expected total ≤{maxTotal}, actual {TotalDuration}.")
                : this;
        }
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
