#nullable enable
namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Redacts sensitive or noisy content from SQL text before it is recorded.
    /// Redactors are applied by <see cref="QueryWatchSession.Record(string, System.TimeSpan)"/> in the order they appear in <see cref="QueryWatchOptions.Redactors"/>.
    /// </summary>
    public interface IQueryTextRedactor {
        /// <summary>
        /// Return a redacted version of the input string.
        /// </summary>
        string Redact(string input);
    }
}
