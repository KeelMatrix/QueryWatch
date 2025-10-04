// Copyright (c) KeelMatrix
#nullable enable
using System.Data.Common;
using KeelMatrix.QueryWatch.Ado;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KeelMatrix.QueryWatch.EfCore {
    /// <summary>
    /// EF Core <see cref="DbCommandInterceptor"/> that records command timings and metadata
    /// into a <see cref="QueryWatchSession"/> (sync and async; reader/scalar/non-query; failures included).
    /// </summary>
    public sealed class EfCoreQueryWatchInterceptor : DbCommandInterceptor {
        private readonly QueryWatchSession _session;

        /// <summary>
        /// Initializes a new interceptor that records EF Core command events into the specified session.
        /// </summary>
        /// <param name="session">The target <see cref="QueryWatchSession"/>; must not be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="session"/> is <c>null</c>.</exception>
        public EfCoreQueryWatchInterceptor(QueryWatchSession session) {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        private string ResolveTextForRecording(DbCommand command) {
            if (_session.Options.DisableEfCoreTextCapture) return string.Empty;
            return command.CommandText ?? string.Empty;
        }

        private IReadOnlyDictionary<string, object?>? CaptureParameterShapeIfEnabled(DbCommand command) {
            if (!_session.Options.CaptureParameterShape) return null;
            return AdoParameterMetadata.TryCapture(command);
        }

        private void Record(QueryWatchSession session, DbCommand command, long elapsedTicks) {
            var duration = TimeSpan.FromTicks(elapsedTicks);
            var text = ResolveTextForRecording(command);
            var meta = CaptureParameterShapeIfEnabled(command);
            session.Record(text, duration, meta);
        }

        private void RecordFailed(QueryWatchSession session, DbCommand command, long elapsedTicks, CommandErrorEventData error) {
            var duration = TimeSpan.FromTicks(elapsedTicks);
            var text = ResolveTextForRecording(command);

            var meta = new Dictionary<string, object?>(StringComparer.Ordinal) {
                ["failed"] = true,
                ["exception"] = error.Exception?.GetType().FullName ?? "UnknownException",
                ["provider"] = "efcore"
            };

            var shapes = CaptureParameterShapeIfEnabled(command);
            if (shapes is not null) {
                foreach (var kv in shapes) meta[kv.Key] = kv.Value;
            }

            session.Record(text, duration, meta);
        }

        /// <summary>
        /// Records the completed reader command and returns the provider's <see cref="DbDataReader"/> unchanged.
        /// </summary>
        /// <param name="command">The executed <see cref="DbCommand"/>.</param>
        /// <param name="eventData">EF Core diagnostics describing the execution.</param>
        /// <param name="result">The reader returned by the provider.</param>
        /// <returns>The same <paramref name="result"/> passed in.</returns>
        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result) {
            Record(_session, command, eventData.Duration.Ticks);
            return base.ReaderExecuted(command, eventData, result);
        }

        /// <summary>
        /// Records the completed async reader command and returns the provider's <see cref="DbDataReader"/> unchanged.
        /// </summary>
        /// <param name="command">The executed <see cref="DbCommand"/>.</param>
        /// <param name="eventData">EF Core diagnostics describing the execution.</param>
        /// <param name="result">The reader returned by the provider.</param>
        /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that yields the same <paramref name="result"/>.</returns>
        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default) {
            Record(_session, command, eventData.Duration.Ticks);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <summary>
        /// Records the completed non-query command and returns the provider result unchanged.
        /// </summary>
        /// <param name="command">The executed <see cref="DbCommand"/>.</param>
        /// <param name="eventData">EF Core diagnostics describing the execution.</param>
        /// <param name="result">The number of rows affected.</param>
        /// <returns>The same <paramref name="result"/> value.</returns>
        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result) {
            Record(_session, command, eventData.Duration.Ticks);
            return base.NonQueryExecuted(command, eventData, result);
        }

        /// <summary>
        /// Records the completed async non-query command and returns the provider result unchanged.
        /// </summary>
        /// <param name="command">The executed <see cref="DbCommand"/>.</param>
        /// <param name="eventData">EF Core diagnostics describing the execution.</param>
        /// <param name="result">The number of rows affected.</param>
        /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that yields the same <paramref name="result"/>.</returns>
        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default) {
            Record(_session, command, eventData.Duration.Ticks);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <summary>
        /// Records the completed scalar command and returns the provider result unchanged.
        /// </summary>
        /// <param name="command">The executed <see cref="DbCommand"/>.</param>
        /// <param name="eventData">EF Core diagnostics describing the execution.</param>
        /// <param name="result">The scalar value returned by the provider.</param>
        /// <returns>The same <paramref name="result"/> value.</returns>
        public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result) {
            Record(_session, command, eventData.Duration.Ticks);
            return base.ScalarExecuted(command, eventData, result);
        }

        /// <summary>
        /// Records the completed async scalar command and returns the provider result unchanged.
        /// </summary>
        /// <param name="command">The executed <see cref="DbCommand"/>.</param>
        /// <param name="eventData">EF Core diagnostics describing the execution.</param>
        /// <param name="result">The scalar value returned by the provider.</param>
        /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
        /// <returns>A <see cref="ValueTask{TResult}"/> that yields the same <paramref name="result"/>.</returns>
        public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default) {
            Record(_session, command, eventData.Duration.Ticks);
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        /// <summary>
        /// Records a failed command execution (including normalized failure metadata) and forwards to base.
        /// </summary>
        /// <param name="command">The <see cref="DbCommand"/> that failed.</param>
        /// <param name="eventData">EF Core diagnostics for the failure.</param>
        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData) {
            RecordFailed(_session, command, eventData.Duration.Ticks, eventData);
            base.CommandFailed(command, eventData);
        }

        /// <summary>
        /// Records a failed async command execution (including normalized failure metadata) and forwards to base.
        /// </summary>
        /// <param name="command">The <see cref="DbCommand"/> that failed.</param>
        /// <param name="eventData">EF Core diagnostics for the failure.</param>
        /// <param name="cancellationToken">A token to observe while awaiting the operation.</param>
        /// <returns>A task that completes when the interceptor pipeline completes.</returns>
        public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default) {
            RecordFailed(_session, command, eventData.Duration.Ticks, eventData);
            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }
    }
}
