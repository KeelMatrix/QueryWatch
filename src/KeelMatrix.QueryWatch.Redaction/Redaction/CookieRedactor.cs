
#nullable enable
using System.Text.RegularExpressions;

using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks cookies in HTTP headers commonly seen in SQL comments/logs.
    /// - "Cookie: ..." → "Cookie: ***"
    /// - "Set-Cookie: name=value; Attr..." → "Set-Cookie: name=***; Attr..."
    /// </summary>
    public sealed class CookieRedactor : IQueryTextRedactor {
        private static readonly Regex CookieHeader = RedactionRegex.Create(
            @"(?im)^\s*Cookie\s*:\s*[^\r\n]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);
        private static readonly Regex SetCookie = RedactionRegex.Create(
            @"(?im)^\s*Set-Cookie\s*:\s*([^\s=;]+)\s*=\s*[^;\r\n]+",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input) {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // Collapse entire Cookie header to avoid partial leaks across many pairs
            var r = CookieHeader.Replace(input, "Cookie: ***");
            // Keep cookie name but mask the primary value; leave attributes as-is
            r = SetCookie.Replace(r, m => $"Set-Cookie: {m.Groups[1].Value}=***");
            return r;
        }
    }
}


