using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Redacts long hexadecimal tokens (32+ hex chars) → ***.
    /// </summary>
    public sealed class LongHexTokenRedactor : IQueryTextRedactor {
        private static readonly Regex Hex = new(
            @"\b[0-9a-fA-F]{32,}\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : Hex.Replace(input, "***");
    }
}
