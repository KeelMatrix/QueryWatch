#nullable enable
using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction.Internal {
    /// <summary>
    /// Centralized creation of hardened <see cref="Regex"/> instances.
    /// <list type="bullet">
    /// <item><description>Always <see cref="RegexOptions.Compiled"/> and <see cref="RegexOptions.CultureInvariant"/>.</description></item>
    /// <item><description>On .NET 8+, enables <c>RegexOptions.NonBacktracking</c> only when the pattern is compatible.</description></item>
    /// <item><description>Applies a short match timeout (default: 100 ms).</description></item>
    /// </list>
    /// </summary>
    internal static class RedactionRegex {
        /// <summary>Create a configured <see cref="Regex"/> with NonBacktracking where supported, with safe fallback.</summary>
        public static Regex Create(string pattern, RegexOptions options = RegexOptions.None, int timeoutMs = 100) {
            var baseOptions = RegexOptions.Compiled | RegexOptions.CultureInvariant | options;
            var timeout = TimeSpan.FromMilliseconds(timeoutMs);

#if NET8_0_OR_GREATER
            // Proactively avoid NonBacktracking for constructs it does not support.
            if (IsSafeForNonBacktracking(pattern)) {
                try {
                    return new Regex(pattern, baseOptions | RegexOptions.NonBacktracking, timeout);
                }
                catch (NotSupportedException) {
                    // Belt-and-suspenders: if the pattern still isn't supported, fall back below.
                }
            }
#endif
            return new Regex(pattern, baseOptions, timeout);
        }

#if NET8_0_OR_GREATER
        // Keep this simple and conservative: if we see *any* look-around or back-refs, don't use NonBacktracking.
        private static bool IsSafeForNonBacktracking(string pattern) {
            // Look-ahead / look-behind
            if (pattern.Contains("(?=")) return false;
            if (pattern.Contains("(?!")) return false;
            if (pattern.Contains("(?<=")) return false;
            if (pattern.Contains("(?<!")) return false;

            // Back-references (named or numeric)
            if (pattern.Contains(@"\k<")) return false;
            for (int i = 1; i <= 9; i++) {
                if (pattern.Contains("\\" + i.ToString(System.Globalization.CultureInfo.InvariantCulture))) return false;
            }

            // Balancing groups / conditionals (rare in our codebase, but be safe)
            if (pattern.Contains("(?<-")) return false;
            if (pattern.Contains("(?>")) return false;

            return true;
        }
#endif
    }
}
