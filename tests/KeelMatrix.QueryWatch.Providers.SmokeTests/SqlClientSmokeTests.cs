// Copyright (c) KeelMatrix
#nullable enable
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using KeelMatrix.QueryWatch.EfCore;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests {
    [Collection("SmokeEnv")]
    public class SqlClientSmokeTests {
        private readonly ITestOutputHelper _output;
        public SqlClientSmokeTests(ITestOutputHelper output) => _output = output;

        private static string? GetConnString() =>
            Environment.GetEnvironmentVariable("QWATCH__SQLSERVER__CS");

        [Fact]
        public void Ado_Wrapper_Select_Then_Failing_Command_Emits_Stable_Meta() {
            var cs = GetConnString();
            using var raw = new Microsoft.Data.SqlClient.SqlConnection(cs);
            using var session = new QueryWatchSession();
            raw!.Open();
            using var conn = new QueryWatchConnection(raw, session);

            using (var ok = conn.CreateCommand()) {
                ok.CommandText = "SELECT 1";
                var scalar = ok.ExecuteScalar();
                scalar.Should().NotBeNull();
            }

            using (var bad = conn.CreateCommand()) {
                bad.CommandText = "SELECT * FROM __NoSuchTable__";
                Action act = () => bad.ExecuteNonQuery();
                act.Should().Throw<Exception>();
            }

            var report = session.Stop();
            report.Events.Should().NotBeEmpty();
            var last = report.Events[^1];
            last.Meta.Should().NotBeNull();
            last.Meta!.Should().ContainKey("failed").WhoseValue.Should().Be(true);
            last.Meta!.Should().ContainKey("exception");
            last.Meta!.Should().ContainKey("provider").WhoseValue.Should().Be("ado");
        }

        private sealed class BareDbContext : DbContext {
            public BareDbContext(DbContextOptions options) : base(options) { }
        }

        [Fact]
        public void EfCore_Interceptor_Select_Then_Failing_Command_Emits_Stable_Meta() {
            var cs = GetConnString();
            var builder = new DbContextOptionsBuilder<BareDbContext>();
            builder.UseSqlServer(cs);

            using var session = new QueryWatchSession();
            builder.UseQueryWatch(session);
            var options = builder.Options;

            using var db = new BareDbContext(options);

            // success
            db.Database.ExecuteSqlRaw("SELECT 1");
            // failure
            Action act = () => db.Database.ExecuteSqlRaw("SELECT * FROM __NoSuchTable__");
            act.Should().Throw<Exception>();

            var report = session.Stop();
            report.Events.Should().NotBeEmpty();
            var last = report.Events[^1];
            last.Meta.Should().NotBeNull();
            last.Meta!.Should().ContainKey("failed").WhoseValue.Should().Be(true);
            last.Meta!.Should().ContainKey("exception");
            last.Meta!.Should().ContainKey("provider").WhoseValue.Should().Be("efcore");
        }
    }
}
