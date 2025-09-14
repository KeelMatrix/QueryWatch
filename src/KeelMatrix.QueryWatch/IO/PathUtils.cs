namespace KeelMatrix.QueryWatch.IO
{
    /// <summary>
    /// Provides helpers for working with file system paths in a cross‑platform way.
    /// Use these helpers instead of building paths manually to avoid problems
    /// related to directory separators on Windows versus Linux/macOS.
    /// </summary>
    public static class PathUtils
    {
        /// <summary>
        /// Combines path segments using the platform's directory separator.
        /// </summary>
        /// <param name="segments">The individual path segments.</param>
        /// <returns>A combined path.</returns>
        public static string Combine(params string[] segments) => Path.Combine(segments);
    }
}
