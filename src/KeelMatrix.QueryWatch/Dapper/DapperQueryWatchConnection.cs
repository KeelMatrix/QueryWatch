using System.Data;
using System.Diagnostics.CodeAnalysis;

namespace KeelMatrix.QueryWatch.Dapper {
    /// <summary>
    /// <see cref="IDbConnection"/> wrapper tailored for Dapper scenarios.
    /// </summary>
    public sealed class DapperQueryWatchConnection : IDbConnection {
        private readonly QueryWatchSession _session;

        /// <summary>
        /// Initializes a new wrapper over an inner <see cref="IDbConnection"/>.
        /// </summary>
        /// <param name="inner">Inner provider connection.</param>
        /// <param name="session">Session to record into.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/> or <paramref name="session"/> is null.</exception>
        public DapperQueryWatchConnection(IDbConnection inner, QueryWatchSession session) {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Gets the inner provider connection.
        /// </summary>
        public IDbConnection Inner { get; }

        /// <inheritdoc />
        [AllowNull]
        public string ConnectionString {
            get => Inner.ConnectionString!;
            set => Inner.ConnectionString = value;
        }

        /// <inheritdoc />
        public int ConnectionTimeout => Inner.ConnectionTimeout;

        /// <inheritdoc />
        public string Database => Inner.Database;

        /// <inheritdoc />
        public ConnectionState State => Inner.State;

        /// <inheritdoc />
        public void Open() => Inner.Open();

        /// <inheritdoc />
        public void Close() => Inner.Close();

        /// <inheritdoc />
        public void ChangeDatabase(string databaseName) => Inner.ChangeDatabase(databaseName);

        /// <inheritdoc />
        public IDbTransaction BeginTransaction() => new DapperQueryWatchTransaction(Inner.BeginTransaction(), this);

        /// <inheritdoc />
        public IDbTransaction BeginTransaction(IsolationLevel il) => new DapperQueryWatchTransaction(Inner.BeginTransaction(il), this);

        /// <inheritdoc />
        public IDbCommand CreateCommand() => new DapperQueryWatchCommand(Inner.CreateCommand(), _session, this);

        /// <inheritdoc />
        public void Dispose() => Inner.Dispose();
    }
}
