using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using KeelMatrix.QueryWatch.Reporting;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class AdoParameterMetadataCaptureTests {
        [Fact]
        public void QueryWatchCommand_Captures_Ado_Parameter_Shapes_When_Enabled() {
            var opts = new QueryWatchOptions { CaptureAdoParameterMetadata = true };
            using var session = QueryWatcher.Start(opts);

            var inner = new FakeDbCommand { CommandText = "SELECT 1" };

            var p1 = inner.CreateParameter();
            p1.ParameterName = "@id";
            p1.DbType = DbType.Int32;
            p1.Direction = ParameterDirection.Input;
            inner.Parameters.Add(p1);

            var p2 = inner.CreateParameter();
            p2.ParameterName = "@name";
            p2.DbType = DbType.String;
            p2.Direction = ParameterDirection.Input;
            inner.Parameters.Add(p2);

            using var cmd = new QueryWatchCommand(inner, session);
            cmd.ExecuteNonQuery();

            var report = session.Stop();
            report.TotalQueries.Should().Be(1);
            var ev = report.Events[0];
            ev.Meta.Should().NotBeNull("metadata should be present when capture is enabled");
            ev.Meta!.Should().ContainKey("parameters");

            var obj = ev.Meta!["parameters"];
            obj.Should().NotBeNull();

            // We reflect over the internal AdoParameterShape to assert its properties without exposing it.
            var seq = obj as System.Collections.IEnumerable;
            seq.Should().NotBeNull("parameters should be an enumerable");
            var list = seq!.Cast<object>().ToList();
            list.Should().HaveCount(2);

            static string? Get(object o, string name) => o.GetType().GetProperty(name)!.GetValue(o)?.ToString();

            Get(list[0], "Name").Should().Be("@id");
            Get(list[0], "DbType").Should().Be("Int32");
            Get(list[0], "ClrType").Should().Be(typeof(int).FullName);
            Get(list[0], "Direction").Should().Be("Input");

            Get(list[1], "Name").Should().Be("@name");
            Get(list[1], "DbType").Should().Be("String");
            Get(list[1], "ClrType").Should().Be(typeof(string).FullName);
            Get(list[1], "Direction").Should().Be("Input");
        }

        [Fact]
        public void QueryWatchCommand_Does_Not_Capture_Parameter_Metadata_When_Disabled() {
            using var session = QueryWatcher.Start(); // default CaptureAdoParameterMetadata=false

            var inner = new FakeDbCommand { CommandText = "SELECT 1" };
            var p = inner.CreateParameter();
            p.ParameterName = "@id";
            p.DbType = DbType.Int32;
            inner.Parameters.Add(p);

            using var cmd = new QueryWatchCommand(inner, session);
            cmd.ExecuteNonQuery();

            var report = session.Stop();
            report.Events.Should().HaveCount(1);
            report.Events[0].Meta.Should().BeNull("metadata capture is off by default");
        }

        [Fact]
        public void Exported_Json_Includes_Event_Meta_Parameters() {
            var opts = new QueryWatchOptions { CaptureAdoParameterMetadata = true };
            using var session = QueryWatcher.Start(opts);

            var inner = new FakeDbCommand { CommandText = "SELECT 1" };
            var p = inner.CreateParameter();
            p.ParameterName = "@when";
            p.DbType = DbType.DateTimeOffset;
            p.Direction = ParameterDirection.Input;
            inner.Parameters.Add(p);

            using var cmd = new QueryWatchCommand(inner, session);
            cmd.ExecuteNonQuery();

            var report = session.Stop();

            var tempRoot = Path.Combine(Path.GetTempPath(), "QueryWatchTests", Guid.NewGuid().ToString("N"));
            var outPath = Path.Combine(tempRoot, "ado-meta.json");
            QueryWatchJson.ExportToFile(report, outPath, sampleTop: 5);

            File.Exists(outPath).Should().BeTrue();
            using var doc = JsonDocument.Parse(File.ReadAllText(outPath));
            var root = doc.RootElement;
            root.GetProperty("events").GetArrayLength().Should().Be(1);
            var ev0 = root.GetProperty("events")[0];
            ev0.TryGetProperty("meta", out var metaEl).Should().BeTrue("event-level meta should be present");
            metaEl.TryGetProperty("parameters", out var pars).Should().BeTrue();
            pars.GetArrayLength().Should().Be(1);
            var p0 = pars[0];
            p0.GetProperty("name").GetString().Should().Be("@when");
            p0.GetProperty("dbType").GetString().Should().Be("DateTimeOffset");
            p0.GetProperty("clrType").GetString().Should().Be(typeof(DateTimeOffset).FullName);
            p0.GetProperty("direction").GetString().Should().Be("Input");
        }

        // Minimal fakes suitable for the ADO wrapper tests.
        private sealed class FakeDbCommand : DbCommand {
            [AllowNull]
            public override string CommandText { get; set; } = string.Empty;
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; } = CommandType.Text;
            public override UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;

            protected override DbConnection? DbConnection { get; set; }
            protected override DbParameterCollection DbParameterCollection { get; } = new FakeDbParameterCollection();
            protected override DbTransaction? DbTransaction { get; set; }
            public override bool DesignTimeVisible { get; set; }

            public override void Cancel() { }
            public override int ExecuteNonQuery() => 1;
            public override object? ExecuteScalar() => 42;
            public override void Prepare() { }

            protected override DbParameter CreateDbParameter() => new FakeDbParameter();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();

            private sealed class FakeDbParameterCollection : DbParameterCollection {
                private readonly System.Collections.ArrayList _list = new();
                public override int Add(object value) { _list.Add(value); return _list.Count - 1; }
                public override void AddRange(Array values) { foreach (var v in values) _list.Add(v); }
                public override void Clear() => _list.Clear();
                public override bool Contains(object value) => _list.Contains(value);
                public override bool Contains(string value) => IndexOf(value) >= 0;
                public override void CopyTo(Array array, int index) => _list.CopyTo(array, index);
                public override int Count => _list.Count;
                public override System.Collections.IEnumerator GetEnumerator() => _list.GetEnumerator();
                protected override DbParameter GetParameter(int index) => (DbParameter)_list[index]!;
                protected override DbParameter GetParameter(string parameterName) => throw new NotSupportedException();
                public override int IndexOf(object value) => _list.IndexOf(value);
                public override int IndexOf(string parameterName) => -1;
                public override void Insert(int index, object value) => _list.Insert(index, value);
                public override bool IsFixedSize => false;
                public override bool IsReadOnly => false;
                public override bool IsSynchronized => false;
                public override void Remove(object value) => _list.Remove(value);
                public override void RemoveAt(int index) => _list.RemoveAt(index);
                public override void RemoveAt(string parameterName) => throw new NotSupportedException();
                protected override void SetParameter(int index, DbParameter value) => _list[index] = value;
                protected override void SetParameter(string parameterName, DbParameter value) => throw new NotSupportedException();
                public override object SyncRoot => this;
            }

            private sealed class FakeDbParameter : DbParameter {
                public override DbType DbType { get; set; }
                public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
                public override bool IsNullable { get; set; }
                [AllowNull]
                public override string ParameterName { get; set; } = string.Empty;
                [AllowNull]
                public override string SourceColumn { get; set; } = string.Empty;
                public override object? Value { get; set; }
                public override bool SourceColumnNullMapping { get; set; }
                public override int Size { get; set; }
                public override void ResetDbType() { }
            }
        }
    }
}
