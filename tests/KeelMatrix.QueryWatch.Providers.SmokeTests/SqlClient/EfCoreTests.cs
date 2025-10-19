using FluentAssertions;
using KeelMatrix.QueryWatch.EfCore;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.SqlClient {
    [Collection("SmokeEnv")]
    public class EfCoreTests {
        private readonly ITestOutputHelper _output;
        public EfCoreTests(ITestOutputHelper output) => _output = output;

        private static string? GetConnString() =>
            Environment.GetEnvironmentVariable("QWATCH__SQLSERVER__CS");

        [Fact]
        public void EfCore_Linq_Path_Is_Recorded() {
            var cs = GetConnString();
            var builder = new DbContextOptionsBuilder<EfCtx>().UseSqlServer(cs);

            using var session = new QueryWatchSession();
            builder.UseQueryWatch(session);
            using var db = new EfCtx(builder.Options);

            // Ensure table
            db.Database.ExecuteSqlRaw("IF OBJECT_ID('dbo.QW_Items','U') IS NULL CREATE TABLE dbo.QW_Items (Id INT IDENTITY PRIMARY KEY, Name NVARCHAR(200));");

            db.Items.Add(new Item { Name = "Alice" });
            db.SaveChanges();
            var count = db.Items.Count(i => i.Name != null);
            count.Should().BeGreaterThan(0);

            var report = session.Stop();
            report.TotalQueries.Should().BeGreaterThan(0);
        }
    }
}
