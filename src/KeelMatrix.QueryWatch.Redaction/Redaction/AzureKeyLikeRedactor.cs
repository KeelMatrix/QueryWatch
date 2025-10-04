#nullable enable
using System.Text.RegularExpressions;
using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks common Azure-style secrets in connection strings or SAS tokens:
    /// - AccountKey=...
    /// - SharedAccessKey=...
    /// - SharedAccessSignature=...
    /// Output is canonicalized to 'AccountKey=***', 'SharedAccessKey=***', 'SharedAccessSignature=***'.
    /// </summary>
    public sealed class AzureKeyLikeRedactor : IQueryTextRedactor {
        private static readonly Regex AzureKey = RedactionRegex.Create(
            // key (group 1), then '=', then a value up to a common delimiter or whitespace
            @"\b(AccountKey|SharedAccessKey|SharedAccessSignature)\s*=\s*[^;,\s]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        /// <inheritdoc />
        public string Redact(string input) {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            return AzureKey.Replace(input, static m => {
                var key = m.Groups[1].Value.ToLowerInvariant();
                var canonicalKey = key switch {
                    "accountkey" => "AccountKey",
                    "sharedaccesskey" => "SharedAccessKey",
                    "sharedaccesssignature" => "SharedAccessSignature",
                    _ => m.Groups[1].Value // fallback (shouldn't happen)
                };
                return $"{canonicalKey}=***";
            });
        }
    }
}


