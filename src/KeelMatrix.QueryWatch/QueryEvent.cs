// Copyright (c) KeelMatrix
#nullable enable
namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// A single observed database command execution.
    /// High level skeleton only — the structure may evolve before 1.0.
    /// </summary>
    public sealed class QueryEvent {
        public QueryEvent(string commandText, TimeSpan duration, DateTimeOffset at) {
            CommandText = commandText;
            Duration = duration;
            At = at;
        }

        /// <summary>SQL or provider-specific textual representation (trimmed / may be redacted).</summary>
        public string CommandText { get; }

        /// <summary>Total time spent executing the command.</summary>
        public TimeSpan Duration { get; }

        /// <summary>Timestamp when the command finished.</summary>
        public DateTimeOffset At { get; }
    }
}
