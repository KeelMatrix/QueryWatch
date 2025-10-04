#nullable enable
using System.Text.RegularExpressions;
using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>Masks timestamps to reduce churn in snapshots.</summary>
    /// <remarks>
    /// Handles ISO‑8601 instants (optionally fractional seconds and <c>Z</c>/offset) and long Unix <em>seconds</em>
    /// (10–11 digits in modern ranges). Unix <em>milliseconds</em> (13 digits) are intentionally not masked to avoid
    /// false positives in identifiers.
    /// </remarks>
    public sealed class TimestampRedactor : IQueryTextRedactor {
        private static readonly Regex Iso = RedactionRegex.Create(
            @"\b\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?(?:Z|[+\-]\d{2}:\d{2})?\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Unix seconds (rough): 10-11 digits starting with 15.. to 20.. (modern ranges).
        private static readonly Regex Unix = RedactionRegex.Create(@"\b1[5-9]\d{8,10}\b", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var r = Iso.Replace(input, "***");
            r = Unix.Replace(r, "***");
            return r;
        }
    }
}


