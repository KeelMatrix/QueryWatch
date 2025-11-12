// Copyright (c) KeelMatrix
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KeelMatrix.QueryWatch.EfCore.Tests {
    public sealed class UseQueryWatchExtensionsTests {
        [Fact]
        public void UseQueryWatch_attaches_interceptor_and_captures_events() {
            using SqliteConnection connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            // 1) Without UseQueryWatch -> no events are recorded
            using (QueryWatchSession sessionNone = new()) {
                DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(connection)
                    .Options;

                using TestDbContext ctx = new(options);
                _ = ctx.Things.ToList();

                QueryWatchReport report = sessionNone.Stop();
                _ = report.TotalQueries.Should().Be(0);
            }

            // 2) With UseQueryWatch -> events are recorded
            using (QueryWatchSession sessionSome = new()) {
                DbContextOptions<TestDbContext> options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(connection)
                    .UseQueryWatch(sessionSome)
                    .Options;

                using TestDbContext ctx = new(options);
                _ = ctx.Things.ToList();

                QueryWatchReport report = sessionSome.Stop();
                _ = report.TotalQueries.Should().BeGreaterThan(0);
            }
        }
    }
}
