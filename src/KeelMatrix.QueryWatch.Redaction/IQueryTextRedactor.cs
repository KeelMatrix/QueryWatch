namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Contract for text redactors used by QueryWatch to mask secrets/PII or normalize noise
    /// before SQL or provider text is recorded.
    /// </summary>
    /// <remarks>
    /// Implementations must be <em>pure</em> and idempotent: invoking <see cref="Redact"/> multiple times
    /// with the same input produces the same output. They should be thread‑safe and tolerate
    /// <c>null</c> or empty input by returning <see cref="string.Empty"/>. Redactors run in the order
    /// they are added to <c>QueryWatchOptions.Redactors</c>.
    /// </remarks>
    public interface IQueryTextRedactor {
        /// <summary>Returns a redacted form of <paramref name="input"/>.</summary>
        /// <param name="input">
        /// Arbitrary text that may contain sensitive values. Implementations should treat <c>null</c>
        /// as <see cref="string.Empty"/>.
        /// </param>
        /// <returns>
        /// The redacted text. If <paramref name="input"/> is <c>null</c> or empty, returns
        /// <see cref="string.Empty"/>.
        /// </returns>
        string Redact(string input);
    }
}



