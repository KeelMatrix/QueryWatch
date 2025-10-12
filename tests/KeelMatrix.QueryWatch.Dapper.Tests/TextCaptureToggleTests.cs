// Copyright (c) KeelMatrix
#nullable enable
using System.Data;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using Xunit;

namespace KeelMatrix.QueryWatch.Dapper.Tests {
    public class TextCaptureToggleTests {

        [Fact]
        public void DisableDapperTextCapture_Overrides_Global_Capture() {
            var opts = new QueryWatchOptions {
                CaptureSqlText = true,
                DisableDapperTextCapture = true // per-adapter fast gate
            };
            using var session = new QueryWatchSession(opts);

            using var conn = new DapperQueryWatchConnection(new OnlyIdbConnection(), session);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            var n = cmd.ExecuteNonQuery();
            n.Should().Be(1);

            var report = session.Stop();
            report.Events.Should().HaveCount(1);
            report.Events[0].CommandText.Should().BeEmpty("Dapper text capture is disabled");
        }

        [Fact]
        public void DisableDapperTextCapture_Does_Not_Affect_Ado_Wrapper() {
            var opts = new QueryWatchOptions {
                CaptureSqlText = true,
                DisableDapperTextCapture = true // only Dapper should be affected
            };
            using var session = new QueryWatchSession(opts);

            using var raw = new MiniDbConnection();
            using var ado = new QueryWatchConnection(raw, session);
            using var cmd = ado.CreateCommand();
            cmd.CommandText = "SELECT 42";
            var n = cmd.ExecuteNonQuery();
            n.Should().Be(1);

            var report = session.Stop();
            report.Events.Should().HaveCount(1);
            report.Events[0].CommandText.Should().Be("SELECT 42");
        }

        #region Minimal fakes

        // Minimal IDbConnection/IDbCommand pair to exercise the Dapper wrapper.
        private sealed class OnlyIdbConnection : IDbConnection {
            [AllowNull]
            public string ConnectionString { get; set; } = "Fake";
            public int ConnectionTimeout => 0;
            public string Database => "Fake";
            public ConnectionState State => ConnectionState.Open;
            public IDbTransaction BeginTransaction() => new OnlyIdbTransaction();
            public IDbTransaction BeginTransaction(IsolationLevel il) => new OnlyIdbTransaction();
            public void ChangeDatabase(string databaseName) { }
            public void Close() { }
            public IDbCommand CreateCommand() => new NoopIdbCommand();
            public void Open() { }
            public void Dispose() { }

            private sealed class OnlyIdbTransaction : IDbTransaction {
                public IDbConnection Connection => null!;
                public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
                public void Commit() { }
                public void Dispose() { }
                public void Rollback() { }
            }
        }

        private sealed class NoopIdbCommand : IDbCommand {
            [AllowNull] public string CommandText { get; set; } = string.Empty;
            public int CommandTimeout { get; set; }
            public CommandType CommandType { get; set; } = CommandType.Text;
            public IDbConnection? Connection { get; set; }
            public IDataParameterCollection Parameters { get; } = new NoopParameterCollection();
            public IDbTransaction? Transaction { get; set; }
            public UpdateRowSource UpdatedRowSource { get; set; }
            public void Cancel() { }
            public IDbDataParameter CreateParameter() => new NoopParameter();
            public void Dispose() { }
            public int ExecuteNonQuery() => 1;
            public IDataReader ExecuteReader() => new NoopReader();
            public IDataReader ExecuteReader(CommandBehavior behavior) => new NoopReader();
            public object? ExecuteScalar() => 1;
            public void Prepare() { }

            private sealed class NoopParameterCollection : System.Collections.ArrayList, IDataParameterCollection {
                [AllowNull]
                public object this[string parameterName] { get => null!; set { _ = value; } }
                public bool Contains(string parameterName) => false;
                public int IndexOf(string parameterName) => -1;
                public void RemoveAt(string parameterName) { }
            }

            private sealed class NoopParameter : IDbDataParameter {
                public byte Precision { get; set; }
                public byte Scale { get; set; }
                public int Size { get; set; }
                public DbType DbType { get; set; }
                public ParameterDirection Direction { get; set; }
                public bool IsNullable => true;
                [AllowNull]
                public string ParameterName { get; set; } = "";
                [AllowNull]
                public string SourceColumn { get; set; } = "";
                public DataRowVersion SourceVersion { get; set; }
                public object? Value { get; set; }
            }

            private sealed class NoopReader : IDataReader {
                public object this[int i] => 1;
                public object this[string name] => 1;
                public int Depth => 0;
                public bool IsClosed => false;
                public int RecordsAffected => 1;
                public int FieldCount => 1;
                public void Close() { }
                public void Dispose() { }
                public bool GetBoolean(int i) => true;
                public byte GetByte(int i) => 0;
                public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => 0;
                public char GetChar(int i) => 'a';
                public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => 0;
                public IDataReader GetData(int i) => this;
                public string GetDataTypeName(int i) => "int";
                public DateTime GetDateTime(int i) => DateTime.UtcNow;
                public decimal GetDecimal(int i) => 0;
                public double GetDouble(int i) => 0;
                public Type GetFieldType(int i) => typeof(int);
                public float GetFloat(int i) => 0;
                public Guid GetGuid(int i) => Guid.Empty;
                public short GetInt16(int i) => 0;
                public int GetInt32(int i) => 1;
                public long GetInt64(int i) => 1;
                public string GetName(int i) => "c";
                public int GetOrdinal(string name) => 0;
                public DataTable GetSchemaTable() => new();
                public string GetString(int i) => "x";
                public object GetValue(int i) => 1;
                public int GetValues(object[] values) { values[0] = 1; return 1; }
                public bool IsDBNull(int i) => false;
                public bool NextResult() => false;
                public bool Read() => false;
            }
        }

        // Minimal DbConnection/DbCommand pair to exercise the ADO wrapper (no provider packages).
        private sealed class MiniDbConnection : System.Data.Common.DbConnection {
            [AllowNull] public override string ConnectionString { get; set; } = "Fake";
            public override string Database => "Fake";
            public override string DataSource => "Fake";
            public override string ServerVersion => "0.0";
            public override ConnectionState State => ConnectionState.Open;
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }
            protected override System.Data.Common.DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
            protected override System.Data.Common.DbCommand CreateDbCommand() => new MiniDbCommand { Connection = this };
        }

        private sealed class MiniDbCommand : System.Data.Common.DbCommand {
            [AllowNull] public override string CommandText { get; set; } = string.Empty;
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; } = CommandType.Text;
            protected override System.Data.Common.DbConnection? DbConnection { get; set; }
            protected override System.Data.Common.DbParameterCollection DbParameterCollection { get; } = new MiniParamCollection();
            protected override System.Data.Common.DbTransaction? DbTransaction { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            public override void Cancel() { }
            public override int ExecuteNonQuery() => 1;
            public override object? ExecuteScalar() => 1;
            public override void Prepare() { }
            protected override System.Data.Common.DbParameter CreateDbParameter() => new MiniParam();
            protected override System.Data.Common.DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();

            private sealed class MiniParamCollection : System.Data.Common.DbParameterCollection {
                private readonly List<object> _list = [];
                public override object SyncRoot => this;
                public override int Count => _list.Count;
                public override int Add(object value) { _list.Add(value); return _list.Count - 1; }
                public override void AddRange(Array values) => _list.AddRange(values.Cast<object>());
                public override void Clear() => _list.Clear();
                public override bool Contains(object value) => _list.Contains(value);
                public override bool Contains(string value) => false;
                public override void CopyTo(Array array, int index) => _list.ToArray().CopyTo(array, index);
                public override System.Collections.IEnumerator GetEnumerator() => _list.GetEnumerator();
                public override int IndexOf(object value) => _list.IndexOf(value);
                public override int IndexOf(string parameterName) => -1;
                public override void Insert(int index, object value) => _list.Insert(index, value);
                public override void Remove(object value) => _list.Remove(value);
                public override void RemoveAt(int index) => _list.RemoveAt(index);
                public override void RemoveAt(string parameterName) { }
                protected override System.Data.Common.DbParameter GetParameter(int index) => (System.Data.Common.DbParameter)_list[index]!;
                protected override System.Data.Common.DbParameter GetParameter(string parameterName) => throw new NotSupportedException();
                protected override void SetParameter(int index, System.Data.Common.DbParameter value) { if (index < _list.Count) _list[index] = value; else _list.Add(value); }
                protected override void SetParameter(string parameterName, System.Data.Common.DbParameter value) => _list.Add(value);
            }

            private sealed class MiniParam : System.Data.Common.DbParameter {
                public override DbType DbType { get; set; }
                public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
                public override bool IsNullable { get; set; }
                [AllowNull] public override string ParameterName { get; set; } = "";
                [AllowNull] public override string SourceColumn { get; set; } = "";
                public override object? Value { get; set; }
                public override bool SourceColumnNullMapping { get; set; }
                public override int Size { get; set; }
                public override void ResetDbType() { }
            }
        }
        #endregion
    }
}
