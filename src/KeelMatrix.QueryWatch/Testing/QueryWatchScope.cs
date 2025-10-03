#nullable enable
using System;
using KeelMatrix.QueryWatch.Reporting;

namespace KeelMatrix.QueryWatch.Testing {
    /// <summary>
    /// Disposable scope for tests: starts a <see cref="QueryWatchSession"/>,
    /// and on dispose stops it, optionally exports JSON, and enforces thresholds.
    /// </summary>
    public sealed class QueryWatchScope : IDisposable {
        private bool _disposed;

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

        /// <summary>The underlying session (wire EF Core or ADO wrappers to this).</summary>
        public QueryWatchSession Session { get; }

        /// <summary>Optional cap on total number of queries.</summary>
        public int? MaxQueries { get; private set; }

        /// <summary>Optional cap on average query duration.</summary>
        public TimeSpan? MaxAverage { get; private set; }

        /// <summary>Optional cap on total duration of all queries.</summary>
        public TimeSpan? MaxTotal { get; private set; }

        /// <summary>If provided, a JSON summary is written on dispose.</summary>
        public string? ExportJsonPath { get; private set; }

        /// <summary>How many top events (by duration) to include in the JSON.</summary>
        public int SampleTop { get; private set; }

        /// <summary>
        /// Create and start a new test scope.
        /// </summary>
        public static QueryWatchScope Start(
            int? maxQueries = null,
            TimeSpan? maxAverage = null,
            TimeSpan? maxTotal = null,
            QueryWatchOptions? options = null,
            string? exportJsonPath = null,
            int sampleTop = 5) {
            var session = QueryWatcher.Start(options);
            return new QueryWatchScope(session, maxQueries, maxAverage, maxTotal, exportJsonPath, sampleTop);
        }

        // note: REMOVE LATER. We export JSON BEFORE asserting budgets to make sure
        // we always have a file for CI analysis even if assertions throw.
        public void Dispose() {
            if (_disposed) return;
            _disposed = true;

            var report = Session.Stop();

            if (!string.IsNullOrWhiteSpace(ExportJsonPath)) {
                try { QueryWatchJson.ExportToFile(report, ExportJsonPath!, SampleTop); }
                catch { /* swallow export errors to avoid masking the real test failure */ }
            }

            if (MaxQueries.HasValue) report.ShouldHaveExecutedAtMost(MaxQueries.Value);
            if (MaxAverage.HasValue) report.ShouldHaveMaxAverageTime(MaxAverage.Value);
            if (MaxTotal.HasValue) report.ShouldHaveMaxTotalTime(MaxTotal.Value);
        }
    }
}
