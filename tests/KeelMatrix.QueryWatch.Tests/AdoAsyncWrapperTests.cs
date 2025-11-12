using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class AdoAsyncWrapperTests {
        [Fact]
        public async Task ExecuteNonQueryAsync_Records_Event() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            FakeAsyncDbCommand inner = new() { CommandText = "UPDATE T" };
            await using QueryWatchCommand cmd = new(inner, session);

            var n = await cmd.ExecuteNonQueryAsync(CancellationToken.None);
            _ = n.Should().Be(1);

            QueryWatchReport report = session.Stop();
            _ = report.TotalQueries.Should().Be(1);
            _ = report.Events[0].CommandText.Should().Be("UPDATE T");
        }

        [Fact]
        public async Task ExecuteScalarAsync_Records_Event() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            FakeAsyncDbCommand inner = new() { CommandText = "SELECT 42" };
            await using QueryWatchCommand cmd = new(inner, session);

            var v = await cmd.ExecuteScalarAsync(CancellationToken.None);
            _ = v.Should().Be(42);

            QueryWatchReport report = session.Stop();
            _ = report.TotalQueries.Should().Be(1);
            _ = report.Events[0].CommandText.Should().Be("SELECT 42");
        }

        [Fact]
        public async Task ExecuteReaderAsync_Records_Event() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            FakeAsyncDbCommand inner = new() { CommandText = "SELECT * FROM T" };
            await using QueryWatchCommand cmd = new(inner, session);

            await using DbDataReader reader = await cmd.ExecuteReaderAsync(CommandBehavior.Default, CancellationToken.None);
            _ = reader.Should().NotBeNull();

            QueryWatchReport report = session.Stop();
            _ = report.TotalQueries.Should().Be(1);
            _ = report.Events[0].CommandText.Should().Be("SELECT * FROM T");
        }

        // ---- Fakes for async surface ----
        private sealed class FakeAsyncDbCommand : DbCommand {
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

            protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => new FakeDbDataReader();

            public override Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken) => Task.FromResult(1);
            public override Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken) => Task.FromResult<object?>(42);
            protected override Task<DbDataReader> ExecuteDbDataReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken)
                => Task.FromResult<DbDataReader>(new FakeDbDataReader());

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

            private sealed class FakeDbDataReader : DbDataReader {
                public override object this[int ordinal] => throw new NotSupportedException();
                public override object this[string name] => throw new NotSupportedException();
                public override int Depth => 0;
                public override int FieldCount => 0;
                public override bool HasRows => false;
                public override bool IsClosed => false;
                public override int RecordsAffected => 0;
                public override bool GetBoolean(int ordinal) => throw new NotSupportedException();
                public override byte GetByte(int ordinal) => throw new NotSupportedException();
                public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
                public override char GetChar(int ordinal) => throw new NotSupportedException();
                public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => throw new NotSupportedException();
                public override string GetDataTypeName(int ordinal) => throw new NotSupportedException();
                public override DateTime GetDateTime(int ordinal) => throw new NotSupportedException();
                public override decimal GetDecimal(int ordinal) => throw new NotSupportedException();
                public override double GetDouble(int ordinal) => throw new NotSupportedException();
                public override System.Collections.IEnumerator GetEnumerator() => Array.Empty<int>().GetEnumerator();
                public override Type GetFieldType(int ordinal) => typeof(object);
                public override float GetFloat(int ordinal) => throw new NotSupportedException();
                public override Guid GetGuid(int ordinal) => throw new NotSupportedException();
                public override short GetInt16(int ordinal) => throw new NotSupportedException();
                public override int GetInt32(int ordinal) => throw new NotSupportedException();
                public override long GetInt64(int ordinal) => throw new NotSupportedException();
                public override string GetName(int ordinal) => throw new NotSupportedException();
                public override int GetOrdinal(string name) => throw new NotSupportedException();
                public override string GetString(int ordinal) => throw new NotSupportedException();
                public override object GetValue(int ordinal) => throw new NotSupportedException();
                public override int GetValues(object[] values) => 0;
                public override bool IsDBNull(int ordinal) => true;
                public override bool NextResult() => false;
                public override bool Read() => false;
                public override void Close() { }
            }
        }
    }
}
