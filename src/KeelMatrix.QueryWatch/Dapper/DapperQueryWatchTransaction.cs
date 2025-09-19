#nullable enable
using System.Data;

namespace KeelMatrix.QueryWatch.Dapper {
    /// <summary>
    /// Delegating <see cref="IDbTransaction"/> that preserves the wrapper connection.
    /// </summary>
    public sealed class DapperQueryWatchTransaction : IDbTransaction {
        private readonly IDbTransaction _inner;
        private readonly DapperQueryWatchConnection _owner;

        public DapperQueryWatchTransaction(IDbTransaction inner, DapperQueryWatchConnection owner) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        public IDbTransaction Inner => _inner;

        public IDbConnection Connection => _owner;
        public IsolationLevel IsolationLevel => _inner.IsolationLevel;

        public void Commit() => _inner.Commit();
        public void Rollback() => _inner.Rollback();
        public void Dispose() => _inner.Dispose();
    }
}
