#nullable enable
using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks AWS Access Key IDs like AKIAxxxxxxxxxxxxxxxx.
    /// </summary>
    public sealed class AwsAccessKeyRedactor : IQueryTextRedactor {
        private static readonly Regex Akid = new(@"\bAKIA[0-9A-Z]{16}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : Akid.Replace(input, "***");
    }
}
