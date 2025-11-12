// Copyright (c) KeelMatrix
using System.Collections;
using System.Data;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.SqlClient {
    [Collection("SmokeEnv")]
    public sealed class AdoParameterShapeTests {
        private static string? GetConnString() =>
            Environment.GetEnvironmentVariable("QWATCH__SQLSERVER__CS");

        [Fact]
        public void Ado_Parameter_Shapes_Are_Captured_For_SqlClient() {
            string? cs = GetConnString();
            using SqlConnection raw = new(cs);
            using QueryWatchSession session = new(); // defaults: CaptureSqlText=true, CaptureParameterShape=true
            raw.Open();

            // Use canonical extension in addition to direct wrapper path exercised elsewhere
            using var conn = raw.WithQueryWatch(session);

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT @id AS A, @name AS B";
            var p1 = cmd.CreateParameter();
            p1.ParameterName = "@id";
            p1.DbType = DbType.Int32;
            p1.Value = 42;
            _ = cmd.Parameters.Add(p1);

            var p2 = cmd.CreateParameter();
            p2.ParameterName = "@name";
            p2.DbType = DbType.String;
            p2.Value = "Alice";
            _ = cmd.Parameters.Add(p2);

            // Execute and ensure a result
            object? scalar = cmd.ExecuteScalar();
            _ = scalar.Should().NotBeNull();

            var ev = session.Stop().Events[^1];
            _ = ev.Meta.Should().NotBeNull("parameter metadata should be attached when enabled");
            _ = ev.Meta.Should().ContainKey("parameters");
            object? parsObj = ev.Meta!["parameters"];
            _ = parsObj.Should().NotBeNull();

            IEnumerable? enumerable = parsObj as System.Collections.IEnumerable;
            _ = enumerable.Should().NotBeNull("parameters should be an enumerable");
            List<object> shapes = enumerable!.Cast<object>().ToList();
            _ = shapes.Should().HaveCountGreaterThanOrEqualTo(2);

            static string? Get(object o, string name) => o.GetType().GetProperty(name)!.GetValue(o)?.ToString();

            // Names and types are stable (no values captured)
            object id = shapes.First(s => Get(s, "Name") == "@id");
            _ = Get(id, "DbType").Should().Be("Int32");
            _ = Get(id, "ClrType").Should().Be(typeof(int).FullName);
            _ = Get(id, "Direction").Should().Be("Input");

            object nm = shapes.First(s => Get(s, "Name") == "@name");
            _ = Get(nm, "DbType").Should().Be("String");
            _ = Get(nm, "ClrType").Should().Be(typeof(string).FullName);
            _ = Get(nm, "Direction").Should().Be("Input");
        }
    }
}
