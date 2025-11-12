using System.Text.RegularExpressions;

using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks HTTP <c>Authorization</c> headers (Bearer/Basic) commonly embedded in SQL comments or logs.
    /// </summary>
    /// <remarks>
    /// Matching is case‑insensitive and multi‑line, and output is canonicalized to <c>Authorization: ***</c>.
    /// Other authorization schemes are intentionally ignored.
    /// </remarks>
    public sealed class AuthorizationRedactor : IQueryTextRedactor {
        private static readonly Regex Auth = RedactionRegex.Create(
            @"(?im)\bAuthorization\s*:\s*(?:Bearer|Basic)\s+[A-Za-z0-9._~+\-/=]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : Auth.Replace(input, "Authorization: ***");
    }
}


