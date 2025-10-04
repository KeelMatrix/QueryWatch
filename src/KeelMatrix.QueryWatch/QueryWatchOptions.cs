#nullable enable
namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Options for a monitoring session.
    /// </summary>
    public sealed class QueryWatchOptions {
        /// <summary>
        /// Maximum number of queries allowed (null = no limit).
        /// </summary>
        public int? MaxQueries { get; set; }

        /// <summary>
        /// Maximum average duration per query (null = no limit).
        /// </summary>
        public System.TimeSpan? MaxAverageDuration { get; set; }

        /// <summary>
        /// Maximum total duration across all queries (null = no limit).
        /// </summary>
        public System.TimeSpan? MaxTotalDuration { get; set; }

        /// <summary>
        /// Whether to capture SQL/command text for events. Defaults to <c>true</c>.
        /// </summary>
        public bool CaptureSqlText { get; set; } = true;

        /// <summary>
        /// Redactors applied to captured SQL text in the order listed.
        /// </summary>
        public IList<IQueryTextRedactor> Redactors { get; } = new List<IQueryTextRedactor>();

        /// <summary>
        /// Whether to capture only parameter shapes (types/directions), never values. Defaults to <c>true</c>.
        /// </summary>
        public bool CaptureParameterShape { get; set; }

        /// <summary>
        /// Fast path to entirely disable text capture for the ADO adapter (overrides <see cref="CaptureSqlText"/>). Default: <c>false</c>.
        /// </summary>
        public bool DisableAdoTextCapture { get; set; }

        /// <summary>
        /// Fast path to entirely disable text capture for the Dapper adapter (overrides <see cref="CaptureSqlText"/>). Default: <c>false</c>.
        /// </summary>
        public bool DisableDapperTextCapture { get; set; }

        /// <summary>
        /// Fast path to entirely disable text capture for the EF Core adapter (overrides <see cref="CaptureSqlText"/>). Default: <c>false</c>.
        /// </summary>
        public bool DisableEfCoreTextCapture { get; set; }
    }
}
