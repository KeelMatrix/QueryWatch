// Copyright (c) KeelMatrix
#nullable enable
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KeelMatrix.QueryWatch.EfCore.Tests {
    public sealed class UseQueryWatchExtensionsTests {
        [Fact]
        public void UseQueryWatch_attaches_interceptor_and_captures_events() {
            using var connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            // 1) Without UseQueryWatch -> no events are recorded
            using (var sessionNone = new QueryWatchSession()) {
                var options = new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(connection)
                    .Options;

                using var ctx = new TestDbContext(options);
                _ = ctx.Things.ToList();

                var report = sessionNone.Stop();
                report.TotalQueries.Should().Be(0);
            }

            // 2) With UseQueryWatch -> events are recorded
            using (var sessionSome = new QueryWatchSession()) {
                var options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                    .UseSqlite(connection)
                    .UseQueryWatch(sessionSome)
                    .Options;

                using var ctx = new TestDbContext(options);
                _ = ctx.Things.ToList();

                var report = sessionSome.Stop();
                report.TotalQueries.Should().BeGreaterThan(0);
            }
        }
    }
}
