#nullable enable
using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace KeelMatrix.QueryWatch.Ado {
    /// <summary>
    /// Wraps a provider <see cref="DbConnection"/> and returns commands that record
    /// execution into a <see cref="QueryWatchSession"/>.
    /// </summary>
    public sealed class QueryWatchConnection : DbConnection {
        private readonly DbConnection _inner;
        private readonly QueryWatchSession _session;

        public QueryWatchConnection(DbConnection inner, QueryWatchSession session) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>The wrapped provider connection.</summary>
        public DbConnection Inner => _inner;

        [AllowNull]
        public override string ConnectionString {
            get => _inner.ConnectionString;
            set => _inner.ConnectionString = value;
        }

        public override string Database => _inner.Database;
        public override string DataSource => _inner.DataSource;
        public override string ServerVersion => _inner.ServerVersion;
        public override ConnectionState State => _inner.State;

        public override void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);
        public override void Close() => _inner.Close();
        public override void Open() => _inner.Open();

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => new QueryWatchTransaction(_inner.BeginTransaction(isolationLevel), this);

        protected override DbCommand CreateDbCommand()
            => new QueryWatchCommand(_inner.CreateCommand(), _session, this);

        protected override void Dispose(bool disposing) {
            if (disposing) {
                _inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Convenience extensions to wrap existing connections.
    /// </summary>
    public static class QueryWatchConnectionExtensions {
        /// <summary>Wrap a <see cref="DbConnection"/>.</summary>
        public static DbConnection WithQueryWatch(this DbConnection connection, QueryWatchSession session)
            => new QueryWatchConnection(connection, session);

        /// <summary>
        /// Wrap an <see cref="IDbConnection"/> where the underlying type is a <see cref="DbConnection"/>.
        /// </summary>
        public static IDbConnection WithQueryWatch(this IDbConnection connection, QueryWatchSession session) {
            if (connection is DbConnection db) return new QueryWatchConnection(db, session);
            throw new NotSupportedException("This provider doesn't derive from DbConnection; wrap commands manually.");
        }
    }
}

