#nullable enable
using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace KeelMatrix.QueryWatch.Dapper {
    /// <summary>
    /// <see cref="IDbConnection"/> wrapper tailored for Dapper scenarios.
    /// </summary>
    public sealed class DapperQueryWatchConnection : IDbConnection {
        private readonly IDbConnection _inner;
        private readonly QueryWatchSession _session;

        /// <summary>
        /// Initializes a new wrapper over an inner <see cref="IDbConnection"/>.
        /// </summary>
        /// <param name="inner">Inner provider connection.</param>
        /// <param name="session">Session to record into.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/> or <paramref name="session"/> is null.</exception>
        public DapperQueryWatchConnection(IDbConnection inner, QueryWatchSession session) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Gets the inner provider connection.
        /// </summary>
        public IDbConnection Inner => _inner;

        /// <inheritdoc />
        [AllowNull]
        public string ConnectionString {
            get => _inner.ConnectionString!;
            set => _inner.ConnectionString = value;
        }

        /// <inheritdoc />
        public int ConnectionTimeout => _inner.ConnectionTimeout;

        /// <inheritdoc />
        public string Database => _inner.Database;

        /// <inheritdoc />
        public ConnectionState State => _inner.State;

        /// <inheritdoc />
        public void Open() => _inner.Open();

        /// <inheritdoc />
        public void Close() => _inner.Close();

        /// <inheritdoc />
        public void ChangeDatabase(string databaseName) => _inner.ChangeDatabase(databaseName);

        /// <inheritdoc />
        public IDbTransaction BeginTransaction() => new DapperQueryWatchTransaction(_inner.BeginTransaction(), this);

        /// <inheritdoc />
        public IDbTransaction BeginTransaction(IsolationLevel il) => new DapperQueryWatchTransaction(_inner.BeginTransaction(il), this);

        /// <inheritdoc />
        public IDbCommand CreateCommand() => new DapperQueryWatchCommand(_inner.CreateCommand(), _session, this);

        /// <inheritdoc />
        public void Dispose() => _inner.Dispose();
    }
}
