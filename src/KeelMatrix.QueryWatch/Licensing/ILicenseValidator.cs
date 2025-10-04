namespace KeelMatrix.QueryWatch.Licensing {
    /// <summary>
    /// Represents a mechanism to validate license keys for premium features.
    /// </summary>
    public interface ILicenseValidator {
        /// <summary>
        /// Determines whether the given license key is valid.
        /// </summary>
        /// <param name="licenseKey">The license key to validate.</param>
        /// <returns><c>true</c> if the key is valid; otherwise, <c>false</c>.</returns>
        bool IsValid(string licenseKey);
    }

    /// <summary>
    /// A default no‑op license validator used when no real validation is configured.
    /// </summary>
    public sealed class NoopLicenseValidator : ILicenseValidator {
        /// <inheritdoc />
        public bool IsValid(string licenseKey) => true;
    }
}
