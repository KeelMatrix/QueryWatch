// Copyright (c) KeelMatrix

using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using KeelMatrix.QueryWatch.Infrastructure;

namespace KeelMatrix.QueryWatch {
    /// <summary>
    /// Canonical entry point for wrapping ADO.NET/Dapper connections so commands
    /// are recorded into a <see cref="QueryWatchSession"/>.
    /// </summary>
    public static class QueryWatchExtensions {
        [SuppressMessage("Performance", "CA1859:Use concrete types when possible for improved performance")]
        private static readonly IConnectionInstrumenter _instrumenter
            = new DefaultConnectionInstrumenter();

        /// <summary>
        /// Wrap a <see cref="DbConnection"/> so commands are recorded into <see cref="QueryWatchSession"/>.
        /// </summary>
        /// <param name="connection">Provider connection to wrap.</param>
        /// <param name="session">Session to record into.</param>
        /// <returns>A wrapper connection that instruments commands.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connection"/> or <paramref name="session"/> is null.</exception>
        public static DbConnection WithQueryWatch(this DbConnection connection, QueryWatchSession session) {
            return _instrumenter.Wrap(connection, session);
        }

        /// <summary>
        /// Wrap an <see cref="IDbConnection"/> so commands are recorded into <see cref="QueryWatchSession"/>.
        /// If the underlying type derives from <see cref="DbConnection"/>, the high-fidelity ADO wrapper is used;
        /// otherwise falls back to Dapper‑specific wrapper.
        /// </summary>
        /// <param name="connection">Provider connection to wrap.</param>
        /// <param name="session">Session to record into.</param>
        /// <returns>A wrapper connection that instruments commands.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="connection"/> or <paramref name="session"/> is null.</exception>
        public static IDbConnection WithQueryWatch(this IDbConnection connection, QueryWatchSession session) {
            return _instrumenter.Wrap(connection, session);
        }
    }
}
