#nullable enable
using System.Data;

namespace KeelMatrix.QueryWatch.Dapper {
    /// <summary>
    /// Delegating <see cref="IDbTransaction"/> that preserves the wrapper connection.
    /// </summary>
    public sealed class DapperQueryWatchTransaction : IDbTransaction {
        private readonly IDbTransaction _inner;
        private readonly DapperQueryWatchConnection _owner;

        /// <summary>
        /// Initializes a new wrapper over an inner <see cref="IDbTransaction"/>.
        /// </summary>
        /// <param name="inner">Inner provider transaction.</param>
        /// <param name="owner">Wrapper connection that owns this transaction.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/> or <paramref name="owner"/> is null.</exception>
        public DapperQueryWatchTransaction(IDbTransaction inner, DapperQueryWatchConnection owner) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// Gets the inner provider transaction.
        /// </summary>
        public IDbTransaction Inner => _inner;

        /// <inheritdoc />
        public IDbConnection Connection => _owner;

        /// <inheritdoc />
        public IsolationLevel IsolationLevel => _inner.IsolationLevel;

        /// <inheritdoc />
        public void Commit() => _inner.Commit();

        /// <inheritdoc />
        public void Rollback() => _inner.Rollback();

        /// <inheritdoc />
        public void Dispose() => _inner.Dispose();
    }
}
