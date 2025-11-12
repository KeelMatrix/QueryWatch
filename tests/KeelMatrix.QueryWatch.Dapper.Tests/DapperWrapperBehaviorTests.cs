// Copyright (c) KeelMatrix
using System.Data;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using KeelMatrix.QueryWatch.Ado;
using Xunit;

namespace KeelMatrix.QueryWatch.Dapper.Tests {
    public class DapperWrapperBehaviorTests {
        [Fact]
        public void WithQueryWatch_Returns_Ado_Wrapper_For_DbConnection() {
            using QueryWatchSession session = new();
            IDbConnection raw = new MiniDbConnection();

            IDbConnection wrapped = raw.WithQueryWatch(session);

            _ = wrapped.Should().BeOfType<QueryWatchConnection>();
        }

        // Minimal DbConnection just for this test
        private sealed class MiniDbConnection : System.Data.Common.DbConnection {
            public override string Database => "Fake";
            public override string DataSource => "Fake";
            public override string ServerVersion => "0.0";
            public override ConnectionState State => ConnectionState.Closed;
            public override void ChangeDatabase(string databaseName) { }
            public override void Close() { }
            public override void Open() { }
            protected override System.Data.Common.DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => throw new NotSupportedException();
            protected override System.Data.Common.DbCommand CreateDbCommand() => throw new NotSupportedException();
            [AllowNull]
            public override string ConnectionString { get; set; } = "Fake";
        }
    }
}
