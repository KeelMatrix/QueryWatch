using System.Text.RegularExpressions;

using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>Masks AWS Access Key IDs (<c>AKIA</c> followed by 16 base‑36 uppercase characters) → <c>***</c>.</summary>
    /// <remarks>Pattern: <c>\bAKIA[0-9A-Z]{16}\b</c>. Matching is case‑sensitive per the AWS format.</remarks>
    public sealed class AwsAccessKeyRedactor : IQueryTextRedactor {
        private static readonly Regex Akid = RedactionRegex.Create(@"\bAKIA[0-9A-Z]{16}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : Akid.Replace(input, "***");
    }
}


