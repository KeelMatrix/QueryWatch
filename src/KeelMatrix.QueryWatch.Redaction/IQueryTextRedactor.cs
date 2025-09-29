#nullable enable
namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Redacts sensitive or noisy content from SQL text before it is recorded.
    /// Redactors are applied by QueryWatchSession.Record(...) in the order they appear in QueryWatchOptions.Redactors.
    /// </summary>
    public interface IQueryTextRedactor {
        /// <summary>
        /// Return a redacted version of the input string.
        /// </summary>
        string Redact(string input);
    }
}



