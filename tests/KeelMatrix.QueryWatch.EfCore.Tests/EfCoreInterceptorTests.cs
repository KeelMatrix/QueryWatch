// Copyright (c) KeelMatrix
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace KeelMatrix.QueryWatch.EfCore.Tests {
    public sealed class EfCoreInterceptorTests {
        [Fact]
        public void ReaderExecuted_records_event() {
            using SqliteConnection connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using QueryWatchSession session = new();
            DbContextOptions<TestDbContext> options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (TestDbContext ctx = new(options)) {
                // A simple SELECT using the EF query pipeline
                _ = ctx.Things.Where(t => t.Id > 0).ToList();
            }

            QueryWatchReport report = session.Stop();
            _ = report.TotalQueries.Should().BeGreaterThan(0);

            // We expect at least one SELECT against the Things table
            _ = report.Events.Should().Contain(e =>
                e.CommandText.Contains("FROM", StringComparison.OrdinalIgnoreCase) &&
                e.CommandText.Contains("Things", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ReaderExecutedAsync_records_event() {
            await using SqliteConnection connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using QueryWatchSession session = new();
            DbContextOptions<TestDbContext> options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            await using (TestDbContext ctx = new(options)) {
                // Async SELECT
                _ = await ctx.Things.Where(t => t.Id >= 0).ToListAsync();
            }

            QueryWatchReport report = session.Stop();
            _ = report.TotalQueries.Should().BeGreaterThan(0);
            _ = report.Events.Should().Contain(e =>
                e.CommandText.Contains("FROM", StringComparison.OrdinalIgnoreCase) &&
                e.CommandText.Contains("Things", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void NonQueryExecuted_records_event() {
            using SqliteConnection connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using QueryWatchSession session = new();
            DbContextOptions<TestDbContext> options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (TestDbContext ctx = new(options)) {
                // Execute a raw non-query (UPDATE). It will run even if the table is empty.
                var affected = ctx.Database.ExecuteSqlRaw("UPDATE Things SET Name = Name");
                _ = affected.Should().BeGreaterThanOrEqualTo(0);
            }

            QueryWatchReport report = session.Stop();
            _ = report.TotalQueries.Should().BeGreaterThan(0);
            _ = report.Events.Should().Contain(e =>
                e.CommandText.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public void ScalarExecuted_records_event() {
            using SqliteConnection connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using QueryWatchSession session = new();
            DbContextOptions<TestDbContext> options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (TestDbContext ctx = new(options)) {
                // Use EF Core relational services to execute a scalar command
                IRelationalCommandBuilderFactory cmdBuilderFactory = ctx.GetService<IRelationalCommandBuilderFactory>();
                IRelationalConnection relationalConnection = ctx.GetService<IRelationalConnection>();
                IRelationalCommand cmd = cmdBuilderFactory.Create().Append("SELECT 1").Build();
                IRelationalCommandDiagnosticsLogger logger = ctx.GetService<IRelationalCommandDiagnosticsLogger>();

                RelationalCommandParameterObject parameterObject = new(
                    relationalConnection,
                    parameterValues: null,
                    readerColumns: null,
                    context: ctx,
                    logger: logger);

                var result = cmd.ExecuteScalar(parameterObject);
                _ = result.Should().NotBeNull();
            }

            QueryWatchReport report = session.Stop();
            _ = report.TotalQueries.Should().BeGreaterThan(0);
            _ = report.Events.Should().Contain(e =>
                e.CommandText.Contains("SELECT", StringComparison.OrdinalIgnoreCase));
        }
    }
}
