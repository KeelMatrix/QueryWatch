#nullable enable
using System.Text.RegularExpressions;
using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks password values inside connection strings (<c>Password=…</c> and <c>Pwd=…</c> forms).
    /// Supports quoted values so that <c>Password="sec;ret;value"</c> is fully masked.
    /// </summary>
    /// <remarks>
    /// Key matching is case‑sensitive (only <c>Password</c> and <c>Pwd</c> are recognized).
    /// The original key casing is preserved in the output; only the value is replaced by <c>***</c>.
    /// </remarks>
    public sealed class ConnectionStringPasswordRedactor : IQueryTextRedactor {
        private static readonly Regex Pw = RedactionRegex.Create(
            // key (group 1), "=", then either a quoted value (single or double) or an unquoted value up to the next semicolon
            @"\b(Password|Pwd)\s*=\s*(?:""[^""]*""|'[^']*'|[^;]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input)
            => string.IsNullOrEmpty(input) ? string.Empty : Pw.Replace(input, m => m.Groups[1].Value + "=***");
    }
}
