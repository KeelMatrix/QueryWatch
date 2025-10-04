#nullable enable
namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Convenience facade API for the simplest manual usage.
    /// </summary>
    public static class QueryWatcher {
        /// <summary>
        /// Starts a new <see cref="QueryWatchSession"/>.
        /// Call <see cref="QueryWatchSession.Record(string, System.TimeSpan)"/> (or the overload with metadata)
        /// to add events when using manual mode.
        /// </summary>
        /// <param name="options">Optional session options.</param>
        /// <returns>The started session.</returns>
        public static QueryWatchSession Start(QueryWatchOptions? options = null)
            => QueryWatchSession.Start(options);
    }
}
