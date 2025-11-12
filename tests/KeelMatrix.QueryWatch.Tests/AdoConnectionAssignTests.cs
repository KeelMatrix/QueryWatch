using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class AdoConnectionAssignTests {
        [Fact]
        public void DbConnection_Setter_Unwraps_QueryWatchConnection() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            FakeDbConnection providerConn = new();
            FakeDbCommand innerCmd = new();
            QueryWatchConnection wrapped = new(providerConn, session);
            using QueryWatchCommand cmd = new(innerCmd, session);

            // Assign wrapper into the command; inner command should receive provider "unwrapped".
            cmd.Connection = wrapped;
            _ = innerCmd.AssignedConnection.Should().Be(providerConn, "setter must unwrap wrapper to provider connection");
            _ = cmd.Connection.Should().Be(wrapped, "getter should return the wrapper when one is assigned");
        }

        [Fact]
        public void DbConnection_Setter_With_Raw_Provider_Clears_Wrapper() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            FakeDbConnection providerA = new();
            FakeDbConnection providerB = new();
            FakeDbCommand innerCmd = new();
            QueryWatchConnection wrapped = new(providerA, session);
            using QueryWatchCommand cmd = new(innerCmd, session, wrapped);

            // Reassign to raw provider B → wrapper should be cleared and reflected by getter.
            cmd.Connection = providerB;
            _ = innerCmd.AssignedConnection.Should().Be(providerB);
            _ = cmd.Connection.Should().Be(providerB);
        }

        [Fact]
        public void CreateDbCommand_From_Wrapped_Connection_Returns_Command_With_Wrapper_Connection() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            using FakeDbConnection provider = new();
            using QueryWatchConnection wrapped = new(provider, session);

            using DbCommand cmd = wrapped.CreateCommand();
            _ = cmd.Should().BeOfType<QueryWatchCommand>();
            _ = cmd.Connection.Should().Be(wrapped, "command.Connection should present the wrapper so caller keeps using it");
        }

        // --- Fakes ---
        private sealed class FakeDbConnection : DbConnection {
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
            private DbConnection? _conn;

            [AllowNull]
            public override string CommandText { get; set; } = string.Empty;
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; } = CommandType.Text;
            public override UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;

            protected override DbParameterCollection DbParameterCollection { get; } = new FakeDbParameterCollection();
            protected override DbTransaction? DbTransaction { get; set; }
            public override bool DesignTimeVisible { get; set; }

            internal DbConnection? AssignedConnection { get; private set; }

            protected override DbConnection? DbConnection {
                get => _conn;
                set { _conn = value; AssignedConnection = value; }
            }

            public override void Cancel() { }
            public override int ExecuteNonQuery() => 1;
            public override object? ExecuteScalar() => 42;
            public override void Prepare() { }

            protected override DbParameter CreateDbParameter() => new FakeDbParameter();
            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();

            private sealed class FakeDbParameterCollection : DbParameterCollection {
                private readonly System.Collections.ArrayList _list = [];
                public override int Add(object value) { _ = _list.Add(value); return _list.Count - 1; }
                public override void AddRange(Array values) { foreach (var v in values) _ = _list.Add(v); }
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
