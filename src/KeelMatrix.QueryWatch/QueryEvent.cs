// Copyright (c) KeelMatrix
#nullable enable
namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// A single observed database command execution.
    /// </summary>
    public sealed class QueryEvent {
        /// <summary>
        /// Initializes a new <see cref="QueryEvent"/>.
        /// </summary>
        /// <param name="commandText">Executed SQL or provider command text. Can be empty when text capture is disabled.</param>
        /// <param name="duration">Execution duration.</param>
        /// <param name="at">UTC timestamp when the command finished.</param>
        public QueryEvent(string commandText, System.TimeSpan duration, System.DateTimeOffset at) {
            CommandText = commandText;
            Duration = duration;
            At = at;
        }

        /// <summary>
        /// Internal constructor that allows attaching optional metadata (e.g., ADO parameter shapes).
        /// </summary>
        /// <param name="commandText">Executed SQL or provider command text. Can be empty when text capture is disabled.</param>
        /// <param name="duration">Execution duration.</param>
        /// <param name="at">UTC timestamp when the command finished.</param>
        /// <param name="meta">Metadata.</param>
        internal QueryEvent(string commandText, System.TimeSpan duration, System.DateTimeOffset at, IReadOnlyDictionary<string, object?>? meta) {
            CommandText = commandText;
            Duration = duration;
            At = at;
            Meta = meta;
        }

        /// <summary>
        /// SQL or provider command text (may be empty when text capture is disabled).
        /// </summary>
        public string CommandText { get; }

        /// <summary>
        /// Execution duration.
        /// </summary>
        public System.TimeSpan Duration { get; }

        /// <summary>
        /// UTC timestamp when the command finished.
        /// </summary>
        public System.DateTimeOffset At { get; }

        /// <summary>
        /// Optional metadata with additive, non‑breaking details about the event (e.g., parameter shapes).
        /// </summary>
        public IReadOnlyDictionary<string, object?>? Meta { get; }
    }
}
