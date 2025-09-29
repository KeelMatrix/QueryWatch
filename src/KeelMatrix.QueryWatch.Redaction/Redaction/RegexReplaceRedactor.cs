
using System.Text.RegularExpressions;

using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Generic regex-based redactor.
    /// </summary>
    public sealed class RegexReplaceRedactor : IQueryTextRedactor {
        private readonly Regex _regex;
        private readonly string _replacement;

        /// <summary>Create a redactor that replaces matches of <paramref name="pattern"/>.</summary>
        public RegexReplaceRedactor(string pattern, string replacement)
            : this(RedactionRegex.Create(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase), replacement) { }

        /// <summary>Create a redactor using a precompiled <see cref="Regex"/>.</summary>
        public RegexReplaceRedactor(Regex regex, string replacement) {
            _regex = regex ?? throw new ArgumentNullException(nameof(regex));
            _replacement = replacement ?? string.Empty;
        }

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : _regex.Replace(input, _replacement);
    }
}


