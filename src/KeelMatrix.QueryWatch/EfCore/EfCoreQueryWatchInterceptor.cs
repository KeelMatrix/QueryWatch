#if NET8_0
#nullable enable
using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KeelMatrix.QueryWatch.EfCore
{
    /// <summary>
    /// EF Core DbCommand interceptor that records command durations into a <see cref="QueryWatchSession"/>.
    /// </summary>
    public sealed class EfCoreQueryWatchInterceptor : DbCommandInterceptor
    {
        private readonly QueryWatchSession _session;

        public EfCoreQueryWatchInterceptor(QueryWatchSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        private static void Record(QueryWatchSession session, DbCommand command, long elapsedTicks)
        {
            var duration = TimeSpan.FromTicks(elapsedTicks);
            var text = command.CommandText ?? string.Empty;
            session.Record(text, duration);
        }

        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result)
        {
            Record(_session, command, eventData.Duration.Ticks);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            Record(_session, command, eventData.Duration.Ticks);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
        {
            Record(_session, command, eventData.Duration.Ticks);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
        {
            Record(_session, command, eventData.Duration.Ticks);
            return base.ScalarExecuted(command, eventData, result);
        }
    }
}
#endif
