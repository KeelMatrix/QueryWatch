#nullable enable
using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks common Azure-style keys in connection strings or SAS tokens:
    /// AccountKey=..., SharedAccessKey=..., SharedAccessSignature=...
    /// </summary>
    public sealed class AzureKeyLikeRedactor : IQueryTextRedactor {
        private static readonly Regex AzureKey = new(
            @"(?i)\b(AccountKey|SharedAccessKey|SharedAccessSignature)\s*=\s*[^;,\s]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : AzureKey.Replace(input, m => m.Groups[1].Value + "=***");
    }
}
