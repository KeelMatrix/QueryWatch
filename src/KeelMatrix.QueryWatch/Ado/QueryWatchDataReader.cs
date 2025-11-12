// Copyright (c) KeelMatrix
using System.Data;
using System.Data.Common;

namespace KeelMatrix.QueryWatch.Ado {
    /// <summary>
    /// Thin delegating wrapper over <see cref="DbDataReader"/> so we can observe Close/Dispose
    /// and untrack it from the owning <see cref="QueryWatchTransaction"/> (if any).
    /// </summary>
    internal sealed class QueryWatchDataReader : DbDataReader {
        private readonly DbDataReader _inner;
        private readonly QueryWatchTransaction? _tracker;

        public QueryWatchDataReader(DbDataReader inner, QueryWatchTransaction? tracker) {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _tracker = tracker;
        }

        public override void Close() {
            try { _inner.Close(); }
            finally { _tracker?.UntrackReader(_inner); }
        }

        protected override void Dispose(bool disposing) {
            try { if (disposing) _inner.Dispose(); }
            finally { _tracker?.UntrackReader(_inner); }
            base.Dispose(disposing);
        }

        // ---- Forwarders ----
        public override int FieldCount => _inner.FieldCount;
        public override int Depth => _inner.Depth;
        public override bool HasRows => _inner.HasRows;
        public override bool IsClosed => _inner.IsClosed;
        public override int RecordsAffected => _inner.RecordsAffected;
        public override int VisibleFieldCount => _inner.VisibleFieldCount;
        public override object this[int ordinal] => _inner[ordinal];
        public override object this[string name] => _inner[name];

        public override bool Read() => _inner.Read();
        public override bool NextResult() => _inner.NextResult();

        public override string GetName(int ordinal) => _inner.GetName(ordinal);
        public override string GetDataTypeName(int ordinal) => _inner.GetDataTypeName(ordinal);
        public override Type GetFieldType(int ordinal) => _inner.GetFieldType(ordinal);
        public override object GetValue(int ordinal) => _inner.GetValue(ordinal);
        public override int GetValues(object[] values) => _inner.GetValues(values);
        public override int GetOrdinal(string name) => _inner.GetOrdinal(name);
        public override bool GetBoolean(int ordinal) => _inner.GetBoolean(ordinal);
        public override byte GetByte(int ordinal) => _inner.GetByte(ordinal);
        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => _inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
        public override char GetChar(int ordinal) => _inner.GetChar(ordinal);
        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => _inner.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
        public override Guid GetGuid(int ordinal) => _inner.GetGuid(ordinal);
        public override short GetInt16(int ordinal) => _inner.GetInt16(ordinal);
        public override int GetInt32(int ordinal) => _inner.GetInt32(ordinal);
        public override long GetInt64(int ordinal) => _inner.GetInt64(ordinal);
        public override DateTime GetDateTime(int ordinal) => _inner.GetDateTime(ordinal);
        public override string GetString(int ordinal) => _inner.GetString(ordinal);
        public override decimal GetDecimal(int ordinal) => _inner.GetDecimal(ordinal);
        public override double GetDouble(int ordinal) => _inner.GetDouble(ordinal);
        public override float GetFloat(int ordinal) => _inner.GetFloat(ordinal);
        public override bool IsDBNull(int ordinal) => _inner.IsDBNull(ordinal);
#if NETSTANDARD2_0
        public override DataTable GetSchemaTable() => _inner.GetSchemaTable();
#else
        public override DataTable? GetSchemaTable() => _inner.GetSchemaTable();
#endif

        public override System.Collections.IEnumerator GetEnumerator() => ((System.Collections.IEnumerable)_inner).GetEnumerator();

#if NET6_0_OR_GREATER
        public override Task<bool> ReadAsync(CancellationToken cancellationToken) => _inner.ReadAsync(cancellationToken);
        public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => _inner.NextResultAsync(cancellationToken);
        public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken) => _inner.IsDBNullAsync(ordinal, cancellationToken);
        public override T GetFieldValue<T>(int ordinal) => _inner.GetFieldValue<T>(ordinal);
        public override Task<T> GetFieldValueAsync<T>(int ordinal, CancellationToken cancellationToken) => _inner.GetFieldValueAsync<T>(ordinal, cancellationToken);
#else
        // netstandard2.0: let base implement async fallbacks; still forward sync generic
        public override T GetFieldValue<T>(int ordinal) => (T)_inner.GetValue(ordinal)!;
#endif
    }
}
