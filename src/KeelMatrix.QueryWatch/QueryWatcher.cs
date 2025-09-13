#nullable enable
using System;

namespace KeelMatrix.QueryWatch
{
    /// <summary>
    /// Convenience facade API for the simplest manual usage.
    /// High-level only; low-level interception adapters live in separate files/namespaces.
    /// </summary>
    public static class QueryWatcher
    {
        /// <summary>
        /// Start a session. In manual mode, you call <see cref="QueryWatchSession.Record"/> yourself.
        /// In EF Core mode, the EF adapter will record automatically.
        /// </summary>
        public static QueryWatchSession Start(QueryWatchOptions? options = null) => QueryWatchSession.Start(options);
    }
}
