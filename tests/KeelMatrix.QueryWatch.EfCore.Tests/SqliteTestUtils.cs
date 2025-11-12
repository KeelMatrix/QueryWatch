// Copyright (c) KeelMatrix
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace KeelMatrix.QueryWatch.EfCore.Tests {
    internal static class SqliteTestUtils {
        public static SqliteConnection CreateOpenConnection() {
            SqliteConnection connection = new("Data Source=:memory:;Cache=Shared");
            connection.Open();
            return connection;
        }

        public static void EnsureCreated(SqliteConnection connection) {
            DbContextOptions<TestDbContext> options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .Options;

            using TestDbContext ctx = new(options);
            _ = ctx.Database.EnsureCreated();
        }
    }
}
