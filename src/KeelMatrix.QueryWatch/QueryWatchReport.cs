#nullable enable
namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Immutable snapshot of a session used for assertions/inspection.
    /// </summary>
    public sealed class QueryWatchReport {
        private readonly IReadOnlyList<QueryEvent> _events;

        private QueryWatchReport(IReadOnlyList<QueryEvent> events,
                                 QueryWatchOptions options,
                                 DateTimeOffset startedAt,
                                 DateTimeOffset stoppedAt) {
            _events = events;
            Options = options;
            StartedAt = startedAt;
            StoppedAt = stoppedAt;
        }

        public QueryWatchOptions Options { get; }
        public DateTimeOffset StartedAt { get; }
        public DateTimeOffset StoppedAt { get; }
        public IReadOnlyList<QueryEvent> Events => _events;

        public int TotalQueries => _events.Count;
        public TimeSpan TotalDuration => TimeSpan.FromMilliseconds(_events.Sum(e => e.Duration.TotalMilliseconds));
        public TimeSpan AverageDuration => TotalQueries == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(_events.Average(e => e.Duration.TotalMilliseconds));

        public static QueryWatchReport CreateSnapshot(IReadOnlyList<QueryEvent> events, QueryWatchOptions options, DateTimeOffset startedAt, DateTimeOffset stoppedAt)
            => new QueryWatchReport(events.ToArray(), options, startedAt, stoppedAt);

        /// <summary>
        /// Throw <see cref="QueryWatchViolationException"/> if configured limits are exceeded.
        /// </summary>
        public void ThrowIfViolations() {
            var problems = new List<string>();

            if (Options.MaxQueries.HasValue && TotalQueries > Options.MaxQueries.Value)
                problems.Add($"MaxQueries={Options.MaxQueries.Value} but executed {TotalQueries}.");

            if (Options.MaxAverageDuration.HasValue && AverageDuration > Options.MaxAverageDuration.Value)
                problems.Add($"MaxAverageDuration={Options.MaxAverageDuration} but actual {AverageDuration}.");

            if (Options.MaxTotalDuration.HasValue && TotalDuration > Options.MaxTotalDuration.Value)
                problems.Add($"MaxTotalDuration={Options.MaxTotalDuration} but actual {TotalDuration}.");

            if (problems.Count > 0) {
                var header = "Summary: QueryWatch detected one or more performance/query budget violations.";
                var details = string.Join(" ", problems);
                var message = $"{header} {details}";
                throw new QueryWatchViolationException(message);
            }
        }

        /// <summary>
        /// Simple fluent-style helpers for common checks.
        /// </summary>
        public QueryWatchReport ShouldHaveExecutedAtMost(int maxQueries) {
            if (TotalQueries > maxQueries)
                throw new QueryWatchViolationException($"Expected ≤{maxQueries} queries, but executed {TotalQueries}.");
            return this;
        }

        public QueryWatchReport ShouldHaveMaxAverageTime(TimeSpan maxAverage) {
            if (AverageDuration > maxAverage)
                throw new QueryWatchViolationException($"Expected average ≤{maxAverage}, actual {AverageDuration}.");
            return this;
        }

        public QueryWatchReport ShouldHaveMaxTotalTime(TimeSpan maxTotal) {
            if (TotalDuration > maxTotal)
                throw new QueryWatchViolationException($"Expected total ≤{maxTotal}, actual {TotalDuration}.");
            return this;
        }
    }

    /// <summary>
    /// Thrown when QueryWatch detects configured or asserted violations.
    /// </summary>
    public sealed class QueryWatchViolationException : InvalidOperationException {
        public QueryWatchViolationException(string message) : base(message) { }
    }
}
