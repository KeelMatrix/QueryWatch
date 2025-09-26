// Copyright (c) KeelMatrix
#nullable enable
using System.Collections.Generic;

namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// A single observed database command execution.
    /// High level skeleton only — the structure may evolve before 1.0.
    /// </summary>
    public sealed class QueryEvent {
        /// <summary>
        /// Create an event with no extra metadata.
        /// </summary>
        public QueryEvent(string commandText, System.TimeSpan duration, System.DateTimeOffset at) {
            CommandText = commandText;
            Duration = duration;
            At = at;
        }

        /// <summary>
        /// Internal constructor that allows attaching optional metadata (e.g., ADO parameter shapes).
        /// </summary>
        internal QueryEvent(string commandText, System.TimeSpan duration, System.DateTimeOffset at, IReadOnlyDictionary<string, object?>? meta) {
            CommandText = commandText;
            Duration = duration;
            At = at;
            Meta = meta;
        }

        /// <summary>SQL or provider-specific textual representation (trimmed / may be redacted).</summary>
        public string CommandText { get; }

        /// <summary>Total time spent executing the command.</summary>
        public System.TimeSpan Duration { get; }

        /// <summary>Timestamp when the command finished.</summary>
        public System.DateTimeOffset At { get; }

        /// <summary>
        /// Optional metadata bag with additive, non-breaking details about the event.
        /// For ADO capture policy this may include a <c>parameters</c> array with items
        /// containing <c>name</c>, <c>dbType</c>, <c>clrType</c> and <c>direction</c>.
        /// </summary>
        public IReadOnlyDictionary<string, object?>? Meta { get; }
    }
}
