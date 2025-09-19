#nullable enable
using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks IPv4 and (conservatively) IPv6 addresses.
    /// </summary>
    public sealed class IpAddressRedactor : IQueryTextRedactor {
        private static readonly Regex IPv4 = new(
            @"\b(?:(?:25[0-5]|2[0-4]\d|1?\d{1,2})\.){3}(?:25[0-5]|2[0-4]\d|1?\d{1,2})\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // Conservative IPv6: 2-7 colons, simple hextets (does not try to match every edge case like :: compression).
        private static readonly Regex IPv6 = new(
            @"\b(?:[A-Fa-f0-9]{1,4}:){2,7}[A-Fa-f0-9]{1,4}\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var r = IPv4.Replace(input, "***");
            r = IPv6.Replace(r, "***");
            return r;
        }
    }
}
