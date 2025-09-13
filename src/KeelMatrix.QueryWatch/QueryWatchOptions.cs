#nullable enable
using System;
using System.Collections.Generic;

namespace KeelMatrix.QueryWatch
{
    /// <summary>
    /// Options for a monitoring session.
    /// Keep intentionally small; extensible via future minor versions.
    /// </summary>
    public sealed class QueryWatchOptions
    {
        /// <summary>
        /// Maximum number of queries allowed before <see cref="QueryWatchReport.ThrowIfViolations"/> fails.
        /// Null means "no limit". Default: null.
        /// </summary>
        public int? MaxQueries { get; set; }

        /// <summary>
        /// Max average duration per query. Null means "no limit". Default: null.
        /// </summary>
        public TimeSpan? MaxAverageDuration { get; set; }

        /// <summary>
        /// Max total duration across all queries. Null means "no limit". Default: null.
        /// </summary>
        public TimeSpan? MaxTotalDuration { get; set; }

        /// <summary>
        /// Whether to capture SQL text. Default: true.
        /// </summary>
        public bool CaptureSqlText { get; set; } = true;

        /// <summary>
        /// A list of redactors to apply (in order) to SQL text before it is recorded.
        /// Provide implementations of <see cref="IQueryTextRedactor"/> to mask PII/secrets or noisy values.
        /// </summary>
        public IList<IQueryTextRedactor> Redactors { get; } = new List<IQueryTextRedactor>();
    }
}
