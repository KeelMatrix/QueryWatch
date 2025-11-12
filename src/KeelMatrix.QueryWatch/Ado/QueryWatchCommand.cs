// Copyright (c) KeelMatrix

using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace KeelMatrix.QueryWatch.Ado {
    /// <summary>
    /// Delegating <see cref="DbCommand"/> that measures execution and records into a session.
    /// </summary>
    public sealed class QueryWatchCommand : DbCommand {
        private readonly DbCommand _inner;
        private readonly QueryWatchSession _session;

        // Keep track of the wrapper connection (so getters surface the wrapper instance).
        private QueryWatchConnection? _wrapperConnection;
        // Keep track of a wrapper transaction so we can track readers for draining before Commit().
        private QueryWatchTransaction? _wrapperTransaction;

        /// <summary>
        /// Initializes a new wrapper over an inner <see cref="DbCommand"/>.
        /// </summary>
        /// <param name="inner">Inner provider command to delegate to.</param>
        /// <param name="session">Session to record into.</param>
        /// <param name="wrapperConnection">Optional wrapper connection to surface via <see cref="DbCommand.Connection"/>.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="inner"/> or <paramref name="session"/> is null.</exception>
        public QueryWatchCommand(DbCommand inner, QueryWatchSession session, DbConnection? wrapperConnection = null) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            _wrapperConnection = wrapperConnection as QueryWatchConnection;
        }

        private sealed class CompositeDisposer : IDisposable {
            private readonly CancellationTokenSource _cts;
            private readonly IDisposable _reg;
            public CompositeDisposer(CancellationTokenSource cts, IDisposable reg) { _cts = cts; _reg = reg; }
            public bool IsCancelled => _cts.IsCancellationRequested;
            public void Dispose() { _reg.Dispose(); _cts.Dispose(); }
        }

        #region Property delegation

        /// <inheritdoc />
        [AllowNull]
        public override string CommandText {
            get => _inner.CommandText ?? string.Empty;
            set => _inner.CommandText = value;
        }

        /// <inheritdoc />
        public override int CommandTimeout {
            get => _inner.CommandTimeout;
            set => _inner.CommandTimeout = value;
        }

        /// <inheritdoc />
        public override CommandType CommandType {
            get => _inner.CommandType;
            set => _inner.CommandType = value;
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        protected override DbParameterCollection DbParameterCollection => _inner.Parameters;

        /// <inheritdoc />
        protected override DbTransaction? DbTransaction {
            get {
                // Prefer the wrapper if we have it and it still matches the inner transaction.
                return _wrapperTransaction is not null && ReferenceEquals(_inner.Transaction, _wrapperTransaction.Inner)
                    ? _wrapperTransaction
                    : _inner.Transaction;
            }
            set {
                if (value is QueryWatchTransaction wrapped) {
                    _inner.Transaction = wrapped.Inner;
                    _wrapperTransaction = wrapped;
                }
                else {
                    _inner.Transaction = value;
                    _wrapperTransaction = null;
                }
            }
        }

        /// <inheritdoc />
        public override bool DesignTimeVisible {
            get => _inner.DesignTimeVisible;
            set => _inner.DesignTimeVisible = value;
        }

        /// <inheritdoc />
        public override UpdateRowSource UpdatedRowSource {
            get => _inner.UpdatedRowSource;
            set => _inner.UpdatedRowSource = value;
        }

        #endregion

        #region Core helpers

        private string ResolveTextForRecording() {
            // Adapter-specific fast disable gate.
            return _session.Options.DisableAdoTextCapture ? string.Empty : _inner.CommandText ?? string.Empty;
        }

        private IReadOnlyDictionary<string, object?>? CaptureParameterShapeIfEnabled() {
            if (!_session.Options.CaptureParameterShape) return null;
            // Reuse internal helper; value object + array lives under "parameters" meta key.
            return AdoParameterMetadata.TryCapture(_inner);
        }

        private void RecordSuccess(TimeSpan elapsed) {
            string text = ResolveTextForRecording();
            IReadOnlyDictionary<string, object?>? meta = CaptureParameterShapeIfEnabled();
            _session.Record(text, elapsed, meta);
        }

        private void RecordFailure(TimeSpan elapsed, Exception ex) {
            string text = ResolveTextForRecording();

            // Always include normalized failure envelope.
            var meta = new Dictionary<string, object?>(StringComparer.Ordinal) {
                ["failed"] = true,
                ["exception"] = ex?.GetType().FullName ?? "UnknownException",
                ["provider"] = "ado",
            };

            // Optionally append parameter shapes.
            IReadOnlyDictionary<string, object?>? shapes = CaptureParameterShapeIfEnabled();
            if (shapes is not null) {
                foreach (KeyValuePair<string, object?> kv in shapes) meta[kv.Key] = kv.Value;
            }

            _session.Record(text, elapsed, meta);
        }

        private static Exception NormalizeCancellation(Exception ex, CancellationToken token) {
            if (!token.IsCancellationRequested) return ex;

            // SQL Server
            string full = ex.GetType()?.FullName ?? string.Empty;
            if (full.IndexOf("SqlClient.SqlException", StringComparison.OrdinalIgnoreCase) >= 0)
                return new OperationCanceledException("Command was cancelled.", ex, token);

            // Npgsql: PostgresException with SqlState 57014 (query_canceled)
            string? typeName = ex.GetType().FullName ?? "";
            if ((typeName ?? string.Empty).IndexOf("Npgsql.PostgresException", StringComparison.OrdinalIgnoreCase) >= 0) {
                try {
                    PropertyInfo? sqlStateProp = ex.GetType().GetProperty("SqlState");
                    string? sqlState = sqlStateProp?.GetValue(ex) as string;
                    if (string.Equals(sqlState, "57014", StringComparison.Ordinal))
                        return new OperationCanceledException("Command was cancelled.", ex, token);
                }
                catch { /* ignore reflection issues */ }
            }

            // MySQL: MySqlConnector.MySqlException often thrown, or TaskCanceled/Timeout
            if ((typeName ?? string.Empty).IndexOf("MySqlConnector.MySqlException", StringComparison.OrdinalIgnoreCase) >= 0 ||
                ex is TaskCanceledException || ex is TimeoutException)
                return new OperationCanceledException("Command was cancelled.", ex, token);

            // Fallback: if token says cancelled, prefer OCE so callers can handle uniformly.
            return new OperationCanceledException("Command was cancelled.", ex, token);
        }

        private static bool IsMySql(DbCommand cmd)
            => cmd.GetType().Namespace?.StartsWith("MySqlConnector", StringComparison.Ordinal) == true;

        /// <summary>Registers a callback that calls provider Cancel() when token is cancelled.</summary>
        private static CancellationTokenRegistration RegisterCancelOnToken(DbCommand cmd, CancellationToken token) {
            if (!token.CanBeCanceled)
                return default;
            try {
                return token.Register(static state => {
                    try { ((DbCommand)state!).Cancel(); } catch { /* best-effort */ }
                }, cmd);
            }
            catch { return default; }
        }

        /// <summary>For providers that may not enforce CommandTimeout deterministically (MySQL),
        /// schedule a Cancel() at timeout expiry (seconds). No effect for zero/infinite timeout.</summary>
        private static CompositeDisposer? BeginTimeoutCancelIfNeeded(DbCommand cmd) {
            try {
                if (!IsMySql(cmd))
                    return null;               // scope to MySQL only
                int seconds = cmd.CommandTimeout;
                if (seconds <= 0)
                    return null;                // 0 == infinite
                var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
                CancellationTokenRegistration reg = cts.Token.Register(static state => {
                    try { ((DbCommand)state!).Cancel(); } catch { /* best-effort */ }
                }, cmd);
                return new CompositeDisposer(cts, reg);
            }
            catch { return null; }
        }

        #endregion

        #region Execute overrides (sync)

        /// <inheritdoc />
        public override int ExecuteNonQuery() {
            var sw = Stopwatch.StartNew();
            IDisposable? tmo = BeginTimeoutCancelIfNeeded(_inner);
            try {
                int result = _inner.ExecuteNonQuery();
                return tmo is CompositeDisposer s && s.IsCancelled
                    ? throw new TimeoutException("CommandTimeout elapsed and the command was cancelled.")
                    : result;
            }
            catch (Exception ex) {
                sw.Stop();
                RecordFailure(sw.Elapsed, ex);
                throw;
            }
            finally {
                tmo?.Dispose();
                if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); }
            }
        }

        /// <inheritdoc />
        public override object? ExecuteScalar() {
            var sw = Stopwatch.StartNew();
            IDisposable? tmo = BeginTimeoutCancelIfNeeded(_inner);
            try {
                object? result = _inner.ExecuteScalar();
                return tmo is CompositeDisposer s && s.IsCancelled
                    ? throw new TimeoutException("CommandTimeout elapsed and the command was cancelled.")
                    : result;
            }
            catch (Exception ex) {
                sw.Stop();
                RecordFailure(sw.Elapsed, ex);
                throw;
            }
            finally {
                tmo?.Dispose();
                if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); }
            }
        }

        /// <inheritdoc />
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) {
            var sw = Stopwatch.StartNew();
            IDisposable? tmo = BeginTimeoutCancelIfNeeded(_inner);
            try {
                DbDataReader r = _inner.ExecuteReader(behavior);
                if (tmo is CompositeDisposer s && s.IsCancelled) {
                    r.Dispose();
                    throw new TimeoutException("CommandTimeout elapsed and the command was cancelled.");
                }
                QueryWatchTransaction? tx = _wrapperTransaction;
                tx?.TrackReader(r);
                return new QueryWatchDataReader(r, tx);
            }
            catch (Exception ex) {
                sw.Stop();
                RecordFailure(sw.Elapsed, ex);
                throw;
            }
            finally {
                tmo?.Dispose();
                if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); }
            }
        }

        #endregion

        #region Execute overrides (async)

        /// <inheritdoc />
        public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) {
            return ExecuteNonQueryAsyncCore(cancellationToken);
        }

        private async Task<int> ExecuteNonQueryAsyncCore(CancellationToken token) {
            var sw = Stopwatch.StartNew();
            CancellationTokenRegistration reg = default;
            try {
                reg = RegisterCancelOnToken(_inner, token);
                int result = await _inner.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                return IsMySql(_inner) && token.IsCancellationRequested
                    ? throw new OperationCanceledException("Command was cancelled.", token)
                    : result;
            }
            catch (Exception ex) {
                sw.Stop();
                RecordFailure(sw.Elapsed, ex);
                throw NormalizeCancellation(ex, token);
            }
            finally {
#if NET8_0_OR_GREATER
                await reg.DisposeAsync();
#else
                reg.Dispose();
#endif
                if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); }
            }
        }

        /// <inheritdoc />
        public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) {
            return ExecuteScalarAsyncCore(cancellationToken);
        }

        private async Task<object?> ExecuteScalarAsyncCore(CancellationToken token) {
            var sw = Stopwatch.StartNew();
            CancellationTokenRegistration reg = default;
            try {
                reg = RegisterCancelOnToken(_inner, token);
                object? obj = await _inner.ExecuteScalarAsync(token).ConfigureAwait(false);
                return IsMySql(_inner) && token.IsCancellationRequested
                    ? throw new OperationCanceledException("Command was cancelled.", token)
                    : obj;
            }
            catch (Exception ex) {
                sw.Stop();
                RecordFailure(sw.Elapsed, ex);
                throw NormalizeCancellation(ex, token);
            }
            finally {
#if NET8_0_OR_GREATER
                await reg.DisposeAsync();
#else
                reg.Dispose();
#endif
                if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); }
            }
        }

        /// <inheritdoc />
        protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) {
            var sw = Stopwatch.StartNew();
            CancellationTokenRegistration reg = default;
            try {
                reg = RegisterCancelOnToken(_inner, cancellationToken);
                DbDataReader r = await _inner.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
                if (IsMySql(_inner) && cancellationToken.IsCancellationRequested) {
#if NET8_0_OR_GREATER
                    await r.DisposeAsync();
#else
                    r.Dispose();
#endif
                    throw new OperationCanceledException("Command was cancelled.", cancellationToken);
                }
                QueryWatchTransaction? tx = _wrapperTransaction;
                tx?.TrackReader(r);
                return new QueryWatchDataReader(r, tx);
            }
            catch (Exception ex) {
                sw.Stop();
                RecordFailure(sw.Elapsed, ex);
                throw NormalizeCancellation(ex, cancellationToken);
            }
            finally {
#if NET8_0_OR_GREATER
                await reg.DisposeAsync();
#else
                reg.Dispose();
#endif
                if (sw.IsRunning) { sw.Stop(); RecordSuccess(sw.Elapsed); }
            }
        }

        #endregion

        #region Misc

        /// <inheritdoc />
        public override void Cancel() => _inner.Cancel();

        /// <inheritdoc />
        public override void Prepare() => _inner.Prepare();

        /// <inheritdoc />
        protected override DbParameter CreateDbParameter() => _inner.CreateParameter();

        #endregion
    }
}
