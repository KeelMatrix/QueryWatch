using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Redacts JWT-like tokens (three base64url segments separated by dots) → ***.
    /// The pattern is conservative to avoid false positives.
    /// </summary>
    public sealed class JwtTokenRedactor : IQueryTextRedactor {
        private static readonly Regex Jwt = new(
            @"\b[A-Za-z0-9\-_]{10,}\.[A-Za-z0-9\-_]{10,}\.[A-Za-z0-9\-_]{10,}\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : Jwt.Replace(input, "***");
    }
}
