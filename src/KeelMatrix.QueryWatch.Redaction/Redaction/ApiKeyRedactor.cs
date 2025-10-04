#nullable enable
using System.Text.RegularExpressions;

using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>Masks API keys in headers and URL query parameters.</summary>
    /// <remarks>
    /// Matches <c>X-Api-Key</c> and <c>ApiKey</c> headers, and query parameters named
    /// <c>api-key</c>, <c>api_key</c>, <c>apikey</c>, or <c>apiKey</c>. Detection is case‑insensitive
    /// and multi‑line. Replacements preserve the original header/parameter name and replace only
    /// the value with <c>***</c>.
    /// </remarks>
    public sealed class ApiKeyRedactor : IQueryTextRedactor {
        private static readonly Regex Header = RedactionRegex.Create(
            @"(?im)\b(X-?Api-?Key|ApiKey)\s*:\s*[^\r\n]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex Param = RedactionRegex.Create(
            @"(?i)(?<=[\?&])(api[_-]?key|apikey|apiKey)=([^&#\s]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var r = Header.Replace(input, m => m.Groups[1].Value + ": ***");
            r = Param.Replace(r, m => m.Groups[1].Value + "=***");
            return r;
        }
    }
}


