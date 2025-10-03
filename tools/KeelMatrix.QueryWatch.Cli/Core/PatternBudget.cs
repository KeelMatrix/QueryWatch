using System.Text.RegularExpressions;
using KeelMatrix.QueryWatch.Contracts;

namespace KeelMatrix.QueryWatch.Cli.Core {
    public sealed class PatternBudget {
        public string Raw { get; }
        public bool IsRegex { get; }
        public string Pattern { get; }
        public int Max { get; }

        private PatternBudget(string raw, bool isRegex, string pattern, int max) {
            Raw = raw; IsRegex = isRegex; Pattern = pattern; Max = max;
        }

        public static bool TryParse(string input, out PatternBudget? budget, out string? error) {
            budget = null; error = null;
            if (string.IsNullOrWhiteSpace(input)) { error = "Empty budget"; return false; }

            string s = input.Trim();
            int sep = s.LastIndexOf('=');
            if (sep < 0) sep = s.LastIndexOf('|');
            if (sep <= 0 || sep >= s.Length - 1) { error = $"Invalid --budget value '{input}'"; return false; }

            string left = s.Substring(0, sep);
            string right = s.Substring(sep + 1);
            if (!int.TryParse(right, out int max) || max < 0) { error = $"Invalid max in --budget '{input}'"; return false; }

            if (left.StartsWith("regex:", StringComparison.OrdinalIgnoreCase)) {
                var pattern = left.Substring("regex:".Length);
                try {
                    _ = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
                }
                catch (Exception ex) { error = $"Invalid --budget value '{input}': Invalid regex: {ex.Message}"; return false; }
                budget = new PatternBudget(input, true, pattern, max);
                return true;
            }

            // Wildcards: * and ? → convert to regex (anchor at start; allow trailing chars)
            string wildcard = left;
            string rx = "^" + Regex.Escape(wildcard).Replace("\\*", ".*").Replace("\\?", ".") + ".*$";
            budget = new PatternBudget(input, false, rx, max);
            return true;
        }

        public int CountMatches(IEnumerable<string> corpus) {
            var rx = new Regex(Pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            int count = 0;
            foreach (var s in corpus) {
                if (s is null) continue;
                if (rx.IsMatch(s)) count++;
            }
            return count;
        }

        // TODO: why is this unused?
        public static List<(PatternBudget budget, int count, bool over)> EvaluateBudgets(
            IEnumerable<EventSample> events,
            IEnumerable<PatternBudget> budgets) {
            var findings = new List<(PatternBudget, int, bool)>();
            var corpus = (events ?? Enumerable.Empty<EventSample>())
                .Select(e => e?.Text ?? string.Empty)
                .ToList();

            foreach (var b in budgets ?? Enumerable.Empty<PatternBudget>()) {
                int count = b.CountMatches(corpus);
                bool over = count > b.Max;
                findings.Add((b, count, over));
            }

            return findings;
        }
    }
}
