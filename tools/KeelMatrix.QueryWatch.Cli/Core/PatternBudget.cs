#nullable enable
using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Cli.Core {
    internal sealed class PatternBudget {
        public required Regex Regex { get; init; }
        public required int MaxCount { get; init; }
        public required string RawPattern { get; init; }

        public static bool TryParse(string spec, out PatternBudget? budget, out string? error) {
            budget = null; error = null;
            if (string.IsNullOrWhiteSpace(spec)) { error = "Empty spec"; return false; }
            var idx = spec.LastIndexOf('=');
            if (idx <= 0 || idx == spec.Length - 1) { error = "Expected '<pattern>=<max>'"; return false; }
            var pRaw = spec.Substring(0, idx).Trim();
            var maxRaw = spec[(idx + 1)..].Trim();
            if (!int.TryParse(maxRaw, out var max) || max < 0) { error = "Invalid <max> (must be non-negative integer)"; return false; }

            Regex rx;
            if (pRaw.StartsWith("regex:", StringComparison.OrdinalIgnoreCase)) {
                var body = pRaw.Substring("regex:".Length);
                try { rx = new Regex(body, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled); }
                catch (Exception ex) { error = "Invalid regex: " + ex.Message; return false; }
            }
            else {
                // Treat as wildcard: escape then replace * -> .*, ? -> .
                var escaped = Regex.Escape(pRaw).Replace(@"\*", ".*").Replace(@"\?", ".");
                rx = new Regex("^" + escaped + "$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled);
            }

            budget = new PatternBudget { Regex = rx, MaxCount = max, RawPattern = pRaw };
            return true;
        }
    }
}
