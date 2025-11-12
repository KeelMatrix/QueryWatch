using System.Text.RegularExpressions;
using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks shorter hex tokens that look like identifiers but are not full GUIDs.
    /// Requires at least one hex letter to avoid masking plain long integers.
    /// </summary>
    public sealed class GuidLikeHexRedactor : IQueryTextRedactor {
        // At least one letter [A-Fa-f] to avoid matching purely numeric strings.
        private static readonly Regex Hexish = RedactionRegex.Create(
            @"\b(?=[0-9A-Fa-f]{16,31}\b)(?=.*[A-Fa-f])[0-9A-Fa-f]{16,31}\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : Hexish.Replace(input, "***");
    }
}


