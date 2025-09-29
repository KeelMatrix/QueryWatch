#nullable enable
using System.Text.RegularExpressions;

using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks IPv4 and IPv6 addresses, including IPv6 compressed forms like <c>::1</c>.
    /// Uses lookarounds instead of word boundaries so addresses starting with ':' are matched.
    /// </summary>
    public sealed class IpAddressRedactor : IQueryTextRedactor {
        private static readonly Regex IPv4 = RedactionRegex.Create(
            @"\b(?:(?:25[0-5]|2[0-4]\d|1?\d{1,2})\.){3}(?:25[0-5]|2[0-4]\d|1?\d{1,2})\b",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // IPv6 with '::' compression (covers ::1, fe80::1, 2001:db8::7334, etc.)
        private static readonly Regex IPv6Compressed = RedactionRegex.Create(
            @"(?<![A-Fa-f0-9:])(?:[A-Fa-f0-9]{1,4}(?::[A-Fa-f0-9]{1,4}){0,5})?::(?:[A-Fa-f0-9]{1,4}(?::[A-Fa-f0-9]{1,4}){0,5})(?![A-Fa-f0-9:])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        // IPv6 without '::' compression (2–8 hextets)
        private static readonly Regex IPv6Full = RedactionRegex.Create(
            @"(?<![A-Fa-f0-9:])(?:[A-Fa-f0-9]{1,4}:){2,7}[A-Fa-f0-9]{1,4}(?![A-Fa-f0-9:])",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc/>
        public string Redact(string input) {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var result = IPv4.Replace(input, "***");
            // Replace compressed first to ensure leading '::' forms are handled
            result = IPv6Compressed.Replace(result, "***");
            result = IPv6Full.Replace(result, "***");
            return result;
        }
    }
}


