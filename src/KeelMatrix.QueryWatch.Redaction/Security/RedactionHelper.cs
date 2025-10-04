using System.Text.RegularExpressions;
using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Security {
    /// <summary>
    /// Provides utilities to redact potentially sensitive information from text, such as
    /// email addresses or tokens. You can add additional patterns as needed.
    /// By default, redaction is off; wire this into your logging pipeline if you
    /// anticipate capturing end‑user data.
    /// </summary>
    public static class RedactionHelper {
        // Matches common email addresses.
        private static readonly Regex EmailRegex = RedactionRegex.Create(@"[a-zA-Z0-9_.+-]+@[a-zA-Z0-9-]+\.[a-zA-Z0-9-.]+", RegexOptions.Compiled);

        // Matches tokens consisting of 20 or more URL‑safe characters.
        private static readonly Regex TokenRegex = RedactionRegex.Create(@"(\b[A-Za-z0-9-_]{20,}\b)", RegexOptions.Compiled);

        /// <summary>Redacts email addresses and high‑entropy tokens from <paramref name="input"/>.</summary>
        /// <param name="input">Text that may contain sensitive data. If <c>null</c> or empty, the original value is returned.</param>
        /// <returns>The redacted text.</returns>
        /// <remarks>
        /// This helper is not wired into QueryWatch automatically; integrate it explicitly into your logging pipeline if needed.
        /// Patterns are intentionally conservative to minimize false positives.
        /// </remarks>
        public static string Redact(string input) {
            if (string.IsNullOrEmpty(input)) return input;
            var redacted = EmailRegex.Replace(input, "&lt;REDACTED_EMAIL&gt;");
            redacted = TokenRegex.Replace(redacted, "&lt;REDACTED_TOKEN&gt;");
            return redacted;
        }
    }
}


