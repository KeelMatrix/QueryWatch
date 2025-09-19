#nullable enable
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace KeelMatrix.QueryWatch.Dapper {
    /// <summary>
    /// IDbConnection wrapper tailored for Dapper scenarios.
    /// It mirrors <c>QueryWatchConnection</c> semantics but does not require the inner connection
    /// to derive from <see cref="System.Data.Common.DbConnection"/>. This allows interception for
    /// providers that only implement <see cref="IDbConnection"/>.
    /// </summary>
    public sealed class DapperQueryWatchConnection : IDbConnection {
        private readonly IDbConnection _inner;
        private readonly QueryWatchSession _session;

        public DapperQueryWatchConnection(IDbConnection inner, QueryWatchSession session) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>The wrapped provider connection.</summary>
        public IDbConnection Inner => _inner;

        [AllowNull]
        public string ConnectionString {
            get => _inner.ConnectionString!;
            set => _inner.ConnectionString = value;
        }

        public int ConnectionTimeout => _inner.ConnectionTimeout;
        public string Database => _inner.Database;
        public ConnectionState State => _inner.State;

        public void Open() => _inner.Open();
        public void Close() => _inner.Close();
        public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

        public IDbTransaction BeginTransaction() => new DapperQueryWatchTransaction(_inner.BeginTransaction(), this);
        public IDbTransaction BeginTransaction(IsolationLevel il) => new DapperQueryWatchTransaction(_inner.BeginTransaction(il), this);

        public IDbCommand CreateCommand() => new DapperQueryWatchCommand(_inner.CreateCommand(), _session, this);

        public void Dispose() => _inner.Dispose();
    }
}
