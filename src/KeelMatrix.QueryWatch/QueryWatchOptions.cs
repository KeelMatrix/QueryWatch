// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Options for a monitoring session.
    /// </summary>
    public sealed class QueryWatchOptions {
        /// <summary>
        /// Whether to capture SQL/command text for events. Defaults to <c>true</c>.
        /// </summary>
        public bool CaptureSqlText { get; set; } = true;

        /// <summary>
        /// Redactors applied to captured SQL text in the order listed.
        /// </summary>
        public IList<IQueryTextRedactor> Redactors { get; } = [];

        /// <summary>
        /// Whether to capture only parameter shapes (names, DbType, size, direction), never values. Defaults to <c>true</c>.
        /// </summary>
        public bool CaptureParameterShape { get; set; } = true;
    }
}
