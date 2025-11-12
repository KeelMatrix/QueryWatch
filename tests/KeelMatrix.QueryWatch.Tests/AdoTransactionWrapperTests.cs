using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class AdoTransactionWrapperTests {
        [Fact]
        public void BeginTransaction_Returns_Wrapper_With_WrapperConnection_And_Exposes_Inner() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            TxFakeDbConnection provider = new();
            using QueryWatchConnection wrapped = new(provider, session);

            using DbTransaction tx = wrapped.BeginTransaction();
            _ = tx.Should().BeOfType<QueryWatchTransaction>();

            _ = ((IDbTransaction)tx).Connection.Should().BeSameAs(wrapped);
            DbTransaction inner = ((QueryWatchTransaction)tx).Inner;
            _ = inner.Should().BeOfType<TxFakeDbTransaction>();
            _ = ((IDbTransaction)inner).Connection.Should().BeSameAs(provider);
        }

        [Fact]
        public void Transaction_Lifecycle_Delegates_Commit_Rollback_Dispose_To_Inner() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            TxFakeDbConnection provider = new();
            using QueryWatchConnection wrapped = new(provider, session);

            QueryWatchTransaction tx1 = (QueryWatchTransaction)wrapped.BeginTransaction();
            TxFakeDbTransaction inner1 = (TxFakeDbTransaction)tx1.Inner;
            tx1.Commit();
            _ = inner1.Committed.Should().BeTrue();

            QueryWatchTransaction tx2 = (QueryWatchTransaction)wrapped.BeginTransaction();
            TxFakeDbTransaction inner2 = (TxFakeDbTransaction)tx2.Inner;
            tx2.Rollback();
            _ = inner2.RolledBack.Should().BeTrue();

            QueryWatchTransaction tx3 = (QueryWatchTransaction)wrapped.BeginTransaction();
            TxFakeDbTransaction inner3 = (TxFakeDbTransaction)tx3.Inner;
            tx3.Dispose();
            _ = inner3.Disposed.Should().BeTrue();
        }

        [Fact]
        public void Command_Transaction_Setter_Unwraps_Wrapper_To_Inner() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            TxFakeDbConnection provider = new();
            using QueryWatchConnection wrapped = new(provider, session);

            CapturingDbCommand innerCmd = new();
            using QueryWatchCommand cmd = new(innerCmd, session, wrapped);

            QueryWatchTransaction wtx = (QueryWatchTransaction)wrapped.BeginTransaction();
            cmd.Transaction = wtx;

            _ = innerCmd.LastAssignedTransaction.Should().BeSameAs(wtx.Inner);
        }

        // ----- Fakes -----

        private sealed class TxFakeDbConnection : DbConnection {
            public bool Opened { get; private set; } = true;
            [AllowNull]
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

            public bool Committed { get; private set; }
            public bool RolledBack { get; private set; }
            public bool Disposed { get; private set; }

            public TxFakeDbTransaction(TxFakeDbConnection conn, IsolationLevel iso) {
                _conn = conn;
                IsolationLevel = iso;
            }

            protected override DbConnection DbConnection => _conn;
            public override IsolationLevel IsolationLevel { get; }

            public override void Commit() => Committed = true;
            public override void Rollback() => RolledBack = true;
            protected override void Dispose(bool disposing) {
                if (disposing)
                    Disposed = true;
                base.Dispose(disposing);
            }
        }

        private sealed class CapturingDbCommand : DbCommand {
            private string _commandText = string.Empty;
            private readonly FakeDbParameterCollection _parameters = new();

            public DbTransaction? LastAssignedTransaction { get; private set; }

            [AllowNull]
            public override string CommandText { get => _commandText; set => _commandText = value ?? string.Empty; }
            public override int CommandTimeout { get; set; }
            public override CommandType CommandType { get; set; }
            public override UpdateRowSource UpdatedRowSource { get; set; }

            [AllowNull]
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
                private readonly System.Collections.ArrayList _list = [];
                public override int Add(object value) { _ = _list.Add(value); return _list.Count - 1; }
                public override void AddRange(Array values) { _list.AddRange(values); }
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
