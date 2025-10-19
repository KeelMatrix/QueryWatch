// Copyright (c) KeelMatrix
using FluentAssertions;
using KeelMatrix.QueryWatch.EfCore;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.MySQL {
    [Collection("SmokeEnv")]
    public class EfCoreTests {
        private static string? GetConnString() =>
            Environment.GetEnvironmentVariable("QWATCH__MYSQL__CS");

        private readonly ITestOutputHelper _output;
        public EfCoreTests(ITestOutputHelper output) => _output = output;

        [Fact]
        public void EfCore_Linq_Path_Is_Recorded() {
            var cs = GetConnString();
            var builder = new DbContextOptionsBuilder<EfCtx>()
                .UseMySql(cs, ServerVersion.AutoDetect(cs));

            using var session = new QueryWatchSession();
            builder.UseQueryWatch(session);
            using var db = new EfCtx(builder.Options);

            db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS QW_Items (Id INT NOT NULL AUTO_INCREMENT PRIMARY KEY, Name VARCHAR(200))");
            db.Items.Add(new Item { Name = "Alice" });
            db.SaveChanges();
            var count = db.Items.Count(i => i.Name != null);
            count.Should().BeGreaterThan(0);

            session.Stop().TotalQueries.Should().BeGreaterThan(0);
        }
    }
}
