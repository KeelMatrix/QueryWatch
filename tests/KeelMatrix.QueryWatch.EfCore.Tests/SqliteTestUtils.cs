// Copyright (c) KeelMatrix
#nullable enable
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KeelMatrix.QueryWatch.EfCore.Tests {
    internal static class SqliteTestUtils {
        public static SqliteConnection CreateOpenConnection() {
            var connection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
            connection.Open();
            return connection;
        }

        public static void EnsureCreated(SqliteConnection connection) {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .Options;

            using var ctx = new TestDbContext(options);
            ctx.Database.EnsureCreated();
        }
    }
}
