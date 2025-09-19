#nullable enable
using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks HTTP Authorization headers commonly embedded in SQL comments/logs.
    /// Examples: "Authorization: Bearer eyJ..." or "Authorization: Basic dXNlcjo...".
    /// </summary>
    public sealed class AuthorizationRedactor : IQueryTextRedactor {
        private static readonly Regex Auth = new(
            @"(?im)\bAuthorization\s*:\s*(?:Bearer|Basic)\s+[A-Za-z0-9._~+\-/=]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : Auth.Replace(input, "Authorization: ***");
    }
}
