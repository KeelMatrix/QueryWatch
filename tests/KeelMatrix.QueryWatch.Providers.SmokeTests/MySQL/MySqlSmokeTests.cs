// Copyright (c) KeelMatrix
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using Xunit;
using Xunit.Abstractions;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.MySQL {
    [Collection("SmokeEnv")]
    public class MySqlSmokeTests {
        private readonly ITestOutputHelper _output;
        public MySqlSmokeTests(ITestOutputHelper output) => _output = output;

        private static string? GetConnString() =>
            Environment.GetEnvironmentVariable("QWATCH__MYSQL__CS");

        [Fact]
        public void Ado_Parameterized_Command_Executes_And_Records() {
            var cs = GetConnString();
            using var raw = new MySqlConnector.MySqlConnection(cs);
            using var session = new QueryWatchSession();
            raw.Open();
            using var conn = new QueryWatchConnection(raw, session);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT @p";
            var p = cmd.CreateParameter();
            p.ParameterName = "@p";
            p.Value = 123;
            cmd.Parameters.Add(p);
            var scalar = cmd.ExecuteScalar();
            scalar.Should().Be(123);

            session.Stop().TotalQueries.Should().Be(1);
        }

        [Fact]
        public async Task Ado_Async_Cancellation_Records_Failure() {
            var cs = GetConnString();
            await using var raw = new MySqlConnector.MySqlConnection(cs);
            await raw.OpenAsync();
            using var session = new QueryWatchSession();
            await using var conn = new QueryWatchConnection(raw, session);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT SLEEP(5)";
            using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));
            var act = async () => await cmd.ExecuteNonQueryAsync(cts.Token);
            await act.Should().ThrowAsync<OperationCanceledException>();

            session.Stop().Events[^1].Meta!["failed"].Should().Be(true);
        }

        [Fact]
        public void Ado_Transaction_With_Multiple_ResultSets_Produces_Single_Event() {
            var cs = GetConnString();
            using var raw = new MySqlConnector.MySqlConnection(cs);
            using var session = new QueryWatchSession();
            raw.Open();
            using var conn = new QueryWatchConnection(raw, session);

            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT 1; SELECT 2;";
            using var reader = cmd.ExecuteReader();
            int sets = 0;

            uint dummy = 0;
            do {
                while (reader.Read())
                    dummy++;
                sets++;
            }
            while (reader.NextResult());

            sets.Should().Be(2);
            tx.Commit();

            session.Stop().TotalQueries.Should().Be(1);
        }

        [Fact]
        public void Ado_CommandTimeout_Triggers_Failure_Meta() {
            var cs = GetConnString();
            using var raw = new MySqlConnector.MySqlConnection(cs);
            using var session = new QueryWatchSession();
            raw.Open();
            using var conn = new QueryWatchConnection(raw, session);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT SLEEP(5)";
            cmd.CommandTimeout = 1;
            Action act = () => cmd.ExecuteNonQuery();
            act.Should().Throw<Exception>();

            session.Stop().Events[^1].Meta!["failed"].Should().Be(true);
        }
    }
}
