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

        /// <summary>
        /// Initializes a new wrapper over an inner <see cref="IDbCommand"/>.
        /// </summary>
        /// <param name="inner">Inner provider command.</param>
        /// <param name="session">Session to record into.</param>
        /// <param name="ownerConnection">Optional wrapper connection to surface via <see cref="IDbCommand.Connection"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/> or <paramref name="session"/> is null.</exception>
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

        /// <inheritdoc />
        [AllowNull]
        public string CommandText {
            get => _inner.CommandText;
            set => _inner.CommandText = value;
        }

        /// <inheritdoc />
        public int CommandTimeout {
            get => _inner.CommandTimeout;
            set => _inner.CommandTimeout = value;
        }

        /// <inheritdoc />
        public CommandType CommandType {
            get => _inner.CommandType;
            set => _inner.CommandType = value;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IDataParameterCollection Parameters => _inner.Parameters;

        /// <inheritdoc />
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

        /// <inheritdoc />
        public UpdateRowSource UpdatedRowSource {
            get => _inner.UpdatedRowSource;
            set => _inner.UpdatedRowSource = value;
        }

        /// <inheritdoc />
        public void Cancel() => _inner.Cancel();

        /// <inheritdoc />
        public IDbDataParameter CreateParameter() => _inner.CreateParameter();

        /// <inheritdoc />
        public void Prepare() => _inner.Prepare();

        /// <inheritdoc />
        public int ExecuteNonQuery() {
            var sw = Stopwatch.StartNew();
            try { return _inner.ExecuteNonQuery(); }
            catch (Exception ex) { sw.Stop(); RecordFailure(sw.Elapsed, ex); throw; }
            finally { if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); } }
        }

        /// <inheritdoc />
        public object? ExecuteScalar() {
            var sw = Stopwatch.StartNew();
            try { return _inner.ExecuteScalar(); }
            catch (Exception ex) { sw.Stop(); RecordFailure(sw.Elapsed, ex); throw; }
            finally { if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); } }
        }

        /// <inheritdoc />
        public IDataReader ExecuteReader() {
            var sw = Stopwatch.StartNew();
            try { return _inner.ExecuteReader(); }
            catch (Exception ex) { sw.Stop(); RecordFailure(sw.Elapsed, ex); throw; }
            finally { if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); } }
        }

        /// <inheritdoc />
        public IDataReader ExecuteReader(CommandBehavior behavior) {
            var sw = Stopwatch.StartNew();
            try { return _inner.ExecuteReader(behavior); }
            catch (Exception ex) { sw.Stop(); RecordFailure(sw.Elapsed, ex); throw; }
            finally { if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); } }
        }

        /// <inheritdoc />
        public void Dispose() => _inner.Dispose();

        #endregion
    }
}
