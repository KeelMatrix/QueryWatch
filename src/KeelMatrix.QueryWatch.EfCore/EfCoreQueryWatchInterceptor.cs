// Copyright (c) KeelMatrix
#nullable enable
using System.Data.Common;
using KeelMatrix.QueryWatch.Ado;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KeelMatrix.QueryWatch.EfCore {
    /// <summary>
    /// EF Core DbCommand interceptor that records command durations into a <see cref="QueryWatchSession"/>.
    /// Handles sync/async Reader/Scalar/NonQuery and failure paths.
    /// </summary>
    public sealed class EfCoreQueryWatchInterceptor : DbCommandInterceptor {
        private readonly QueryWatchSession _session;

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

        public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result) {
            Record(_session, command, eventData.Duration.Ticks);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default) {
            Record(_session, command, eventData.Duration.Ticks);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result) {
            Record(_session, command, eventData.Duration.Ticks);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default) {
            Record(_session, command, eventData.Duration.Ticks);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result) {
            Record(_session, command, eventData.Duration.Ticks);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default) {
            Record(_session, command, eventData.Duration.Ticks);
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData) {
            RecordFailed(_session, command, eventData.Duration.Ticks, eventData);
            base.CommandFailed(command, eventData);
        }

        public override Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default) {
            RecordFailed(_session, command, eventData.Duration.Ticks, eventData);
            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }
    }
}
