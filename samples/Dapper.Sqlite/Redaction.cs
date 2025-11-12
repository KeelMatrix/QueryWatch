using System.Text.RegularExpressions;

namespace DapperSample {
    /// <summary>
    /// Simple redaction helper for the sample (emails + long hex tokens)
    /// </summary>
    internal static partial class Redaction {
        // Very small demo rules; expand as needed
        private static readonly Regex Email = EmailRegex();
        private static readonly Regex HexToken = HexTokenRegex();

        public static string Apply(string input) =>
            HexToken.Replace(Email.Replace(input, "***"), "***");

        public static object Param(object? v) => v is string s ? Apply(s) : v ?? DBNull.Value;

        [GeneratedRegex(@"[A-Za-z0-9._%+\-]+@[A-Za-z0-9.\-]+\.[A-Za-z]{2,}", RegexOptions.Compiled)]
        private static partial Regex EmailRegex();

        [GeneratedRegex(@"\b[0-9a-f]{32,}\b", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
        private static partial Regex HexTokenRegex();
    }
}
