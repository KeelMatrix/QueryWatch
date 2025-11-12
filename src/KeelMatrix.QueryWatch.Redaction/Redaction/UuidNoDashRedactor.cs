using System.Text.RegularExpressions;
using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks UUIDs without dashes (32 hex chars). Requires at least one letter to avoid masking long integers.
    /// </summary>
    public sealed class UuidNoDashRedactor : IQueryTextRedactor {
        private static readonly Regex Uuid = RedactionRegex.Create(
            @"\b(?=[0-9A-Fa-f]{32}\b)(?=.*[A-Fa-f])[0-9A-Fa-f]{32}\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : Uuid.Replace(input, "***");
    }
}


