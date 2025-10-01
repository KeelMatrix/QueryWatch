using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using KeelMatrix.QueryWatch.Dapper;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class AdapterParityMetaTests {
        [Fact]
        public void Ado_Failure_Emits_Normalized_Failure_Meta() {
            using var session = QueryWatcher.Start();
            var inner = new ThrowingDbCommand { CommandText = "SELECT 1" };
            using var cmd = new QueryWatchCommand(inner, session);

            Action act = () => cmd.ExecuteNonQuery();
            act.Should().Throw<InvalidOperationException>();

            var ev = session.Stop().Events[^1];
            ev.Meta.Should().NotBeNull();
            ev.Meta!.Should().ContainKey("failed").WhoseValue.Should().Be(true);
            ev.Meta!.Should().ContainKey("exception");
            ev.Meta!.Should().ContainKey("provider").WhoseValue.Should().Be("ado");
        }

        [Fact]
        public void Dapper_Failure_Emits_Normalized_Failure_Meta() {
            using var session = QueryWatcher.Start();
            var inner = new ThrowingIdbCommand { CommandText = "UPDATE T SET X=1" };
            using var conn = new DapperQueryWatchConnection(new OnlyIdbConnection(), session);
            using var cmd = new DapperQueryWatchCommand(inner, session, conn);

            Action act = () => cmd.ExecuteNonQuery();
            act.Should().Throw<InvalidOperationException>();

            var ev = session.Stop().Events[^1];
            ev.Meta.Should().NotBeNull();
            ev.Meta!.Should().ContainKey("failed").WhoseValue.Should().Be(true);
            ev.Meta!.Should().ContainKey("exception");
            ev.Meta!.Should().ContainKey("provider").WhoseValue.Should().Be("dapper");
        }

        [Fact]
        public void PerAdapter_Disable_Text_Capture_Works_For_ADO_And_Dapper() {
            var opts = new QueryWatchOptions {
                CaptureSqlText = true,
                DisableAdoTextCapture = true,
                DisableDapperTextCapture = true
            };
            using var session = QueryWatcher.Start(opts);

            var adoInner = new NoopDbCommand { CommandText = "SELECT 42" };
            using var adoCmd = new QueryWatchCommand(adoInner, session);
            adoCmd.ExecuteNonQuery();

            var dapperInner = new NoopIdbCommand { CommandText = "SELECT 7" };
            using var conn = new DapperQueryWatchConnection(new OnlyIdbConnection(), session);
            using var dapperCmd = new DapperQueryWatchCommand(dapperInner, session, conn);
            dapperCmd.ExecuteNonQuery();

            var report = session.Stop();
            report.Events.Should().HaveCount(2);
            report.Events[0].CommandText.Should().BeEmpty();
            report.Events[1].CommandText.Should().BeEmpty();
        }

        #region Minimal fakes

        private sealed class ThrowingDbCommand : DbCommand {
            [AllowNull]
            public override string CommandText { get; set; } = string.Empty;
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            [AllowNull]
            protected override DbConnection DbConnection { get; set; } = new FakeDbConnection();
            protected override DbParameterCollection DbParameterCollection { get; } = new FakeDbParameterCollection();
            protected override DbTransaction? DbTransaction { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            public override void Cancel() { }
            public override int ExecuteNonQuery() => throw new InvalidOperationException("boom");
            public override object? ExecuteScalar() => throw new InvalidOperationException("boom");
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => new FakeDbParameter();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new InvalidOperationException("boom");
        }

        private sealed class NoopDbCommand : DbCommand {
            [AllowNull]
            public override string CommandText { get; set; } = string.Empty;
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            [AllowNull]
            protected override DbConnection DbConnection { get; set; } = new FakeDbConnection();
            protected override DbParameterCollection DbParameterCollection { get; } = new FakeDbParameterCollection();
            protected override DbTransaction? DbTransaction { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }
            public override void Cancel() { }
            public override int ExecuteNonQuery() => 1;
            public override object? ExecuteScalar() => 1;
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => new FakeDbParameter();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => new FakeDbDataReader();
        }

        private sealed class NoopIdbCommand : IDbCommand {
            [AllowNull]
            public string CommandText { get; set; } = string.Empty;
            public int CommandTimeout { get; set; }
            public CommandType CommandType { get; set; }
            public IDbConnection? Connection { get; set; }
            public IDataParameterCollection Parameters { get; } = new FakeDbParameterCollection();
            public IDbTransaction? Transaction { get; set; }
            public UpdateRowSource UpdatedRowSource { get; set; }
            public void Cancel() { }
            public IDbDataParameter CreateParameter() => new FakeDbParameter();
            public void Dispose() { }
            public int ExecuteNonQuery() => 1;
            public IDataReader ExecuteReader() => new System.Data.DataTable().CreateDataReader();
            public IDataReader ExecuteReader(CommandBehavior behavior) => new System.Data.DataTable().CreateDataReader();
            public object? ExecuteScalar() => 1;
            public void Prepare() { }
        }

        private sealed class ThrowingIdbCommand : IDbCommand {
            [AllowNull]
            public string CommandText { get; set; } = string.Empty;
            public int CommandTimeout { get; set; }
            public CommandType CommandType { get; set; }
            public IDbConnection? Connection { get; set; }
            public IDataParameterCollection Parameters { get; } = new FakeDbParameterCollection();
            public IDbTransaction? Transaction { get; set; }
            public UpdateRowSource UpdatedRowSource { get; set; }
            public void Cancel() { }
            public IDbDataParameter CreateParameter() => new FakeDbParameter();
            public void Dispose() { }
            public int ExecuteNonQuery() => throw new InvalidOperationException("boom");
            public IDataReader ExecuteReader() => throw new InvalidOperationException("boom");
            public IDataReader ExecuteReader(CommandBehavior behavior) => throw new InvalidOperationException("boom");
            public object? ExecuteScalar() => throw new InvalidOperationException("boom");
            public void Prepare() { }
        }

        private sealed class FakeDbConnection : DbConnection {
            [AllowNull]
            public override string ConnectionString { get; set; } = "Fake";
            public override string Database => "Fake";
            public override string DataSource => "Fake";
            public override string ServerVersion => "1.0";
            public override ConnectionState State => ConnectionState.Open;
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }
            protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel) => throw new NotSupportedException();
            protected override DbCommand CreateDbCommand() => new NoopDbCommand();
        }

        private sealed class FakeDbDataReader : DbDataReader {
            public override int Depth => 0;
            public override int FieldCount => 0;
            public override bool HasRows => false;
            public override bool IsClosed => false;
            public override int RecordsAffected => 0;
            public override object this[int ordinal] => throw new NotSupportedException();
            public override object this[string name] => throw new NotSupportedException();
            public override bool GetBoolean(int ordinal) => false;
            public override byte GetByte(int ordinal) => 0;
            public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
            public override char GetChar(int ordinal) => '\0';
            public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
            public override string GetDataTypeName(int ordinal) => string.Empty;
            public override DateTime GetDateTime(int ordinal) => DateTime.UtcNow;
            public override decimal GetDecimal(int ordinal) => 0;
            public override double GetDouble(int ordinal) => 0;
            public override System.Collections.IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();
            public override Type GetFieldType(int ordinal) => typeof(object);
            public override float GetFloat(int ordinal) => 0;
            public override Guid GetGuid(int ordinal) => Guid.Empty;
            public override short GetInt16(int ordinal) => 0;
            public override int GetInt32(int ordinal) => 0;
            public override long GetInt64(int ordinal) => 0;
            public override string GetName(int ordinal) => string.Empty;
            public override int GetOrdinal(string name) => -1;
            public override string GetString(int ordinal) => string.Empty;
            public override object GetValue(int ordinal) => new();
            public override int GetValues(object[] values) => 0;
            public override bool IsDBNull(int ordinal) => true;
            public override bool NextResult() => false;
            public override bool Read() => false;
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

            /// <summary>
            /// Required by DbParameter. For this fake parameter, it’s a no-op.
            /// Real providers would reset DbType back to their default.
            /// </summary>
            public override void ResetDbType() { }
        }

        private sealed class FakeDbParameterCollection : DbParameterCollection {
            private readonly System.Collections.ArrayList _list = [];
            public override int Add(object value) { _list.Add(value); return _list.Count - 1; }
            public override void AddRange(Array values) { _list.AddRange(values); }
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

        // Minimal IDbConnection used to create Dapper wrapper owners in tests.
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

        #endregion
    }
}
