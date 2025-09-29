
#nullable enable
using System.Text.RegularExpressions;

using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks international-looking phone numbers.
    /// - Conservative: requires at least 7 digits in total to avoid over-masking short codes.
    /// - Deterministic: always replaces the whole match (including an optional leading '+') with "***".
    /// - Idempotent: re-applying does not change the output.
    /// </summary>
    public sealed class PhoneRedactor : IQueryTextRedactor {
        // Explanation of the pattern:
        //  (?<!\w)               - don't start in the middle of a word
        //  \+?                   - optional leading '+'
        //  (?=(?:[^\d]*\d){7,})  - require >= 7 digits in the overall match (prevents masking short numbers like 123-45)
        //  (?:\s*\(?\d{1,4}\)?[\s\-.]*){2,} - at least two digit groups with optional separators/parentheses
        //  \d{2,}                - a trailing group with at least 2 digits
        //  (?!\w)                - don't end in the middle of a word
        private static readonly Regex Phone = RedactionRegex.Create(
            @"(?<!\w)\+?(?=(?:[^\d]*\d){7,})(?:\s*\(?\d{1,4}\)?[\s\-.]*){2,}\d{2,}(?!\w)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input)
            => string.IsNullOrEmpty(input) ? string.Empty : Phone.Replace(input, "***");
    }
}


