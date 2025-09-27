using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using KeelMatrix.QueryWatch.Dapper;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class ExtensionHelpersOverloadsTests {
        [Fact]
        public void Ado_Extensions_WithQueryWatch_Overloads_Work_For_DbConnection_And_IDbConnection() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            var provider = new FakeDbConnection();

            // DbConnection overload
            var w1 = KeelMatrix.QueryWatch.Ado.QueryWatchConnectionExtensions.WithQueryWatch((DbConnection)provider, session);
            w1.Should().BeOfType<QueryWatchConnection>();

            // IDbConnection overload with Db-derived type must also return QueryWatchConnection
            var w2 = KeelMatrix.QueryWatch.Ado.QueryWatchConnectionExtensions.WithQueryWatch((IDbConnection)provider, session);
            w2.Should().BeOfType<QueryWatchConnection>();
        }

        [Fact]
        public void Ado_Extension_WithQueryWatch_On_NonDbConnection_Throws_NotSupported() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            IDbConnection onlyIdb = new OnlyIdbConnection();

            Action act = () => KeelMatrix.QueryWatch.Ado.QueryWatchConnectionExtensions.WithQueryWatch(onlyIdb, session);
            act.Should().Throw<NotSupportedException>();
        }

        [Fact]
        public void Dapper_Extension_Prefers_Ado_Wrapper_For_DbDerived() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            IDbConnection provider = new FakeDbConnection();

            var wrapped = KeelMatrix.QueryWatch.Dapper.DapperQueryWatchExtensions.WithQueryWatch(provider, session);
            wrapped.Should().BeOfType<QueryWatchConnection>();
        }

        // --- Fakes ---
        private sealed class FakeDbConnection : DbConnection, IDbConnection {
            [AllowNull]
            public override string ConnectionString { get; set; } = string.Empty;
            public override string Database => "Fake";
            public override string DataSource => "Fake";
            public override string ServerVersion => "1.0";
            public override ConnectionState State => ConnectionState.Open;
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
            protected override DbCommand CreateDbCommand() => new FakeDbCommand();
        }

        private sealed class FakeDbCommand : DbCommand {
            [AllowNull]
            public override string CommandText { get; set; } = string.Empty;
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; } = CommandType.Text;
            public override UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;
            protected override DbConnection? DbConnection { get; set; }
            protected override DbParameterCollection DbParameterCollection { get; } = new FakeParams();
            protected override DbTransaction? DbTransaction { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override void Cancel() { }
            public override int ExecuteNonQuery() => 1;
            public override object? ExecuteScalar() => 42;
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => new FakeParam();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();

            private sealed class FakeParams : DbParameterCollection {
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

            private sealed class FakeParam : DbParameter {
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

        private sealed class OnlyIdbConnection : IDbConnection {
            public string ConnectionString { get; set; } = string.Empty;
            public int ConnectionTimeout => 15;
            public string Database => "Fake";
            public ConnectionState State { get; private set; } = ConnectionState.Open;
            public IDbTransaction BeginTransaction() => new OnlyIdbTransaction(this);
            public IDbTransaction BeginTransaction(IsolationLevel il) => new OnlyIdbTransaction(this);
            public void ChangeDatabase(string databaseName) { }
            public void Close() => State = ConnectionState.Closed;
            public IDbCommand CreateCommand() => new OnlyIdbCommand(this);
            public void Open() => State = ConnectionState.Open;
            public void Dispose() { }
        }

        private sealed class OnlyIdbTransaction : IDbTransaction {
            public OnlyIdbTransaction(IDbConnection conn) { Connection = conn; }
            public IDbConnection Connection { get; }
            public IsolationLevel IsolationLevel => IsolationLevel.ReadCommitted;
            public void Commit() { }
            public void Dispose() { }
            public void Rollback() { }
        }

        private sealed class OnlyIdbCommand : IDbCommand {
            private readonly IDbConnection _conn;
            public OnlyIdbCommand(IDbConnection conn) { _conn = conn; }
            public string CommandText { get; set; } = string.Empty;
            public int CommandTimeout { get; set; }
            public CommandType CommandType { get; set; } = CommandType.Text;
            public IDbConnection? Connection { get => _conn; set { } }
            public IDataParameterCollection Parameters { get; } = new FakeParams();
            public IDbTransaction? Transaction { get; set; }
            public UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;
            public void Cancel() { }
            public IDbDataParameter CreateParameter() => new FakeParameter();
            public void Dispose() { }
            public int ExecuteNonQuery() => 1;
            public IDataReader ExecuteReader() => new FakeReader();
            public IDataReader ExecuteReader(CommandBehavior behavior) => new FakeReader();
            public object? ExecuteScalar() => 42;
            public void Prepare() { }
        }

        private sealed class FakeParameter : IDbDataParameter {
            public byte Precision { get; set; }
            public byte Scale { get; set; }
            public int Size { get; set; }
            public DbType DbType { get; set; }
            public ParameterDirection Direction { get; set; } = ParameterDirection.Input;
            public bool IsNullable => true;
            public string ParameterName { get; set; } = string.Empty;
            public string SourceColumn { get; set; } = string.Empty;
            public DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;
            public object? Value { get; set; }
        }

        private sealed class FakeParams : System.Collections.ArrayList, IDataParameterCollection {
            public object? this[string parameterName] { get => null; set { } }
            public bool Contains(string parameterName) => false;
            public int IndexOf(string parameterName) => -1;
            public void RemoveAt(string parameterName) { }
        }

        private sealed class FakeReader : IDataReader {
            public int Depth => 0;
            public bool IsClosed => false;
            public int RecordsAffected => 0;
            public int FieldCount => 0;
            public void Close() { }
            public void Dispose() { }
            public bool GetBoolean(int i) => false;
            public byte GetByte(int i) => 0;
            public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => 0;
            public char GetChar(int i) => '\0';
            public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => 0;
            public IDataReader GetData(int i) => this;
            public string GetDataTypeName(int i) => string.Empty;
            public DateTime GetDateTime(int i) => default;
            public decimal GetDecimal(int i) => 0m;
            public double GetDouble(int i) => 0d;
            public Type GetFieldType(int i) => typeof(object);
            public float GetFloat(int i) => 0f;
            public Guid GetGuid(int i) => Guid.Empty;
            public short GetInt16(int i) => 0;
            public int GetInt32(int i) => 0;
            public long GetInt64(int i) => 0L;
            public string GetName(int i) => string.Empty;
            public int GetOrdinal(string name) => 0;
            public string GetString(int i) => string.Empty;
            public object GetValue(int i) => DBNull.Value;
            public int GetValues(object[] values) => 0;
            public bool IsDBNull(int i) => true;
            public bool NextResult() => false;
            public bool Read() => false;

            public DataTable? GetSchemaTable() {
                throw new NotImplementedException();
            }

            public object this[int i] => DBNull.Value;
            public object this[string name] => DBNull.Value;
        }
    }
}
