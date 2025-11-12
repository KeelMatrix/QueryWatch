// Copyright (c) KeelMatrix
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KeelMatrix.QueryWatch.EfCore.Tests {
    public sealed class EfCoreAsyncAndFailureTests {
        [Fact]
        public async Task NonQueryExecutedAsync_records_event() {
            await using SqliteConnection connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using QueryWatchSession session = new();
            DbContextOptions<TestDbContext> options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            await using (TestDbContext ctx = new(options)) {
                var affected = await ctx.Database.ExecuteSqlRawAsync("INSERT INTO Things(Name) VALUES ('A')", CancellationToken.None);
                _ = affected.Should().Be(1);
            }

            QueryWatchReport report = session.Stop();
            _ = report.TotalQueries.Should().BeGreaterThan(0);
            _ = report.Events.Should().Contain(e => e.CommandText.Contains("INSERT", System.StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ScalarExecutedAsync_records_event() {
            await using SqliteConnection connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using QueryWatchSession session = new();
            DbContextOptions<TestDbContext> options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            await using (TestDbContext ctx = new(options)) {
                // CountAsync triggers a scalar command
                var count = await ctx.Things.CountAsync();
                _ = count.Should().BeGreaterThanOrEqualTo(0);
            }

            QueryWatchReport report = session.Stop();
            _ = report.TotalQueries.Should().BeGreaterThan(0);
            _ = report.Events.Should().Contain(e => e.CommandText.Contains("COUNT", System.StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task CommandFailed_still_records_event_with_failed_meta() {
            await using SqliteConnection connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using QueryWatchSession session = new();
            DbContextOptions<TestDbContext> options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            // Intentionally target a non-existent table to trigger a fast failure
            _ = await Assert.ThrowsAsync<SqliteException>(async () => {
                await using TestDbContext ctx = new(options);
                _ = await ctx.Database.ExecuteSqlRawAsync("INSERT INTO __NoSuchTable__(X) VALUES (1)");
            });

            QueryWatchReport report = session.Stop();
            _ = report.TotalQueries.Should().BeGreaterThan(0, "failed commands should still be recorded");

            QueryEvent? failed = report.Events.FirstOrDefault(e => e.CommandText.Contains("__NoSuchTable__", System.StringComparison.OrdinalIgnoreCase));
            _ = failed.Should().NotBeNull("the failing SQL text should be captured");

            _ = failed!.Meta.Should().NotBeNull("failure events should include meta");
            _ = failed.Meta!.Should().ContainKey("failed").WhoseValue.Should().Be(true);
        }
    }
}
