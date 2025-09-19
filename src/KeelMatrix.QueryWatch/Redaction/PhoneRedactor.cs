#nullable enable
using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks international-looking phone numbers. Conservative to avoid over-masking.
    /// </summary>
    public sealed class PhoneRedactor : IQueryTextRedactor {
        private static readonly Regex Phone = new(
            @"\b\+?\d[\d\-\s\(\)]{6,}\d\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : Phone.Replace(input, "***");
    }
}
