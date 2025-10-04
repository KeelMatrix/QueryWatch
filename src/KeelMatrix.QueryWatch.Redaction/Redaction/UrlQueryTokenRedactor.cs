#nullable enable
using System.Text.RegularExpressions;
using KeelMatrix.QueryWatch.Redaction.Internal;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks sensitive URL query parameters like <c>token</c>, <c>access_token</c>, <c>code</c>,
    /// <c>id_token</c>, and <c>auth</c>. Also handles tokens in URL fragments (after <c>#</c>).
    /// </summary>
    /// <remarks>
    /// Detection is case‑insensitive and applies to both query (<c>?</c>/<c>&amp;</c>) and fragment (<c>#</c>) sections.
    /// The original parameter name is preserved; only the value is replaced with <c>***</c>.
    /// Percent‑encoding is not decoded.
    /// </remarks>
    public sealed class UrlQueryTokenRedactor : IQueryTextRedactor {
        private static readonly Regex Param = RedactionRegex.Create(
            // the parameter name (group 1) must be preceded by ?, &, or #; value runs until next & or # or whitespace
            @"(?i)(?:(?<=[\?&])|(?<=#))(token|access_token|code|id_token|auth)=([^&#\s]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input)
            => string.IsNullOrEmpty(input) ? string.Empty : Param.Replace(input, m => m.Groups[1].Value + "=***");
    }
}


