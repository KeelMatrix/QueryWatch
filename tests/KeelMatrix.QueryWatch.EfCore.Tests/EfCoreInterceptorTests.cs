// Copyright (c) KeelMatrix
#nullable enable
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace KeelMatrix.QueryWatch.EfCore.Tests {
    public sealed class EfCoreInterceptorTests {
        [Fact]
        public void ReaderExecuted_records_event() {
            using var connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using var session = new QueryWatchSession();
            var options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (var ctx = new TestDbContext(options)) {
                // A simple SELECT using the EF query pipeline
                _ = ctx.Things.Where(t => t.Id > 0).ToList();
            }

            var report = session.Stop();
            report.TotalQueries.Should().BeGreaterThan(0);

            // We expect at least one SELECT against the Things table
            report.Events.Should().Contain(e =>
                e.CommandText.Contains("FROM", StringComparison.OrdinalIgnoreCase) &&
                e.CommandText.Contains("Things", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ReaderExecutedAsync_records_event() {
            using var connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using var session = new QueryWatchSession();
            var options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            await using (var ctx = new TestDbContext(options)) {
                // Async SELECT
                _ = await ctx.Things.Where(t => t.Id >= 0).ToListAsync();
            }

            var report = session.Stop();
            report.TotalQueries.Should().BeGreaterThan(0);
            report.Events.Should().Contain(e =>
                e.CommandText.Contains("FROM", StringComparison.OrdinalIgnoreCase) &&
                e.CommandText.Contains("Things", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void NonQueryExecuted_records_event() {
            using var connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using var session = new QueryWatchSession();
            var options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (var ctx = new TestDbContext(options)) {
                // Execute a raw non-query (UPDATE). It will run even if the table is empty.
                var affected = ctx.Database.ExecuteSqlRaw("UPDATE Things SET Name = Name");
                affected.Should().BeGreaterThanOrEqualTo(0);
            }

            var report = session.Stop();
            report.TotalQueries.Should().BeGreaterThan(0);
            report.Events.Should().Contain(e =>
                e.CommandText.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ScalarExecuted_records_event() {
            using var connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using var session = new QueryWatchSession();
            var options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (var ctx = new TestDbContext(options)) {
                // Use EF Core relational services to execute a scalar command
                var cmdBuilderFactory = ctx.GetService<IRelationalCommandBuilderFactory>();
                var relationalConnection = ctx.GetService<IRelationalConnection>();
                var cmd = cmdBuilderFactory.Create().Append("SELECT 1").Build();
                var logger = ctx.GetService<IRelationalCommandDiagnosticsLogger>();

                var parameterObject = new RelationalCommandParameterObject(
                    relationalConnection,
                    parameterValues: null,
                    readerColumns: null,
                    context: ctx,
                    logger: logger);

                var result = cmd.ExecuteScalar(parameterObject);
                result.Should().NotBeNull();
            }

            var report = session.Stop();
            report.TotalQueries.Should().BeGreaterThan(0);
            report.Events.Should().Contain(e =>
                e.CommandText.Contains("SELECT", StringComparison.OrdinalIgnoreCase));
        }
    }
}
