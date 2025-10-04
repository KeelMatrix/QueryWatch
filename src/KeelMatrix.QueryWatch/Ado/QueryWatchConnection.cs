#nullable enable
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace KeelMatrix.QueryWatch.Ado {
    /// <summary>
    /// Wraps a provider <see cref="DbConnection"/> and returns commands that record execution into a <see cref="QueryWatchSession"/>.
    /// </summary>
    public sealed class QueryWatchConnection : DbConnection {
        private readonly DbConnection _inner;
        private readonly QueryWatchSession _session;

        /// <summary>
        /// Initializes a new wrapper over an inner <see cref="DbConnection"/>.
        /// </summary>
        /// <param name="inner">Inner provider connection to delegate to.</param>
        /// <param name="session">Session to record into.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/> or <paramref name="session"/> is null.</exception>
        public QueryWatchConnection(DbConnection inner, QueryWatchSession session) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Gets the inner provider connection.
        /// </summary>
        public DbConnection Inner => _inner;

        /// <inheritdoc />
        [AllowNull]
        public override string ConnectionString {
            get => _inner.ConnectionString;
            set => _inner.ConnectionString = value;
        }

        /// <inheritdoc />
        public override string Database => _inner.Database;

        /// <inheritdoc />
        public override string DataSource => _inner.DataSource;

        /// <inheritdoc />
        public override string ServerVersion => _inner.ServerVersion;

        /// <inheritdoc />
        public override ConnectionState State => _inner.State;

        /// <inheritdoc />
        public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

        /// <inheritdoc />
        public override void Close() => _inner.Close();

        /// <inheritdoc />
        public override void Open() => _inner.Open();

        /// <inheritdoc />
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => new QueryWatchTransaction(_inner.BeginTransaction(isolationLevel), this);

        /// <inheritdoc />
        protected override DbCommand CreateDbCommand()
            => new QueryWatchCommand(_inner.CreateCommand(), _session, this);

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Extension helpers for wrapping connections with QueryWatch.
    /// </summary>
    public static class QueryWatchConnectionExtensions {
        /// <summary>
        /// Wrap a <see cref="DbConnection"/> to record commands into <see cref="QueryWatchSession"/>.
        /// </summary>
        /// <param name="connection">Connection to wrap.</param>
        /// <param name="session">Session to record into.</param>
        /// <returns>A wrapper connection that instruments commands.</returns>
        public static DbConnection WithQueryWatch(this DbConnection connection, QueryWatchSession session)
            => new QueryWatchConnection(connection, session);

        /// <summary>
        /// Wrap an <see cref="IDbConnection"/> whose underlying type is a <see cref="DbConnection"/>.
        /// </summary>
        /// <param name="connection">Connection to wrap.</param>
        /// <param name="session">Session to record into.</param>
        /// <returns>A wrapper connection that instruments commands.</returns>
        /// <exception cref="NotSupportedException">Thrown when the connection type does not derive from <see cref="DbConnection"/>.</exception>
        public static IDbConnection WithQueryWatch(this IDbConnection connection, QueryWatchSession session) {
            if (connection is DbConnection db) return new QueryWatchConnection(db, session);
            throw new NotSupportedException("This provider doesn't derive from DbConnection; wrap commands manually.");
        }
    }
}

