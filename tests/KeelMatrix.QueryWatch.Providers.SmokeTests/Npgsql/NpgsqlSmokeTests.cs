// Copyright (c) KeelMatrix
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using Npgsql;
using Xunit;
using Xunit.Abstractions;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.Npgsql {
    [Collection("SmokeEnv")]
    public class NpgsqlSmokeTests {
        private readonly ITestOutputHelper _output;
        public NpgsqlSmokeTests(ITestOutputHelper output) => _output = output;

        private static string? GetConnString() =>
            Environment.GetEnvironmentVariable("QWATCH__POSTGRES__CS");

        [Fact]
        public void Ado_Wrapper_Select_Then_Failing_Command_Emits_Stable_Meta() {
            string? cs = GetConnString();
            using NpgsqlConnection raw = new(cs);
            using QueryWatchSession session = new();
            raw!.Open();
            using QueryWatchConnection conn = new(raw, session);

            using (var ok = conn.CreateCommand()) {
                ok.CommandText = "SELECT 1";
                object? scalar = ok.ExecuteScalar();
                _ = scalar.Should().NotBeNull();
            }

            using (var bad = conn.CreateCommand()) {
                bad.CommandText = "SELECT * FROM __NoSuchTable__";
                Action act = () => bad.ExecuteNonQuery();
                _ = act.Should().Throw<Exception>();
            }

            var report = session.Stop();
            _ = report.Events.Should().NotBeEmpty();
            var last = report.Events[^1];
            _ = last.Meta.Should().NotBeNull();
            _ = last.Meta!.Should().ContainKey("failed").WhoseValue.Should().Be(true);
            _ = last.Meta!.Should().ContainKey("exception");
            _ = last.Meta!.Should().ContainKey("provider").WhoseValue.Should().Be("ado");
        }

        [Fact]
        public async Task Ado_Async_Cancellation_Records_Failure() {
            string? cs = GetConnString();
            await using NpgsqlConnection raw = new(cs);
            await raw.OpenAsync();
            using QueryWatchSession session = new();
            await using QueryWatchConnection conn = new(raw, session);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT pg_sleep(5)";
            using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(200));
            var act = async () => await cmd.ExecuteNonQueryAsync(cts.Token);
            _ = await act.Should().ThrowAsync<OperationCanceledException>();

            var last = session.Stop().Events[^1];
            _ = last.Meta!["failed"].Should().Be(true);
        }

        [Fact]
        public void Ado_Parameterized_Command_Executes_And_Records() {
            string? cs = GetConnString();
            using NpgsqlConnection raw = new(cs);
            using QueryWatchSession session = new();
            raw.Open();
            using QueryWatchConnection conn = new(raw, session);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT @p";
            var p = cmd.CreateParameter();
            p.ParameterName = "@p";
            p.Value = 42;
            _ = cmd.Parameters.Add(p);
            object? scalar = cmd.ExecuteScalar();
            _ = scalar.Should().Be(42);
            _ = session.Stop().TotalQueries.Should().Be(1);
        }

        [Fact]
        public void Ado_Transaction_With_Multiple_ResultSets_Produces_Single_Event() {
            string? cs = GetConnString();
            using NpgsqlConnection raw = new(cs);
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
            using NpgsqlConnection raw = new(cs);
            using QueryWatchSession session = new();
            raw.Open();
            using QueryWatchConnection conn = new(raw, session);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT pg_sleep(5)";
            cmd.CommandTimeout = 1;
            Action act = () => cmd.ExecuteNonQuery();
            _ = act.Should().Throw<Exception>();

            _ = session.Stop().Events[^1].Meta!["failed"].Should().Be(true);
        }
    }
}
