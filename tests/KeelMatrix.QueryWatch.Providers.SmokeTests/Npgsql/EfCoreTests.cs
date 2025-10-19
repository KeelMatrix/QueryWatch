using FluentAssertions;
using KeelMatrix.QueryWatch.EfCore;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.Npgsql {
    [Collection("SmokeEnv")]
    public class EfCoreTests {
        private readonly ITestOutputHelper _output;
        public EfCoreTests(ITestOutputHelper output) => _output = output;

        private static string? GetConnString() =>
            Environment.GetEnvironmentVariable("QWATCH__POSTGRES__CS");

        [Fact]
        public void EfCore_Linq_Path_Is_Recorded() {
            var cs = GetConnString();
            var builder = new DbContextOptionsBuilder<EfCtx>().UseNpgsql(cs);

            using var session = new QueryWatchSession();
            builder.UseQueryWatch(session);
            using var db = new EfCtx(builder.Options);

            db.Database.ExecuteSqlRaw("CREATE TABLE IF NOT EXISTS qw_items (id SERIAL PRIMARY KEY, name TEXT)");
            db.Items.Add(new Item { Name = "Alice" });
            db.SaveChanges();
            var count = db.Items.Count(i => i.Name != null);
            count.Should().BeGreaterThan(0);

            session.Stop().TotalQueries.Should().BeGreaterThan(0);
        }
    }
}
