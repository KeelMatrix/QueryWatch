#nullable enable
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace KeelMatrix.QueryWatch.Ado {
    /// <summary>
    /// Delegating <see cref="DbCommand"/> that measures execution and records into a session.
    /// </summary>
    public sealed class QueryWatchCommand : DbCommand {
        private readonly DbCommand _inner;
        private readonly QueryWatchSession _session;
        private DbConnection? _wrapperConnection; // wrapper connection, if any

        public QueryWatchCommand(DbCommand inner, QueryWatchSession session, DbConnection? wrapperConnection = null) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _wrapperConnection = wrapperConnection;
        }

        [AllowNull]
        public override string CommandText {
            get => _inner.CommandText;
            set => _inner.CommandText = value ?? string.Empty;
        }

        public override int CommandTimeout {
            get => _inner.CommandTimeout;
            set => _inner.CommandTimeout = value;
        }

        public override CommandType CommandType {
            get => _inner.CommandType;
            set => _inner.CommandType = value;
        }

        [AllowNull]
        protected override DbConnection DbConnection {
            get => _wrapperConnection ?? _inner.Connection!;
            set {
                // If the user assigns a wrapped connection, unwrap before passing to inner.
                if (value is QueryWatchConnection wrapped) {
                    _inner.Connection = wrapped.Inner;
                    _wrapperConnection = value;
                }
                else {
                    _inner.Connection = value;
                    _wrapperConnection = null;
                }
            }
        }

        protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

        protected override DbTransaction? DbTransaction {
            get => _inner.Transaction;
            set => _inner.Transaction = value;
        }

        public override bool DesignTimeVisible {
            get => _inner.DesignTimeVisible;
            set => _inner.DesignTimeVisible = value;
        }

        public override UpdateRowSource UpdatedRowSource {
            get => _inner.UpdatedRowSource;
            set => _inner.UpdatedRowSource = value;
        }

        public override void Cancel() => _inner.Cancel();

        public override void Prepare() => _inner.Prepare();

        protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

        /// <summary>
        /// Record an executed command into the session.
        /// </summary>
        private void Record(TimeSpan elapsed) {
            string text = _inner.CommandText ?? string.Empty;

            IReadOnlyDictionary<string, object?>? meta = null;
            if (_session.Options.CaptureAdoParameterMetadata) {
                meta = AdoParameterMetadata.TryCapture(_inner);
            }

            _session.Record(text, elapsed, meta);
        }

        public override int ExecuteNonQuery() {
            var sw = Stopwatch.StartNew();
            try {
                return _inner.ExecuteNonQuery();
            }
            finally {
                sw.Stop();
                Record(sw.Elapsed);
            }
        }

        public override object? ExecuteScalar() {
            var sw = Stopwatch.StartNew();
            try {
                return _inner.ExecuteScalar();
            }
            finally {
                sw.Stop();
                Record(sw.Elapsed);
            }
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) {
            var sw = Stopwatch.StartNew();
            try {
                return _inner.ExecuteReader(behavior);
            }
            finally {
                sw.Stop();
                Record(sw.Elapsed);
            }
        }

        public override System.Threading.Tasks.Task<int> ExecuteNonQueryAsync(System.Threading.CancellationToken cancellationToken) {
            var sw = Stopwatch.StartNew();
            return ExecuteAsyncCore(_inner.ExecuteNonQueryAsync, sw, cancellationToken);
        }

        public override System.Threading.Tasks.Task<object?> ExecuteScalarAsync(System.Threading.CancellationToken cancellationToken) {
            var sw = Stopwatch.StartNew();
            return ExecuteAsyncCore(_inner.ExecuteScalarAsync, sw, cancellationToken);
        }

        protected override async System.Threading.Tasks.Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, System.Threading.CancellationToken cancellationToken) {
            var sw = Stopwatch.StartNew();
            try {
                return await _inner.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
            }
            finally {
                sw.Stop();
                Record(sw.Elapsed);
            }
        }

        private async System.Threading.Tasks.Task<T> ExecuteAsyncCore<T>(Func<System.Threading.CancellationToken, System.Threading.Tasks.Task<T>> func, Stopwatch sw, System.Threading.CancellationToken token) {
            try {
                return await func(token).ConfigureAwait(false);
            }
            finally {
                sw.Stop();
                Record(sw.Elapsed);
            }
        }

        protected override void Dispose(bool disposing) {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }
    }
}
