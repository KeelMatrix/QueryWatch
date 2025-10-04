using System.Data;

namespace KeelMatrix.QueryWatch.Dapper {
    /// <summary>
    /// Extension helpers for Dapper users: wraps an <see cref="IDbConnection"/> so commands are recorded into a <see cref="QueryWatchSession"/>.
    /// </summary>
    public static class DapperQueryWatchExtensions {
        /// <summary>
        /// Wraps a connection for QueryWatch. Returns the high‑fidelity ADO wrapper when possible; otherwise falls back to Dapper‑specific wrapper.
        /// </summary>
        /// <param name="connection">Connection to wrap.</param>
        /// <param name="session">Session to record into.</param>
        /// <returns>A wrapper connection that instruments commands.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connection"/> or <paramref name="session"/> is null.</exception>
        public static IDbConnection WithQueryWatch(this IDbConnection connection, QueryWatchSession session) {
            if (connection is null) throw new ArgumentNullException(nameof(connection));
            if (session is null) throw new ArgumentNullException(nameof(session));

            if (connection is System.Data.Common.DbConnection db)
                return new KeelMatrix.QueryWatch.Ado.QueryWatchConnection(db, session);

            return new DapperQueryWatchConnection(connection, session);
        }
    }
}
