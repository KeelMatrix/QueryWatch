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

        #region Helper plumbing

        private string ResolveTextForRecording() {
            if (_session.Options.DisableDapperTextCapture) return string.Empty;
            return _inner.CommandText ?? string.Empty;
        }

        private void RecordSuccess(TimeSpan elapsed) {
            var text = ResolveTextForRecording();
            _session.Record(text, elapsed);
        }

        private void RecordFailure(TimeSpan elapsed, Exception ex) {
            var text = ResolveTextForRecording();
            var meta = new Dictionary<string, object?>(StringComparer.Ordinal) {
                ["failed"] = true,
                ["exception"] = ex?.GetType().FullName ?? "UnknownException",
                ["provider"] = "dapper"
            };
            _session.Record(text, elapsed, meta);
        }

        #endregion

        #region IDbCommand

        [AllowNull]
        public string CommandText {
            get => _inner.CommandText;
            set => _inner.CommandText = value;
        }

        public int CommandTimeout {
            get => _inner.CommandTimeout;
            set => _inner.CommandTimeout = value;
        }

        public CommandType CommandType {
            get => _inner.CommandType;
            set => _inner.CommandType = value;
        }

        public IDbConnection? Connection {
            get => (IDbConnection?)_ownerConnection ?? _inner.Connection;
            set {
                if (value is DapperQueryWatchConnection wrapped) {
                    _inner.Connection = wrapped.Inner;
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

        public UpdateRowSource UpdatedRowSource {
            get => _inner.UpdatedRowSource;
            set => _inner.UpdatedRowSource = value;
        }

        public void Cancel() => _inner.Cancel();
        public IDbDataParameter CreateParameter() => _inner.CreateParameter();
        public void Prepare() => _inner.Prepare();

        public int ExecuteNonQuery() {
            var sw = Stopwatch.StartNew();
            try { return _inner.ExecuteNonQuery(); }
            catch (Exception ex) { sw.Stop(); RecordFailure(sw.Elapsed, ex); throw; }
            finally { if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); } }
        }

        public object? ExecuteScalar() {
            var sw = Stopwatch.StartNew();
            try { return _inner.ExecuteScalar(); }
            catch (Exception ex) { sw.Stop(); RecordFailure(sw.Elapsed, ex); throw; }
            finally { if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); } }
        }

        public IDataReader ExecuteReader() {
            var sw = Stopwatch.StartNew();
            try { return _inner.ExecuteReader(); }
            catch (Exception ex) { sw.Stop(); RecordFailure(sw.Elapsed, ex); throw; }
            finally { if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); } }
        }

        public IDataReader ExecuteReader(CommandBehavior behavior) {
            var sw = Stopwatch.StartNew();
            try { return _inner.ExecuteReader(behavior); }
            catch (Exception ex) { sw.Stop(); RecordFailure(sw.Elapsed, ex); throw; }
            finally { if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); } }
        }

        public void Dispose() => _inner.Dispose();

        #endregion
    }
}
