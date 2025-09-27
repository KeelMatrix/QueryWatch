using System.Data;
using System.Data.Common;
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class AdoTransactionWrapperTests {
        [Fact]
        public void BeginTransaction_Returns_Wrapper_With_WrapperConnection_And_Exposes_Inner() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            var provider = new TxFakeDbConnection();
            using var wrapped = new QueryWatchConnection(provider, session);

            using var tx = wrapped.BeginTransaction();
            tx.Should().BeOfType<QueryWatchTransaction>();

            ((IDbTransaction)tx).Connection.Should().BeSameAs(wrapped);
            var inner = ((QueryWatchTransaction)tx).Inner;
            inner.Should().BeOfType<TxFakeDbTransaction>();
            ((IDbTransaction)inner).Connection.Should().BeSameAs(provider);
        }

        [Fact]
        public void Transaction_Lifecycle_Delegates_Commit_Rollback_Dispose_To_Inner() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            var provider = new TxFakeDbConnection();
            using var wrapped = new QueryWatchConnection(provider, session);

            var tx1 = (QueryWatchTransaction)wrapped.BeginTransaction();
            var inner1 = (TxFakeDbTransaction)tx1.Inner;
            tx1.Commit();
            inner1.Committed.Should().BeTrue();

            var tx2 = (QueryWatchTransaction)wrapped.BeginTransaction();
            var inner2 = (TxFakeDbTransaction)tx2.Inner;
            tx2.Rollback();
            inner2.RolledBack.Should().BeTrue();

            var tx3 = (QueryWatchTransaction)wrapped.BeginTransaction();
            var inner3 = (TxFakeDbTransaction)tx3.Inner;
            tx3.Dispose();
            inner3.Disposed.Should().BeTrue();
        }

        [Fact]
        public void Command_Transaction_Setter_Unwraps_Wrapper_To_Inner() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            var provider = new TxFakeDbConnection();
            using var wrapped = new QueryWatchConnection(provider, session);

            var innerCmd = new CapturingDbCommand();
            using var cmd = new QueryWatchCommand(innerCmd, session, wrapped);

            var wtx = (QueryWatchTransaction)wrapped.BeginTransaction();
            cmd.Transaction = wtx;

            innerCmd.LastAssignedTransaction.Should().BeSameAs(wtx.Inner);
        }

        // ----- Fakes -----

        private sealed class TxFakeDbConnection : DbConnection {
            public bool Opened { get; private set; } = true;

            public override string ConnectionString { get; set; } = string.Empty;
            public override string Database => "TxFake";
            public override string DataSource => "TxFake";
            public override string ServerVersion => "1.0";
            public override ConnectionState State => Opened ? ConnectionState.Open : ConnectionState.Closed;
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { Opened = false; }
            public override void Open() { Opened = true; }
            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => new TxFakeDbTransaction(this, isolationLevel);
            protected override DbCommand CreateDbCommand() => new CapturingDbCommand();
        }

        private sealed class TxFakeDbTransaction : DbTransaction, IDbTransaction {
            private readonly TxFakeDbConnection _conn;
            private readonly IsolationLevel _iso;

            public bool Committed { get; private set; }
            public bool RolledBack { get; private set; }
            public bool Disposed { get; private set; }

            public TxFakeDbTransaction(TxFakeDbConnection conn, IsolationLevel iso) {
                _conn = conn;
                _iso = iso;
            }

            protected override DbConnection DbConnection => _conn;
            public override IsolationLevel IsolationLevel => _iso;

            public override void Commit() => Committed = true;
            public override void Rollback() => RolledBack = true;
            protected override void Dispose(bool disposing) { if (disposing) Disposed = true; base.Dispose(disposing); }
        }

        private sealed class CapturingDbCommand : DbCommand {
            private string _commandText = string.Empty;
            private int _timeout;
            private CommandType _type;
            private UpdateRowSource _updateRowSource;
            private readonly FakeDbParameterCollection _parameters = new FakeDbParameterCollection();

            public DbTransaction? LastAssignedTransaction { get; private set; }

            public override string CommandText { get => _commandText; set => _commandText = value ?? string.Empty; }
            public override int CommandTimeout { get => _timeout; set => _timeout = value; }
            public override CommandType CommandType { get => _type; set => _type = value; }
            public override UpdateRowSource UpdatedRowSource { get => _updateRowSource; set => _updateRowSource = value; }

            protected override DbConnection DbConnection { get; set; } = null!;
            protected override DbTransaction? DbTransaction { get => LastAssignedTransaction; set => LastAssignedTransaction = value; }
            protected override DbParameterCollection DbParameterCollection => _parameters;
            public override bool DesignTimeVisible { get; set; }

            public override void Cancel() { }
            public override int ExecuteNonQuery() => 1;
            public override object? ExecuteScalar() => null;
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
                protected override DbParameter GetParameter(string parameterName) => (DbParameter)_list[0]!;
                public override int IndexOf(object value) => _list.IndexOf(value);
                public override int IndexOf(string parameterName) => 0;
                public override void Insert(int index, object value) => _list.Insert(index, value);
                public override bool IsFixedSize => false;
                public override bool IsReadOnly => false;
                public override bool IsSynchronized => false;
                public override void Remove(object value) => _list.Remove(value);
                public override void RemoveAt(int index) => _list.RemoveAt(index);
                public override void RemoveAt(string parameterName) { if (_list.Count > 0) _list.RemoveAt(0); }
                protected override void SetParameter(int index, DbParameter value) => _list[index] = value;
                protected override void SetParameter(string parameterName, DbParameter value) { if (_list.Count > 0) _list[0] = value; }
                public override object SyncRoot => this;
            }

            private sealed class FakeDbParameter : DbParameter {
                public override DbType DbType { get; set; }
                public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
                public override bool IsNullable { get; set; }
                public override string ParameterName { get; set; } = string.Empty;
                public override string SourceColumn { get; set; } = string.Empty;
                public override object? Value { get; set; }
                public override bool SourceColumnNullMapping { get; set; }
                public override int Size { get; set; }
                public override void ResetDbType() { }
            }
        }
    }
}
