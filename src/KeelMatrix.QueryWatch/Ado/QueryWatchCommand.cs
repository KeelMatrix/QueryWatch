#nullable enable
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

        // Keep track of the wrapper connection (so getters surface the wrapper instance).
        private QueryWatchConnection? _wrapperConnection;

        public QueryWatchCommand(DbCommand inner, QueryWatchSession session, DbConnection? wrapperConnection = null) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _wrapperConnection = wrapperConnection as QueryWatchConnection;
        }

        #region Property delegation

        [AllowNull]
        public override string CommandText {
            get => _inner.CommandText ?? string.Empty;
            set => _inner.CommandText = value;
        }

        public override int CommandTimeout {
            get => _inner.CommandTimeout;
            set => _inner.CommandTimeout = value;
        }

        public override CommandType CommandType {
            get => _inner.CommandType;
            set => _inner.CommandType = value;
        }

        protected override DbConnection? DbConnection {
            get => _wrapperConnection ?? _inner.Connection;
            set {
                if (value is QueryWatchConnection wrapped) {
                    _inner.Connection = wrapped.Inner;
                    _wrapperConnection = wrapped;
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
            set {
                if (value is QueryWatchTransaction wrapped) {
                    _inner.Transaction = wrapped.Inner;
                }
                else {
                    _inner.Transaction = value;
                }
            }
        }

        public override bool DesignTimeVisible {
            get => _inner.DesignTimeVisible;
            set => _inner.DesignTimeVisible = value;
        }

        public override UpdateRowSource UpdatedRowSource {
            get => _inner.UpdatedRowSource;
            set => _inner.UpdatedRowSource = value;
        }

        #endregion

        #region Core helpers

        private string ResolveTextForRecording() {
            // Adapter-specific fast disable gate.
            if (_session.Options.DisableAdoTextCapture) return string.Empty;
            return _inner.CommandText ?? string.Empty;
        }

        private IReadOnlyDictionary<string, object?>? CaptureParameterShapeIfEnabled() {
            if (!_session.Options.CaptureParameterShape) return null;
            // Reuse internal helper; value object + array lives under "parameters" meta key.
            return AdoParameterMetadata.TryCapture(_inner);
        }

        private void RecordSuccess(TimeSpan elapsed) {
            var text = ResolveTextForRecording();
            var meta = CaptureParameterShapeIfEnabled();
            _session.Record(text, elapsed, meta);
        }

        private void RecordFailure(TimeSpan elapsed, Exception ex) {
            var text = ResolveTextForRecording();

            // Always include normalized failure envelope.
            var meta = new Dictionary<string, object?>(StringComparer.Ordinal) {
                ["failed"] = true,
                ["exception"] = ex?.GetType().FullName ?? "UnknownException",
                ["provider"] = "ado",
            };

            // Optionally append parameter shapes.
            var shapes = CaptureParameterShapeIfEnabled();
            if (shapes is not null) {
                foreach (var kv in shapes) meta[kv.Key] = kv.Value;
            }

            _session.Record(text, elapsed, meta);
        }

        #endregion

        #region Execute overrides (sync)

        public override int ExecuteNonQuery() {
            var sw = Stopwatch.StartNew();
            try {
                return _inner.ExecuteNonQuery();
            }
            catch (Exception ex) {
                sw.Stop();
                RecordFailure(sw.Elapsed, ex);
                throw;
            }
            finally {
                if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); }
            }
        }

        public override object? ExecuteScalar() {
            var sw = Stopwatch.StartNew();
            try {
                return _inner.ExecuteScalar();
            }
            catch (Exception ex) {
                sw.Stop();
                RecordFailure(sw.Elapsed, ex);
                throw;
            }
            finally {
                if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); }
            }
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) {
            var sw = Stopwatch.StartNew();
            try {
                return _inner.ExecuteReader(behavior);
            }
            catch (Exception ex) {
                sw.Stop();
                RecordFailure(sw.Elapsed, ex);
                throw;
            }
            finally {
                if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); }
            }
        }

        #endregion

        #region Execute overrides (async)

        public override System.Threading.Tasks.Task<int> ExecuteNonQueryAsync(System.Threading.CancellationToken cancellationToken) {
            return ExecuteNonQueryAsyncCore(cancellationToken);
        }

        private async System.Threading.Tasks.Task<int> ExecuteNonQueryAsyncCore(System.Threading.CancellationToken token) {
            var sw = Stopwatch.StartNew();
            try {
                return await _inner.ExecuteNonQueryAsync(token).ConfigureAwait(false);
            }
            catch (Exception ex) {
                sw.Stop();
                RecordFailure(sw.Elapsed, ex);
                throw;
            }
            finally {
                if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); }
            }
        }

        public override System.Threading.Tasks.Task<object?> ExecuteScalarAsync(System.Threading.CancellationToken cancellationToken) {
            return ExecuteScalarAsyncCore(cancellationToken);
        }

        private async System.Threading.Tasks.Task<object?> ExecuteScalarAsyncCore(System.Threading.CancellationToken token) {
            var sw = Stopwatch.StartNew();
            try {
                return await _inner.ExecuteScalarAsync(token).ConfigureAwait(false);
            }
            catch (Exception ex) {
                sw.Stop();
                RecordFailure(sw.Elapsed, ex);
                throw;
            }
            finally {
                if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); }
            }
        }

        protected override System.Threading.Tasks.Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, System.Threading.CancellationToken cancellationToken) {
            return ExecuteDbDataReaderAsyncCore(behavior, cancellationToken);
        }

        private async System.Threading.Tasks.Task<DbDataReader> ExecuteDbDataReaderAsyncCore(CommandBehavior behavior, System.Threading.CancellationToken token) {
            var sw = Stopwatch.StartNew();
            try {
                return await _inner.ExecuteReaderAsync(behavior, token).ConfigureAwait(false);
            }
            catch (Exception ex) {
                sw.Stop();
                RecordFailure(sw.Elapsed, ex);
                throw;
            }
            finally {
                if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); }
            }
        }

        #endregion

        #region Boilerplate

        public override void Cancel() => _inner.Cancel();
        public override void Prepare() => _inner.Prepare();
        protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

        protected override void Dispose(bool disposing) {
            if (disposing) _inner.Dispose();
            base.Dispose(disposing);
        }

        #endregion
    }
}
