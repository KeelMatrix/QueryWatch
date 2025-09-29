
#nullable enable
using System.Text.RegularExpressions;

using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks Google API keys that match the common pattern: AIza[0-9A-Za-z\-_]{35}.
    /// </summary>
    public sealed class GoogleApiKeyRedactor : IQueryTextRedactor {
        private static readonly Regex GKey = RedactionRegex.Create(
            @"\bAIza[0-9A-Za-z\-_]{35}\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : GKey.Replace(input, "***");
    }
}


