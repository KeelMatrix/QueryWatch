#nullable enable
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace KeelMatrix.QueryWatch.Ado {
    /// <summary>
    /// Wraps a provider <see cref="DbConnection"/> and returns commands that record execution into a <see cref="QueryWatchSession"/>.
    /// </summary>
    public sealed class QueryWatchConnection : DbConnection {
        private readonly QueryWatchSession _session;

        /// <summary>
        /// Initializes a new wrapper over an inner <see cref="DbConnection"/>.
        /// </summary>
        /// <param name="inner">Inner provider connection to delegate to.</param>
        /// <param name="session">Session to record into.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/> or <paramref name="session"/> is null.</exception>
        public QueryWatchConnection(DbConnection inner, QueryWatchSession session) {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>
        /// Gets the inner provider connection.
        /// </summary>
        public DbConnection Inner { get; }

        /// <inheritdoc />
        [AllowNull]
        public override string ConnectionString {
            get => Inner.ConnectionString;
            set => Inner.ConnectionString = value;
        }

        /// <inheritdoc />
        public override string Database => Inner.Database;

        /// <inheritdoc />
        public override string DataSource => Inner.DataSource;

        /// <inheritdoc />
        public override string ServerVersion => Inner.ServerVersion;

        /// <inheritdoc />
        public override ConnectionState State => Inner.State;

        /// <inheritdoc />
        public override void ChangeDatabase(string databaseName) => Inner.ChangeDatabase(databaseName);

        /// <inheritdoc />
        public override void Close() => Inner.Close();

        /// <inheritdoc />
        public override void Open() => Inner.Open();

        /// <inheritdoc />
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            => new QueryWatchTransaction(Inner.BeginTransaction(isolationLevel), this);

        /// <inheritdoc />
        protected override DbCommand CreateDbCommand()
            => new QueryWatchCommand(Inner.CreateCommand(), _session, this);

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) {
                Inner.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
