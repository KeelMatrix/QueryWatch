// Copyright (c) KeelMatrix

using System.Data;

namespace KeelMatrix.QueryWatch.Infrastructure.Dapper {
    /// <summary>
    /// Delegating <see cref="IDbTransaction"/> that preserves the wrapper connection.
    /// </summary>
    internal sealed class InstrumentedIdbTransaction : IDbTransaction {
        private readonly InstrumentedIdbConnection _owner;

        /// <summary>
        /// Initializes a new wrapper over an inner <see cref="IDbTransaction"/>.
        /// </summary>
        /// <param name="inner">Inner provider transaction.</param>
        /// <param name="owner">Wrapper connection that owns this transaction.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/> or <paramref name="owner"/> is null.</exception>
        public InstrumentedIdbTransaction(IDbTransaction inner, InstrumentedIdbConnection owner) {
            Inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        }

        /// <summary>
        /// Gets the inner provider transaction.
        /// </summary>
        public IDbTransaction Inner { get; }

        /// <inheritdoc />
        public IDbConnection Connection => _owner;

        /// <inheritdoc />
        public IsolationLevel IsolationLevel => Inner.IsolationLevel;

        /// <inheritdoc />
        public void Commit() => Inner.Commit();

        /// <inheritdoc />
        public void Rollback() => Inner.Rollback();

        /// <inheritdoc />
        public void Dispose() => Inner.Dispose();
    }
}
