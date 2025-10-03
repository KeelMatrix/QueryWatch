// Copyright (c) KeelMatrix
#nullable enable
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KeelMatrix.QueryWatch.EfCore.Tests {
    public sealed class EfCoreAsyncAndFailureTests {
        [Fact]
        public async Task NonQueryExecutedAsync_records_event() {
            using var connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using var session = new QueryWatchSession();
            var options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (var ctx = new TestDbContext(options)) {
                var affected = await ctx.Database.ExecuteSqlRawAsync("INSERT INTO Things(Name) VALUES ('A')", CancellationToken.None);
                affected.Should().Be(1);
            }

            var report = session.Stop();
            report.TotalQueries.Should().BeGreaterThan(0);
            report.Events.Should().Contain(e => e.CommandText.Contains("INSERT", System.StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task ScalarExecutedAsync_records_event() {
            using var connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using var session = new QueryWatchSession();
            var options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (var ctx = new TestDbContext(options)) {
                // CountAsync triggers a scalar command
                var count = await ctx.Things.CountAsync();
                count.Should().BeGreaterThanOrEqualTo(0);
            }

            var report = session.Stop();
            report.TotalQueries.Should().BeGreaterThan(0);
            report.Events.Should().Contain(e => e.CommandText.Contains("COUNT", System.StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task CommandFailed_still_records_event_with_failed_meta() {
            using var connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            using var session = new QueryWatchSession();
            var options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            // Intentionally target a non-existent table to trigger a fast failure
            await Assert.ThrowsAsync<SqliteException>(async () => {
                using var ctx = new TestDbContext(options);
                await ctx.Database.ExecuteSqlRawAsync("INSERT INTO __NoSuchTable__(X) VALUES (1)");
            });

            var report = session.Stop();
            report.TotalQueries.Should().BeGreaterThan(0, "failed commands should still be recorded");

            var failed = report.Events.FirstOrDefault(e => e.CommandText.Contains("__NoSuchTable__", System.StringComparison.OrdinalIgnoreCase));
            failed.Should().NotBeNull("the failing SQL text should be captured");

            failed!.Meta.Should().NotBeNull("failure events should include meta");
            failed.Meta!.Should().ContainKey("failed").WhoseValue.Should().Be(true);
        }
    }
}
