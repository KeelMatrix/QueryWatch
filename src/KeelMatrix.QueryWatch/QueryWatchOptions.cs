#nullable enable
namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Options for a monitoring session.
    /// Kept intentionally small; extensible via future minor versions.
    /// </summary>
    public sealed class QueryWatchOptions {
        /// <summary>
        /// Maximum number of queries allowed before <see cref="QueryWatchReport.ThrowIfViolations"/> fails.
        /// Null means "no limit". Default: null.
        /// </summary>
        public int? MaxQueries { get; set; }

        /// <summary>
        /// Max average duration per query. Null means "no limit". Default: null.
        /// </summary>
        public System.TimeSpan? MaxAverageDuration { get; set; }

        /// <summary>
        /// Max total duration across all queries. Null means "no limit". Default: null.
        /// </summary>
        public System.TimeSpan? MaxTotalDuration { get; set; }

        /// <summary>
        /// Whether to capture SQL text. Default: true.
        /// </summary>
        public bool CaptureSqlText { get; set; } = true;

        /// <summary>
        /// A list of redactors to apply (in order) to SQL text before it is recorded.
        /// Provide implementations of <see cref="IQueryTextRedactor"/> to mask PII/secrets or noisy values.
        /// </summary>
        public IList<IQueryTextRedactor> Redactors { get; } = new List<IQueryTextRedactor>();

        /// <summary>
        /// Promote "parameter shape capture" to a top-level flag: when <c>true</c>, adapters MAY attach
        /// parameter <b>names/types/directions</b> as event metadata (never values).
        /// When enabled, the event JSON will include <c>meta.parameters</c> entries such as
        /// <c>{ name: "@id", dbType: "Int32", clrType: "System.Int32", direction: "Input" }</c>.
        /// Default: <c>false</c>.
        /// </summary>
        public bool CaptureParameterShape { get; set; } = false;

        /// <summary>
        /// Fast path to fully disable SQL text capture for the ADO.NET adapter (even when <see cref="CaptureSqlText"/> is true).
        /// Default: <c>false</c> (text capture enabled).
        /// </summary>
        public bool DisableAdoTextCapture { get; set; } = false;

        /// <summary>
        /// Fast path to fully disable SQL text capture for the Dapper adapter (even when <see cref="CaptureSqlText"/> is true).
        /// Default: <c>false</c> (text capture enabled).
        /// </summary>
        public bool DisableDapperTextCapture { get; set; } = false;

        /// <summary>
        /// Fast path to fully disable SQL text capture for the EF Core adapter (even when <see cref="CaptureSqlText"/> is true).
        /// Default: <c>false</c> (text capture enabled).
        /// </summary>
        public bool DisableEfCoreTextCapture { get; set; } = false;
    }
}
