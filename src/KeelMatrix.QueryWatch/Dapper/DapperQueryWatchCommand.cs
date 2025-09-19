#nullable enable
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace KeelMatrix.QueryWatch.Dapper {
    /// <summary>
    /// Delegating <see cref="IDbCommand"/> that measures execution and records into a session.
    /// </summary>
    public sealed class DapperQueryWatchCommand : IDbCommand {
        private readonly IDbCommand _inner;
        private readonly QueryWatchSession _session;
        private readonly DapperQueryWatchConnection? _ownerConnection;

        public DapperQueryWatchCommand(IDbCommand inner, QueryWatchSession session, DapperQueryWatchConnection? ownerConnection = null) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _ownerConnection = ownerConnection;
        }

        [AllowNull]
        public string CommandText {
            get => _inner.CommandText!;
            set => _inner.CommandText = value ?? string.Empty;
        }
        public int CommandTimeout { get => _inner.CommandTimeout; set => _inner.CommandTimeout = value; }
        public CommandType CommandType { get => _inner.CommandType; set => _inner.CommandType = value; }
        public UpdateRowSource UpdatedRowSource { get => _inner.UpdatedRowSource; set => _inner.UpdatedRowSource = value; }

        public IDbConnection? Connection {
            get => _ownerConnection ?? _inner.Connection;
            set {
                if (value is DapperQueryWatchConnection wrap) {
                    _inner.Connection = wrap.Inner;
                }
                else {
                    _inner.Connection = value;
                }
            }
        }

        public IDataParameterCollection Parameters => _inner.Parameters;

        public IDbTransaction? Transaction {
            get => _inner.Transaction;
            set {
                if (value is DapperQueryWatchTransaction tx) {
                    _inner.Transaction = tx.Inner;
                }
                else {
                    _inner.Transaction = value;
                }
            }
        }

        public void Cancel() => _inner.Cancel();
        public IDbDataParameter CreateParameter() => _inner.CreateParameter();
        public void Prepare() => _inner.Prepare();

        private void Record(TimeSpan elapsed) => _session.Record(_inner.CommandText ?? string.Empty, elapsed);

        public int ExecuteNonQuery() {
            var sw = Stopwatch.StartNew();
            try { return _inner.ExecuteNonQuery(); }
            finally { sw.Stop(); Record(sw.Elapsed); }
        }

        public object? ExecuteScalar() {
            var sw = Stopwatch.StartNew();
            try { return _inner.ExecuteScalar(); }
            finally { sw.Stop(); Record(sw.Elapsed); }
        }

        public IDataReader ExecuteReader() {
            var sw = Stopwatch.StartNew();
            try { return _inner.ExecuteReader(); }
            finally { sw.Stop(); Record(sw.Elapsed); }
        }

        public IDataReader ExecuteReader(CommandBehavior behavior) {
            var sw = Stopwatch.StartNew();
            try { return _inner.ExecuteReader(behavior); }
            finally { sw.Stop(); Record(sw.Elapsed); }
        }

        public void Dispose() => _inner.Dispose();
    }
}
