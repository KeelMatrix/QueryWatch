// Copyright (c) KeelMatrix
using System.Data;
using FluentAssertions;
using Npgsql;
using Xunit;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.Npgsql {
    [Collection("SmokeEnv")]
    public sealed class AdoParameterShapeTests {
        private static string? GetConnString() =>
            Environment.GetEnvironmentVariable("QWATCH__POSTGRES__CS");

        [Fact]
        public void Ado_Parameter_Shapes_Are_Captured_For_Npgsql() {
            string? cs = GetConnString();
            using NpgsqlConnection raw = new(cs);
            using QueryWatchSession session = new();
            raw.Open();

            // Use the canonical extension to wrap a real provider
            using var conn = raw.WithQueryWatch(session);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT @p_int, @p_when";
            var p1 = cmd.CreateParameter();
            p1.ParameterName = "@p_int";
            p1.DbType = DbType.Int32;
            p1.Value = 7;
            _ = cmd.Parameters.Add(p1);

            var p2 = cmd.CreateParameter();
            p2.ParameterName = "@p_when";
            p2.DbType = DbType.DateTimeOffset;
            p2.Value = DateTimeOffset.UtcNow;
            _ = cmd.Parameters.Add(p2);

            object? scalar = cmd.ExecuteScalar();
            _ = scalar.Should().NotBeNull();

            var ev = session.Stop().Events[^1];
            _ = ev.Meta.Should().NotBeNull();
            _ = ev.Meta.Should().ContainKey("parameters");

            var enumerable = (ev.Meta!["parameters"] as System.Collections.IEnumerable)!;
            List<object> shapes = [.. enumerable.Cast<object>()];
            _ = shapes.Should().HaveCountGreaterThanOrEqualTo(2);

            static string? Get(object o, string name) => o.GetType().GetProperty(name)!.GetValue(o)?.ToString();

            object pint = shapes.First(s => Get(s, "Name") == "@p_int");
            _ = Get(pint, "DbType").Should().Be("Int32");
            _ = Get(pint, "ClrType").Should().Be(typeof(int).FullName);
            _ = Get(pint, "Direction").Should().Be("Input");

            object pwhen = shapes.First(s => Get(s, "Name") == "@p_when");
            _ = Get(pwhen, "DbType").Should().BeOneOf("DateTimeOffset", "DateTime");
            _ = Get(pwhen, "ClrType").Should().BeOneOf(typeof(DateTimeOffset).FullName, typeof(DateTime).FullName);
            _ = Get(pwhen, "Direction").Should().Be("Input");
        }
    }
}
