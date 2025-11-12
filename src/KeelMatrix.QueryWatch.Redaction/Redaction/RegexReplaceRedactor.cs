using System.Text.RegularExpressions;
using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// A reusable redactor that replaces all matches of a regular expression with a replacement string.
    /// </summary>
    /// <remarks>
    /// When constructed from a <see cref="string"/> pattern, the regex is created with
    /// <see cref="System.Text.RegularExpressions.RegexOptions.Compiled"/>,
    /// <see cref="System.Text.RegularExpressions.RegexOptions.CultureInvariant"/>, and
    /// <see cref="System.Text.RegularExpressions.RegexOptions.IgnoreCase"/> to reduce noise from casing and locales.
    /// Instances are immutable and thread‑safe.
    /// </remarks>
    /// <remarks>Creates a redactor using a precompiled <see cref="System.Text.RegularExpressions.Regex"/>.</remarks>
    /// <param name="regex">The regular expression to apply.</param>
    /// <param name="replacement">
    /// The replacement to use. If <c>null</c>, an empty string is used.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="regex"/> is <c>null</c>.</exception>
    public sealed class RegexReplaceRedactor(Regex regex, string replacement) : IQueryTextRedactor {
        private readonly Regex _regex = regex ?? throw new ArgumentNullException(nameof(regex));
        private readonly string _replacement = replacement ?? string.Empty;

        /// <summary>Creates a redactor that replaces matches of <paramref name="pattern"/>.</summary>
        /// <param name="pattern">
        /// The regular expression pattern. It is compiled with
        /// <see cref="System.Text.RegularExpressions.RegexOptions.Compiled"/>,
        /// <see cref="System.Text.RegularExpressions.RegexOptions.CultureInvariant"/>, and
        /// <see cref="System.Text.RegularExpressions.RegexOptions.IgnoreCase"/>.
        /// </param>
        /// <param name="replacement">
        /// The replacement to use. If <c>null</c>, an empty string is used.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="pattern"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">The pattern is not a valid regular expression.</exception>
        public RegexReplaceRedactor(string pattern, string replacement)
            : this(RedactionRegex.Create(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase), replacement) { }

        /// <inheritdoc />
        public string Redact(string input) => string.IsNullOrEmpty(input) ? string.Empty : _regex.Replace(input, _replacement);
    }
}


