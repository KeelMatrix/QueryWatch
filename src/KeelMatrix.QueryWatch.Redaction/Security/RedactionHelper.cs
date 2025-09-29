
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

        /// <summary>
        /// Redacts email addresses and tokens from the specified input.
        /// </summary>
        /// <param name="input">The input string that may contain sensitive data.</param>
        /// <returns>The redacted string.</returns>
        public static string Redact(string input) {
            if (string.IsNullOrEmpty(input)) return input;
            var redacted = EmailRegex.Replace(input, "&lt;REDACTED_EMAIL&gt;");
            redacted = TokenRegex.Replace(redacted, "&lt;REDACTED_TOKEN&gt;");
            return redacted;
        }
    }
}


