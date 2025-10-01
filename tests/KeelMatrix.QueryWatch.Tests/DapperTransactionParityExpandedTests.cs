using System;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using KeelMatrix.QueryWatch.Dapper;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class DapperTransactionParityExpandedTests {
        [Fact]
        public void Reassign_Transaction_After_Execute_RoundTrips_Inner_Correctly() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            var provider = new OnlyIdbConnection();
            using var wrapped = new DapperQueryWatchConnection(provider, session);

            var dcmd = (DapperQueryWatchCommand)wrapped.CreateCommand();
            dcmd.CommandText = "UPDATE T SET X=1";
            var inner = (OnlyIdbCommand)DapperCommandIntrospectionExtensions.GetInnerForTest(dcmd);

            dcmd.ExecuteNonQuery();

            var tx1 = (DapperQueryWatchTransaction)wrapped.BeginTransaction();
            dcmd.Transaction = tx1;
            inner.LastAssignedTransaction.Should().BeSameAs(tx1.Inner);

            var tx2 = (DapperQueryWatchTransaction)wrapped.BeginTransaction();
            dcmd.Transaction = tx2;
            inner.LastAssignedTransaction.Should().BeSameAs(tx2.Inner);

            dcmd.Transaction = null;
            inner.LastAssignedTransaction.Should().BeNull();
        }

        [Fact]
        public void Reassign_Connection_After_Execute_RoundTrips_Inner_Correctly() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            var provider = new OnlyIdbConnection();
            using var wrapped = new DapperQueryWatchConnection(provider, session);

            var dcmd = (DapperQueryWatchCommand)wrapped.CreateCommand();
            var inner = (OnlyIdbCommand)DapperCommandIntrospectionExtensions.GetInnerForTest(dcmd);

            dcmd.CommandText = "SELECT 1";
            dcmd.ExecuteNonQuery();

            dcmd.Connection = wrapped;
            inner.LastAssignedConnection.Should().BeSameAs(provider);

            dcmd.Connection = null;
            inner.LastAssignedConnection.Should().BeNull();
            dcmd.Connection = wrapped;
            inner.LastAssignedConnection.Should().BeSameAs(provider);
        }

        // ----- Minimal fakes -----
        private sealed class OnlyIdbConnection : IDbConnection {
            [AllowNull]
            public string ConnectionString { get; set; } = string.Empty;
            public int ConnectionTimeout => 0;
            public string Database => "OnlyIdb";
            public ConnectionState State => ConnectionState.Open;
            public IDbTransaction BeginTransaction() => new OnlyIdbTransaction(this);
            public IDbTransaction BeginTransaction(IsolationLevel il) => new OnlyIdbTransaction(this);
            public void ChangeDatabase(string databaseName) { }
            public void Close() { }
            public IDbCommand CreateCommand() => new OnlyIdbCommand(this);
            public void Open() { }
            public void Dispose() { }
        }

        private sealed class OnlyIdbTransaction : IDbTransaction {
            public OnlyIdbTransaction(IDbConnection conn) { Connection = conn; }
            public IDbConnection Connection { get; }
            public IsolationLevel IsolationLevel => IsolationLevel.Unspecified;
            public void Commit() { }
            public void Rollback() { }
            public void Dispose() { }
        }

        private sealed class OnlyIdbCommand : IDbCommand {
            private string _text = string.Empty;
            private IDbConnection? _conn;
            private IDbTransaction? _tx;

            public IDbConnection? LastAssignedConnection { get; private set; }
            public IDbTransaction? LastAssignedTransaction { get; private set; }

            public OnlyIdbCommand(IDbConnection conn) { _conn = conn; }
            [AllowNull]
            public string CommandText { get => _text; set => _text = value ?? string.Empty; }
            public int CommandTimeout { get; set; }
            public CommandType CommandType { get; set; }
            public IDbConnection? Connection { get => _conn; set { _conn = value; LastAssignedConnection = value; } }
            public IDataParameterCollection Parameters { get; } = new DummyParameters();
            public IDbTransaction? Transaction { get => _tx; set { _tx = value; LastAssignedTransaction = value; } }
            public UpdateRowSource UpdatedRowSource { get; set; }

            public void Cancel() { }
            public IDbDataParameter CreateParameter() => new DummyParam();
            public void Prepare() { }
            public int ExecuteNonQuery() => 1;
            public IDataReader ExecuteReader() => new DummyReader();
            public IDataReader ExecuteReader(CommandBehavior behavior) => new DummyReader();
            public object? ExecuteScalar() => 1;
            public void Dispose() { }

            private sealed class DummyReader : IDataReader {
                public int Depth => 0; public bool IsClosed => false; public int RecordsAffected => 1; public int FieldCount => 0;
                public object this[int i] => 0; public object this[string name] => 0;
                public void Close() { }
                public void Dispose() { }
                public bool GetBoolean(int i) => false; public byte GetByte(int i) => 0; public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferoffset, int length) => 0;
                public char GetChar(int i) => '\0'; public long GetChars(int i, long fieldoffset, char[]? buffer, int bufferoffset, int length) => 0;
                public IDataReader GetData(int i) => this; public string GetDataTypeName(int i) => string.Empty; public DateTime GetDateTime(int i) => DateTime.MinValue;
                public decimal GetDecimal(int i) => 0; public double GetDouble(int i) => 0; public Type GetFieldType(int i) => typeof(object);
                public float GetFloat(int i) => 0; public Guid GetGuid(int i) => Guid.Empty; public short GetInt16(int i) => 0; public int GetInt32(int i) => 0; public long GetInt64(int i) => 0;
                public string GetName(int i) => string.Empty; public int GetOrdinal(string name) => -1; public DataTable GetSchemaTable() => new();
                public string GetString(int i) => string.Empty; public object GetValue(int i) => 0; public int GetValues(object[] values) => 0; public bool IsDBNull(int i) => false;
                public bool NextResult() => false; public bool Read() => false;
            }

            private sealed class DummyParam : IDbDataParameter {
                public byte Precision { get; set; }
                public byte Scale { get; set; }
                public int Size { get; set; }
                public DbType DbType { get; set; }
                public ParameterDirection Direction { get; set; }
                public bool IsNullable => true;
                [AllowNull]
                public string ParameterName { get; set; } = string.Empty;
                [AllowNull]
                public string SourceColumn { get; set; } = string.Empty; public DataRowVersion SourceVersion { get; set; }
                public object? Value { get; set; }
            }

            private sealed class DummyParameters : IDataParameterCollection {
                private readonly System.Collections.ArrayList _list = [];
                public object this[string parameterName] { get => null!; set { _ = value; } }
                public object? this[int index] { get => _list[index]; set => _list[index] = value; }
                public bool IsFixedSize => false; public bool IsReadOnly => false; public int Count => _list.Count;
                public bool IsSynchronized => false; public object SyncRoot => this;
                public int Add(object? value) => _list.Add(value);
                public void Clear() => _list.Clear();
                public bool Contains(object? value) => _list.Contains(value);
                public bool Contains(string parameterName) => false;
                public void CopyTo(Array array, int index) => _list.CopyTo(array, index);
                public System.Collections.IEnumerator GetEnumerator() => _list.GetEnumerator();
                public int IndexOf(object? value) => _list.IndexOf(value);
                public int IndexOf(string parameterName) => -1;
                public void Insert(int index, object? value) => _list.Insert(index, value);
                public void Remove(object? value) => _list.Remove(value);
                public void RemoveAt(int index) => _list.RemoveAt(index);
                public void RemoveAt(string parameterName) { }
            }
        }

        internal static class DapperCommandIntrospectionExtensions {
            public static IDbCommand GetInnerForTest(DapperQueryWatchCommand cmd) {
                var f = typeof(DapperQueryWatchCommand).GetField("_inner", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return (IDbCommand)f!.GetValue(cmd)!;
            }
        }
    }
}
