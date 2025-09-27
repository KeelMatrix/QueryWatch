#nullable enable
using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks password values inside connection strings (Password=...; Pwd=...; forms).
    /// Supports quoted values so that <c>Password="sec;ret;value"</c> is fully masked.
    /// </summary>
    public sealed class ConnectionStringPasswordRedactor : IQueryTextRedactor {
        private static readonly Regex Pw = new(
            // key (group 1), "=", then either a quoted value (single or double) or an unquoted value up to the next semicolon
            @"\b(Password|Pwd)\s*=\s*(?:""[^""]*""|'[^']*'|[^;]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input)
            => string.IsNullOrEmpty(input) ? string.Empty : Pw.Replace(input, m => m.Groups[1].Value + "=***");
    }
}
