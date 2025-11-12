using System.Text.RegularExpressions;
using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks international-looking phone numbers.
    /// - Requires ≥ 7 digits overall (so short codes like "123-45" are not masked).
    /// - Replaces the entire phone (including leading '+' or '(' and country/area prefixes) with "***".
    /// - Idempotent: reapplying does not change the output.
    /// </summary>
    public sealed class PhoneRedactor : IQueryTextRedactor {
        // Start-of-match rule:
        //   (?<!\w)    -> not preceded by a word char (lets us start at '+' or '(' as part of the phone)
        // Require at least 7 digits in the entire match (prevents masking short numbers):
        //   (?=(?:\D*\d){7,})
        // Phone shape (very permissive but safe for masking):
        //   (?:\+?[\s\-.]*)?            optional leading '+' with optional separators
        //   (?:\(?\d{1,4}\)?[\s\-.]*){2,}  at least two digit groups (country/area + local parts)
        //   \d{2,}                      final digit group with ≥2 digits
        // End-of-match rule:
        //   (?!\w)    -> not followed by a word char
        private static readonly Regex Phone = RedactionRegex.Create(
            @"(?<!\w)(?=(?:\D*\d){7,})(?:\+?[\s\-.]*)?(?:\(?\d{1,4}\)?[\s\-.]*){2,}\d{2,}(?!\w)",
            RegexOptions.None);

        /// <inheritdoc/>
        public string Redact(string input) {
            if (string.IsNullOrEmpty(input)) return input;

            // Fast path: skip regex if fewer than 7 digits in total.
            int digits = 0;
            for (int i = 0; i < input.Length && digits < 7; i++) {
                char ch = input[i];
                if (ch is >= '0' and <= '9') digits++;
            }
            return digits < 7 ? input : Phone.Replace(input, "***");
        }
    }
}
