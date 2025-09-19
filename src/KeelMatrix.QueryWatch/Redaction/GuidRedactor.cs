#nullable enable
using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks GUIDs in canonical dashed form.
    /// </summary>
    public sealed class GuidRedactor : IQueryTextRedactor {
        private static readonly Regex GuidRx = new(
            @"\b[0-9a-fA-F]{8}(?:-[0-9a-fA-F]{4}){3}-[0-9a-fA-F]{12}\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : GuidRx.Replace(input, "***");
    }
}
