#nullable enable
using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks API keys in headers and URL query parameters.
    /// Handles: "X-Api-Key: ...", "ApiKey: ...", and query params like
    /// <c>?api_key=...&amp;apikey=...&amp;apiKey=...</c>.
    /// </summary>
    public sealed class ApiKeyRedactor : IQueryTextRedactor {
        private static readonly Regex Header = new(
            @"(?im)\b(X-?Api-?Key|ApiKey)\s*:\s*[^\r\n]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex Param = new(
            @"(?i)(?<=[\?&])(api[_-]?key|apikey|apiKey)=([^&#\s]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        /// <remarks>
        /// TODO: When we add Luhn-validated credit cards / IBAN / Slack/GitHub tokens, consider ordering:
        /// normalization → PII/secrets (incl. API keys) → noise (timestamps/IP/phone) to keep budgets stable.
        /// </remarks>
        public string Redact(string input) {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var r = Header.Replace(input, m => m.Groups[1].Value + ": ***");
            r = Param.Replace(r, m => m.Groups[1].Value + "=***");
            return r;
        }
    }
}
