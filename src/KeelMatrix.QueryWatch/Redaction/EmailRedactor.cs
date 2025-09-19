using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Redacts email addresses like user@example.com → ***.
    /// </summary>
    public sealed class EmailRedactor : IQueryTextRedactor {
        private static readonly Regex Email = new(
            @"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : Email.Replace(input, "***");
    }
}
