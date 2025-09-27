#nullable enable
using System.Data;
using System.Data.Common;

namespace KeelMatrix.QueryWatch.Ado {
    /// <summary>
    /// Delegating <see cref="DbTransaction"/> that preserves the wrapper connection.
    /// This mirrors <c>DapperQueryWatchTransaction</c> for ADO/DbConnection scenarios.
    /// </summary>
    public sealed class QueryWatchTransaction : DbTransaction {
        private readonly DbTransaction _inner;
        private readonly QueryWatchConnection _owner;

        public QueryWatchTransaction(DbTransaction inner, QueryWatchConnection owner) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>The underlying provider transaction.</summary>
        public DbTransaction Inner => _inner;

        /// <summary>Return the wrapping connection so Dapper/ADO code can pass it back.</summary>
        protected override DbConnection DbConnection => _owner;

        public override IsolationLevel IsolationLevel => _inner.IsolationLevel;

        public override void Commit() => _inner.Commit();

        public override void Rollback() => _inner.Rollback();

        protected override void Dispose(bool disposing) {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
