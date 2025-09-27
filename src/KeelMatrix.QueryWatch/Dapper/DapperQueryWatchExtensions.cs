using System.Data;

namespace KeelMatrix.QueryWatch.Dapper {
    /// <summary>
    /// Extension helpers for Dapper users: wraps an <see cref="IDbConnection"/> so the
    /// commands Dapper creates are recorded into <see cref="QueryWatchSession"/>.
    /// </summary>
    public static class DapperQueryWatchExtensions {
        /// <summary>
        /// Wrap a connection for QueryWatch.
        /// If <paramref name="connection"/> derives from <c>DbConnection</c>, this returns
        /// the high‑fidelity <c>QueryWatchConnection</c> (supports async); otherwise it falls
        /// back to <see cref="DapperQueryWatchConnection"/> so non‑DbConnection providers still work.
        /// </summary>
        /// <remarks>
        /// Prefer this overload in Dapper scenarios because it preserves
        /// async support when the provider derives from DbConnection. If you also import
        /// <c>KeelMatrix.QueryWatch.Ado</c>, fully qualify the call to avoid extension ambiguity.
        /// </remarks>
        public static IDbConnection WithQueryWatch(this IDbConnection connection, QueryWatchSession session) {
            if (connection is null) throw new ArgumentNullException(nameof(connection));
            if (session is null) throw new ArgumentNullException(nameof(session));

            if (connection is System.Data.Common.DbConnection db)
                return new KeelMatrix.QueryWatch.Ado.QueryWatchConnection(db, session);

            return new DapperQueryWatchConnection(connection, session);
        }
    }
}
