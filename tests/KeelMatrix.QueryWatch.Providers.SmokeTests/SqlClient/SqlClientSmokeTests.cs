// Copyright (c) KeelMatrix
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using Microsoft.Data.SqlClient;
using Xunit;
using Xunit.Abstractions;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.SqlClient {
    [Collection("SmokeEnv")]
    public class SqlClientSmokeTests {
        private readonly ITestOutputHelper _output;
        public SqlClientSmokeTests(ITestOutputHelper output) => _output = output;

        private static string? GetConnString() =>
            Environment.GetEnvironmentVariable("QWATCH__SQLSERVER__CS");

        [Fact]
        public void Ado_Wrapper_Select_Then_Failing_Command_Emits_Stable_Meta() {
            string? cs = GetConnString();
            using SqlConnection raw = new(cs);
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
            await using SqlConnection raw = new(cs);
            await raw.OpenAsync();
            using QueryWatchSession session = new();
            await using QueryWatchConnection conn = new(raw, session);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "WAITFOR DELAY '00:00:05'";
            using CancellationTokenSource cts = new(TimeSpan.FromMilliseconds(200));

            var act = async () => await cmd.ExecuteNonQueryAsync(cts.Token);
            _ = await act.Should().ThrowAsync<OperationCanceledException>();

            var report = session.Stop();
            _ = report.Events.Should().NotBeEmpty();
            _ = report.Events[^1].Meta!["failed"].Should().Be(true);
        }

        [Fact]
        public void Ado_Parameterized_Command_Executes_And_Records() {
            string? cs = GetConnString();
            using SqlConnection raw = new(cs);
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

            var report = session.Stop();
            _ = report.TotalQueries.Should().Be(1);
        }

        [Fact]
        public void Ado_Transaction_With_Multiple_ResultSets_Produces_Single_Event() {
            string? cs = GetConnString();
            using SqlConnection raw = new(cs);
            using QueryWatchSession session = new();
            raw.Open();
            using QueryWatchConnection conn = new(raw, session);

            using var tx = conn.BeginTransaction();
            using var cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = "SELECT 1 AS A; SELECT 2 AS B;";
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

            var report = session.Stop();
            _ = report.TotalQueries.Should().Be(1, "one command with two result sets should be a single event");
        }

        [Fact]
        public void Ado_CommandTimeout_Triggers_Failure_Meta() {
            string? cs = GetConnString();
            using SqlConnection raw = new(cs);
            using QueryWatchSession session = new();
            raw.Open();
            using QueryWatchConnection conn = new(raw, session);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "WAITFOR DELAY '00:00:05'";
            cmd.CommandTimeout = 1; // seconds
            Action act = () => cmd.ExecuteNonQuery();
            _ = act.Should().Throw<Exception>();

            var last = session.Stop().Events[^1];
            _ = last.Meta.Should().NotBeNull();
            _ = last.Meta!["failed"].Should().Be(true);
        }
    }
}
