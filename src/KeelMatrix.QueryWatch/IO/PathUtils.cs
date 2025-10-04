namespace KeelMatrix.QueryWatch.IO {
    /// <summary>
    /// Helpers for working with file system paths in a cross‑platform way.
    /// </summary>
    public static class PathUtils {
        /// <summary>
        /// Combines path segments using the platform’s directory separator.
        /// </summary>
        /// <param name="segments">The individual path segments.</param>
        /// <returns>The combined path.</returns>
        public static string Combine(params string[] segments) => Path.Combine(segments);
    }
}
