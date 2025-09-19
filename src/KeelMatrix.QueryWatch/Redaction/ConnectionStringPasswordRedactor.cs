#nullable enable
using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks password values inside connection strings (Password=...; Pwd=...; forms).
    /// </summary>
    public sealed class ConnectionStringPasswordRedactor : IQueryTextRedactor {
        private static readonly Regex Pw = new(
            @"(?i)\b(Password|Pwd)\s*=\s*([^;]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : Pw.Replace(input, m => m.Groups[1].Value + "=***");
    }
}
