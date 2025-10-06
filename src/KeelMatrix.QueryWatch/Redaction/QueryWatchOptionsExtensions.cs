#nullable enable
namespace KeelMatrix.QueryWatch.Redaction {
    /// <summary>
    /// Extensions for <see cref="QueryWatchOptions"/> to add built‑in redactors.
    /// </summary>
    public static class QueryWatchOptionsExtensions {
        /// <summary>
        /// Adds a recommended set of redactors in a safe order (normalize → PII/secrets → noise).
        /// </summary>
        /// <param name="options">Options to modify.</param>
        /// <param name="includeWhitespaceNormalizer">Normalize whitespace before masking. Default: <c>true</c>.</param>
        /// <param name="includeEmails">Mask emails. Default: <c>true</c>.</param>
        /// <param name="includeLongHex">Mask long hex tokens (32+). Default: <c>true</c>.</param>
        /// <param name="includeJwt">Mask JWT‑like tokens. Default: <c>true</c>.</param>
        /// <param name="includeAuthorization">Mask Authorization headers. Default: <c>true</c>.</param>
        /// <param name="includeConnStringPwd">Mask password in connection strings. Default: <c>true</c>.</param>
        /// <param name="includeGuidLikeHex">Mask shorter hex identifiers (16–31 chars, at least one letter). Default: <c>true</c>.</param>
        /// <param name="includeGuid">Mask GUIDs. Default: <c>true</c>.</param>
        /// <param name="includeUrlTokens">Mask URL query tokens (token/access_token/code/id_token/auth). Default: <c>true</c>.</param>
        /// <param name="includeAwsAccessKey">Mask AWS Access Key IDs. Default: <c>true</c>.</param>
        /// <param name="includeAzureKeys">Mask Azure AccountKey/SharedAccess*. Default: <c>true</c>.</param>
        /// <param name="includeTimestamps">Mask ISO/Unix timestamps (can increase false positives). Default: <c>false</c>.</param>
        /// <param name="includeIpAddresses">Mask IPv4/IPv6 addresses. Default: <c>false</c>.</param>
        /// <param name="includePhone">Mask common phone numbers. Default: <c>false</c>.</param>
        /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
        public static QueryWatchOptions UseRecommendedRedactors(
            this QueryWatchOptions options,
            bool includeWhitespaceNormalizer = true,
            bool includeEmails = true,
            bool includeLongHex = true,
            bool includeJwt = true,
            bool includeAuthorization = true,
            bool includeConnStringPwd = true,
            bool includeGuid = true,
            bool includeUrlTokens = true,
            bool includeAwsAccessKey = true,
            bool includeAzureKeys = true,
            bool includeGuidLikeHex = true,
            bool includeTimestamps = false,
            bool includeIpAddresses = false,
            bool includePhone = false) {
            if (options is null) throw new ArgumentNullException(nameof(options));

            // 1) Normalize first
            if (includeWhitespaceNormalizer) options.Redactors.Add(new WhitespaceNormalizerRedactor());

            // 2) PII / Secrets
            if (includeEmails) options.Redactors.Add(new EmailRedactor());
            if (includeLongHex) options.Redactors.Add(new LongHexTokenRedactor());
            if (includeJwt) options.Redactors.Add(new JwtTokenRedactor());
            if (includeAuthorization) options.Redactors.Add(new AuthorizationRedactor());
            if (includeConnStringPwd) options.Redactors.Add(new ConnectionStringPasswordRedactor());
            if (includeGuid) options.Redactors.Add(new GuidRedactor());
            if (includeUrlTokens) options.Redactors.Add(new UrlQueryTokenRedactor());
            if (includeAwsAccessKey) options.Redactors.Add(new AwsAccessKeyRedactor());
            if (includeAzureKeys) options.Redactors.Add(new AzureKeyLikeRedactor());
            if (includeGuidLikeHex) options.Redactors.Add(new GuidLikeHexRedactor());

            // 3) Noise / stability
            if (includeTimestamps) options.Redactors.Add(new TimestampRedactor());
            if (includeIpAddresses) options.Redactors.Add(new IpAddressRedactor());
            if (includePhone) options.Redactors.Add(new PhoneRedactor());

            return options;
        }

        /// <summary>
        /// Adds a single regex‑based redactor to <paramref name="options"/>.
        /// </summary>
        /// <param name="options">Options to modify.</param>
        /// <param name="pattern">Regex pattern to replace.</param>
        /// <param name="replacement">Replacement text. Default: <c>***</c>.</param>
        /// <returns>The same <paramref name="options"/> instance for chaining.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
        public static QueryWatchOptions AddRegexRedactor(this QueryWatchOptions options, string pattern, string replacement = "***") {
            if (options is null) throw new ArgumentNullException(nameof(options));
            options.Redactors.Add(new RegexReplaceRedactor(pattern, replacement));
            return options;
        }
    }
}
