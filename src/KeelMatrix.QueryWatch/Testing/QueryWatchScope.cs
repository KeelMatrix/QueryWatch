using KeelMatrix.QueryWatch.Reporting;

namespace KeelMatrix.QueryWatch.Testing {
    /// <summary>
    /// Disposable scope for tests: starts a <see cref="QueryWatchSession"/>, and on dispose stops it, optionally exports JSON, and enforces thresholds.
    /// </summary>
    public sealed class QueryWatchScope : IDisposable {
        private bool _disposed;

        /// <summary>
        /// Initializes a new test scope.
        /// </summary>
        /// <param name="session">The session to manage.</param>
        /// <param name="maxQueries">Optional maximum number of queries.</param>
        /// <param name="maxAverage">Optional maximum average query time.</param>
        /// <param name="maxTotal">Optional maximum total query time.</param>
        /// <param name="exportJsonPath">Optional file path for exporting JSON before assertions.</param>
        /// <param name="sampleTop">Number of top events to include in the JSON sample. Default: 5.</param>
        public QueryWatchScope(QueryWatchSession session,
                               int? maxQueries = null,
                               TimeSpan? maxAverage = null,
                               TimeSpan? maxTotal = null,
                               string? exportJsonPath = null,
                               int sampleTop = 5) {
            Session = session ?? throw new ArgumentNullException(nameof(session));
            MaxQueries = maxQueries;
            MaxAverage = maxAverage;
            MaxTotal = maxTotal;
            ExportJsonPath = exportJsonPath;
            SampleTop = sampleTop;
        }

        /// <summary>
        /// The managed session.
        /// </summary>
        public QueryWatchSession Session { get; }

        /// <summary>
        /// Maximum number of queries to assert, if any.
        /// </summary>
        public int? MaxQueries { get; private set; }

        /// <summary>
        /// Maximum average query time to assert, if any.
        /// </summary>
        public TimeSpan? MaxAverage { get; private set; }

        /// <summary>
        /// Maximum total query time to assert, if any.
        /// </summary>
        public TimeSpan? MaxTotal { get; private set; }

        /// <summary>
        /// Optional file path for exporting JSON before assertions.
        /// </summary>
        public string? ExportJsonPath { get; }

        /// <summary>
        /// Number of top events to include in the JSON sample.
        /// </summary>
        public int SampleTop { get; }

        /// <summary>
        /// Starts a new scope with a new session.
        /// </summary>
        /// <param name="maxQueries">Optional maximum number of queries.</param>
        /// <param name="maxAverage">Optional maximum average query time.</param>
        /// <param name="maxTotal">Optional maximum total query time.</param>
        /// <param name="options">Optional session options.</param>
        /// <param name="exportJsonPath">Optional export path.</param>
        /// <param name="sampleTop">Number of top events to include in the JSON sample. Default: 5.</param>
        /// <returns>A disposable test scope.</returns>
        public static QueryWatchScope Start(
            int? maxQueries = null,
            TimeSpan? maxAverage = null,
            TimeSpan? maxTotal = null,
            QueryWatchOptions? options = null,
            string? exportJsonPath = null,
            int sampleTop = 5) {
            QueryWatchSession session = QueryWatcher.Start(options);
            return new QueryWatchScope(session, maxQueries, maxAverage, maxTotal, exportJsonPath, sampleTop);
        }

        /// <summary>
        /// Stops the session, exports JSON if requested, and enforces thresholds.
        /// </summary>
        public void Dispose() {
            if (_disposed) return;
            _disposed = true;

            QueryWatchReport report = Session.Stop();

            if (!string.IsNullOrWhiteSpace(ExportJsonPath)) {
                try { QueryWatchJson.ExportToFile(report, ExportJsonPath!, SampleTop); }
                catch { /* swallow export errors to avoid masking the real test failure */ }
            }

            if (MaxQueries.HasValue) _ = report.ShouldHaveExecutedAtMost(MaxQueries.Value);
            if (MaxAverage.HasValue) _ = report.ShouldHaveMaxAverageTime(MaxAverage.Value);
            if (MaxTotal.HasValue) _ = report.ShouldHaveMaxTotalTime(MaxTotal.Value);
        }
    }
}
