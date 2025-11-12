// Copyright (c) KeelMatrix
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using MySqlConnector;
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
            string? cs = GetConnString();
            using MySqlConnection raw = new(cs);
            using QueryWatchSession session = new();
            raw.Open();
            using QueryWatchConnection conn = new(raw, session);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT @p";
            var p = cmd.CreateParameter();
            p.ParameterName = "@p";
            p.Value = 123;
            _ = cmd.Parameters.Add(p);
            object? scalar = cmd.ExecuteScalar();
            _ = scalar.Should().Be(123);

            _ = session.Stop().TotalQueries.Should().Be(1);
        }

        [Fact]
        public async Task Ado_Async_Cancellation_Records_Failure() {
            string? cs = GetConnString();
            await using MySqlConnection raw = new(cs);
            await raw.OpenAsync();
            using QueryWatchSession session = new();
            await using QueryWatchConnection conn = new(raw, session);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT SLEEP(5)";
            using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(200));
            var act = async () => await cmd.ExecuteNonQueryAsync(cts.Token);
            _ = await act.Should().ThrowAsync<OperationCanceledException>();

            _ = session.Stop().Events[^1].Meta!["failed"].Should().Be(true);
        }

        [Fact]
        public void Ado_Transaction_With_Multiple_ResultSets_Produces_Single_Event() {
            string? cs = GetConnString();
            using MySqlConnection raw = new(cs);
            using QueryWatchSession session = new();
            raw.Open();
            using QueryWatchConnection conn = new(raw, session);

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

            _ = sets.Should().Be(2);
            tx.Commit();

            _ = session.Stop().TotalQueries.Should().Be(1);
        }

        [Fact]
        public void Ado_CommandTimeout_Triggers_Failure_Meta() {
            string? cs = GetConnString();
            using MySqlConnection raw = new(cs);
            using QueryWatchSession session = new();
            raw.Open();
            using QueryWatchConnection conn = new(raw, session);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT SLEEP(5)";
            cmd.CommandTimeout = 1;
            Action act = () => cmd.ExecuteNonQuery();
            _ = act.Should().Throw<Exception>();

            _ = session.Stop().Events[^1].Meta!["failed"].Should().Be(true);
        }
    }
}
