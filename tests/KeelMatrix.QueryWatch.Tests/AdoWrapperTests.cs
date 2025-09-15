using System;
using System.Data;
using System.Data.Common;
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class AdoWrapperTests {
        [Fact]
        public void QueryWatchCommand_ExecuteNonQuery_Records_One_Event_With_Text() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            var inner = new FakeDbCommand { CommandText = "SELECT 1" };
            using var cmd = new QueryWatchCommand(inner, session);

            var result = cmd.ExecuteNonQuery();
            result.Should().Be(1);

            var report = session.Stop();
            report.TotalQueries.Should().Be(1);
            report.Events[0].CommandText.Should().Be("SELECT 1");
        }

        [Fact]
        public void QueryWatchConnection_CreateDbCommand_Wraps_Inner_Command() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            using var innerConn = new FakeDbConnection();
            using var wrapped = new QueryWatchConnection(innerConn, session);

            using var cmd = wrapped.CreateCommand();
            cmd.Should().BeOfType<QueryWatchCommand>();

            cmd.CommandText = "UPDATE X";
            cmd.ExecuteNonQuery();

            var report = session.Stop();
            report.TotalQueries.Should().Be(1);
            report.Events[0].CommandText.Should().Be("UPDATE X");
        }

        private sealed class FakeDbConnection : DbConnection {
            public override string ConnectionString { get; set; } = "";
            public override string Database => "FakeDb";
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
            private string _commandText = string.Empty;
            public override string CommandText { get => _commandText; set => _commandText = value ?? string.Empty; }
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
                public override string ParameterName { get; set; } = "";
                public override string SourceColumn { get; set; } = "";
                public override object? Value { get; set; }
                public override bool SourceColumnNullMapping { get; set; }
                public override int Size { get; set; }
                public override void ResetDbType() { }
            }
        }
    }
}
