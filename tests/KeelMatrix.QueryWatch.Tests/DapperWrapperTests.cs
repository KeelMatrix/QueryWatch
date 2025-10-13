using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using KeelMatrix.QueryWatch.Dapper;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class DapperWrapperTests {
        [Fact]
        public void CreateCommand_Wraps_And_Records_On_ExecuteNonQuery() {
            using var session = QueryWatcher.Start();
            var inner = new OnlyIdbConnection();
            using var wrapped = new DapperQueryWatchConnection(inner, session);

            using var cmd = wrapped.CreateCommand();
            cmd.Should().BeOfType<DapperQueryWatchCommand>();

            cmd.CommandText = "DELETE FROM T";
            var n = cmd.ExecuteNonQuery();
            n.Should().Be(1);

            var report = session.Stop();
            report.TotalQueries.Should().Be(1);
            report.Events[0].CommandText.Should().Be("DELETE FROM T");
        }

        [Fact]
        public void ExecuteScalar_And_Reader_Record_Events() {
            using var session = QueryWatcher.Start();
            using var wrapped = new DapperQueryWatchConnection(new OnlyIdbConnection(), session);

            using (var cmd = wrapped.CreateCommand()) {
                cmd.CommandText = "SELECT 42";
                cmd.ExecuteScalar().Should().Be(42);
            }
            using (var cmd = wrapped.CreateCommand()) {
                cmd.CommandText = "SELECT * FROM X";
                using var reader = cmd.ExecuteReader();
                reader.Should().NotBeNull();
            }

            var report = session.Stop();
            report.TotalQueries.Should().Be(2);
        }

        [Fact]
        public void BeginTransaction_Preserves_Wrapper_Connection_And_Unwraps_Inner_On_Command_Setter() {
            using var session = QueryWatcher.Start();
            var inner = new OnlyIdbConnection();
            using var wrapped = new DapperQueryWatchConnection(inner, session);

            using var tx = (DapperQueryWatchTransaction)wrapped.BeginTransaction();
            tx.Connection.Should().Be(wrapped, "transaction.Connection must be the wrapper");

            using var cmd = (OnlyIdbCommand)((DapperQueryWatchCommand)wrapped.CreateCommand()).GetInnerForTest();
            var wrapperCmd = new DapperQueryWatchCommand(cmd, session, wrapped);
            // Assign the wrapped transaction to the command; inner command should receive the *inner* tx object.
            wrapperCmd.Transaction = tx;
            cmd.LastAssignedTransaction.Should().Be(tx.Inner);
        }

        [Fact]
        public void Command_Connection_Getter_Is_Wrapper_And_Setter_Unwraps() {
            using var session = QueryWatcher.Start();
            var inner = new OnlyIdbConnection();
            using var wrapped = new DapperQueryWatchConnection(inner, session);

            using var dapperCmd = (DapperQueryWatchCommand)wrapped.CreateCommand();
            dapperCmd.Connection.Should().Be(wrapped);

            // Set the connection to the wrapper again; inner command should receive inner.
            var innerCmd = (OnlyIdbCommand)dapperCmd.GetInnerForTest();
            dapperCmd.Connection = wrapped;
            innerCmd.LastAssignedConnection.Should().Be(inner);
        }

        [Fact]
        public void Dapper_Extension_WithQueryWatch_Chooses_Highest_Fidelity_Wrapper() {
            using var session = QueryWatcher.Start();

            // 1) If provider derives from DbConnection, we should get ADO wrapper (supports async).
            var dbDerived = new DbDerivedConnection();
            var wrapped1 = dbDerived.WithQueryWatch(session);
            wrapped1.Should().BeOfType<QueryWatchConnection>();

            // 2) If provider does not derive from DbConnection, we should get the Dapper-only wrapper.
            IDbConnection onlyIdb = new OnlyIdbConnection();
            var wrapped2 = QueryWatchExtensions.WithQueryWatch(onlyIdb, session);
            wrapped2.Should().BeOfType<DapperQueryWatchConnection>();
        }

        // ---- Fakes ----

        /// <summary>Provider that only implements IDbConnection (not DbConnection).</summary>
        private sealed class OnlyIdbConnection : IDbConnection {
            [AllowNull]
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
            [AllowNull]
            public string CommandText { get; set; } = string.Empty;
            public int CommandTimeout { get; set; }
            public CommandType CommandType { get; set; } = CommandType.Text;
            public IDbConnection? Connection { get => _conn; set { LastAssignedConnection = value; } }
            public IDataParameterCollection Parameters { get; } = new FakeParams();
            public IDbTransaction? Transaction { get => _tx; set { _tx = value; LastAssignedTransaction = value; } }
            public UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;
            private IDbTransaction? _tx;

            // Testing hooks
            public IDbConnection? LastAssignedConnection { get; private set; }
            public IDbTransaction? LastAssignedTransaction { get; private set; }

            public void Cancel() { }
            public IDbDataParameter CreateParameter() => new FakeParameter();
            public void Dispose() { }
            public int ExecuteNonQuery() => 1;
            public IDataReader ExecuteReader() => new FakeReader();
            public IDataReader ExecuteReader(CommandBehavior behavior) => new FakeReader();
            public object? ExecuteScalar() => 42;
            public void Prepare() { }

#pragma warning disable S1144 // allow DapperQueryWatchCommand to access inner in tests
            public IDbCommand GetInnerForTest() => this;
#pragma warning restore S1144
        }

        private sealed class FakeParameter : IDbDataParameter {
            public byte Precision { get; set; }
            public byte Scale { get; set; }
            public int Size { get; set; }
            public DbType DbType { get; set; }
            public ParameterDirection Direction { get; set; } = ParameterDirection.Input;
            public bool IsNullable => true;
            [AllowNull]
            public string ParameterName { get; set; } = string.Empty;
            [AllowNull]
            public string SourceColumn { get; set; } = string.Empty;
            public DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;
            public object? Value { get; set; }
        }

        private sealed class FakeParams : System.Collections.ArrayList, IDataParameterCollection {
            [AllowNull]
            public object this[string parameterName] { get => null!; set { _ = value; } }
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

        /// <summary>DbConnection-derived provider to exercise Dapper extension choosing the ADO wrapper.</summary>
        private sealed class DbDerivedConnection : DbConnection, IDbConnection {
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
            protected override DbCommand CreateDbCommand() => new DbDerivedCommand();
        }

        private sealed class DbDerivedCommand : DbCommand {
            [AllowNull]
            public override string CommandText { get; set; } = string.Empty;
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; } = CommandType.Text;
            public override UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;
            protected override DbConnection? DbConnection { get; set; }
            protected override DbParameterCollection DbParameterCollection { get; } = new FakeParamCollection();
            protected override DbTransaction? DbTransaction { get; set; }
            public override bool DesignTimeVisible { get; set; }
            public override void Cancel() { }
            public override int ExecuteNonQuery() => 1;
            public override object? ExecuteScalar() => 42;
            public override void Prepare() { }
            protected override DbParameter CreateDbParameter() => new DbDerivedParameter();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();

            private sealed class FakeParamCollection : DbParameterCollection {
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

            private sealed class DbDerivedParameter : DbParameter {
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

    internal static class DapperCommandIntrospectionExtensions {
        public static IDbCommand GetInnerForTest(this DapperQueryWatchCommand cmd) {
            // We can't access private fields; but we know DapperQueryWatchCommand wraps an inner IDbCommand,
            // and returns it via CreateCommand(); here we downcast the field via reflection for testing.
            var f = typeof(DapperQueryWatchCommand).GetField("_inner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (IDbCommand)f!.GetValue(cmd)!;
        }
    }
}
