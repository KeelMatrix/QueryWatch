#nullable enable
using System.Data;
using System.Data.Common;

namespace KeelMatrix.QueryWatch.Ado {
    /// <summary>
    /// Delegating <see cref="DbTransaction"/> that preserves the wrapper connection.
    /// </summary>
    public sealed class QueryWatchTransaction : DbTransaction {
        private readonly DbTransaction _inner;
        private readonly QueryWatchConnection _owner;

        /// <summary>
        /// Initializes a new wrapper over an inner <see cref="DbTransaction"/>.
        /// </summary>
        /// <param name="inner">Inner provider transaction to delegate to.</param>
        /// <param name="owner">Wrapper connection that owns this transaction.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/> or <paramref name="owner"/> is null.</exception>
        public QueryWatchTransaction(DbTransaction inner, QueryWatchConnection owner) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// Gets the inner provider transaction.
        /// </summary>
        public DbTransaction Inner => _inner;

        /// <summary>Return the wrapping connection so Dapper/ADO code can pass it back.</summary>
        protected override DbConnection DbConnection => _owner;

        /// <inheritdoc />
        public override IsolationLevel IsolationLevel => _inner.IsolationLevel;

        /// <inheritdoc />
        public override void Commit() => _inner.Commit();

        /// <inheritdoc />
        public override void Rollback() => _inner.Rollback();

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
