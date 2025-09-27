#nullable enable
using System.Text.RegularExpressions;

namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Masks sensitive URL query parameters like token, access_token, code, id_token, auth.
    /// Also handles tokens present in URL fragments (after <c>#</c>), which are common in OAuth implicit flows.
    /// </summary>
    public sealed class UrlQueryTokenRedactor : IQueryTextRedactor {
        private static readonly Regex Param = new(
            // the parameter name (group 1) must be preceded by ?, &, or #; value runs until next & or # or whitespace
            @"(?i)(?:(?<=[\?&])|(?<=#))(token|access_token|code|id_token|auth)=([^&#\s]+)",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <inheritdoc />
        public string Redact(string input)
            => string.IsNullOrEmpty(input) ? string.Empty : Param.Replace(input, m => m.Groups[1].Value + "=***");
    }
}
