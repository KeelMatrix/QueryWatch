// Copyright (c) KeelMatrix
#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace KeelMatrix.QueryWatch.Ado {
    /// <summary>
    /// Delegating <see cref="DbTransaction"/> that preserves the wrapper connection
    /// and ensures multi-result <see cref="DbDataReader"/>s are closed before commit.
    /// </summary>
    public sealed class QueryWatchTransaction : DbTransaction {
        private readonly DbTransaction _inner;
        private readonly QueryWatchConnection _owner;

        // Track active readers created under this transaction so Commit can safely proceed
        // on providers that require no active results (e.g., SQL Server, MySqlConnector, Npgsql).
        private readonly Dictionary<int, WeakReference<DbDataReader>> _activeReaders = new();

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

        /// <summary>Gets the inner provider transaction.</summary>
        public DbTransaction Inner => _inner;

        /// <summary>Return the wrapping connection so Dapper/ADO code can pass it back.</summary>
        protected override DbConnection DbConnection => _owner;

        /// <inheritdoc />
        public override IsolationLevel IsolationLevel => _inner.IsolationLevel;

        // ---- Reader tracking API (called by QueryWatchCommand/QueryWatchDataReader) ----
        internal void TrackReader(DbDataReader reader) {
            if (reader is null) return;
            lock (_activeReaders) {
                _activeReaders[reader.GetHashCode()] = new WeakReference<DbDataReader>(reader);
            }
        }

        internal void UntrackReader(DbDataReader reader) {
            if (reader is null) return;
            lock (_activeReaders) {
                _activeReaders.Remove(reader.GetHashCode());
            }
        }

        private static void TryDrain(DbDataReader r) {
            try {
                if (r is null) return;
                // Consume all remaining rows & result sets, then close.
                while (true) {
                    while (r.Read()) { /* drain */ }
                    if (!r.NextResult()) break;
                }
                r.Close();
            }
            catch {
                // Best-effort: providers may throw while draining; ignore.
            }
        }

        private void EnsureNoActiveReaders() {
            // Fast path
            if (_activeReaders.Count == 0) return;

            foreach (var kv in _activeReaders.ToArray()) {
                if (kv.Value.TryGetTarget(out var r)) {
                    TryDrain(r);
                }
            }
            _activeReaders.Clear();
        }

        /// <inheritdoc />
        public override void Commit() {
            // Ensure all multi-result readers are closed before committing.
            EnsureNoActiveReaders();
            _inner.Commit();
        }

        /// <inheritdoc />
        public override void Rollback() => _inner.Rollback();

        /// <inheritdoc />
        protected override void Dispose(bool disposing) {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
