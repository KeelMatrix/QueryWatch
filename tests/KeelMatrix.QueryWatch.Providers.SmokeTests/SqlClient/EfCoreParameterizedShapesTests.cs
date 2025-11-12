// Copyright (c) KeelMatrix
using System.Collections;
using System.Data;
using FluentAssertions;
using KeelMatrix.QueryWatch.EfCore;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests.SqlClient {
    [Collection("SmokeEnv")]
    public sealed class EfCoreParameterizedShapesTests {
        private static string? GetConnString() =>
            Environment.GetEnvironmentVariable("QWATCH__SQLSERVER__CS");

        [Fact]
        public void EfCore_FromSqlRaw_Parameter_Shapes_Are_Captured() {
            string? cs = GetConnString();
            DbContextOptionsBuilder<EfCtx> builder = new DbContextOptionsBuilder<EfCtx>().UseSqlServer(cs);

            using var session = new QueryWatchSession();
            _ = builder.UseQueryWatch(session);
            using var db = new EfCtx(builder.Options);

            // Ensure table exists and contains a row
            _ = db.Database.ExecuteSqlRaw("IF OBJECT_ID('dbo.QW_Items','U') IS NULL CREATE TABLE dbo.QW_Items (Id INT IDENTITY PRIMARY KEY, Name NVARCHAR(200));");
            _ = db.Database.ExecuteSqlRaw("INSERT INTO dbo.QW_Items(Name) VALUES (N'Bob');");

            // Parameterized raw SQL through EF Core (reader path)
            var param = new SqlParameter("@name", SqlDbType.NVarChar, 200) { Value = "Bob" };
            var list = db.Items.FromSqlRaw("SELECT TOP 1 * FROM dbo.QW_Items WHERE Name = @name", param).ToList();
            _ = list.Should().NotBeEmpty();

            QueryEvent ev = session.Stop().Events[^1];
            _ = ev.Meta.Should().NotBeNull();
            _ = ev.Meta.Should().ContainKey("parameters");

            IEnumerable enumerable = (ev.Meta!["parameters"] as System.Collections.IEnumerable)!;
            var shapes = enumerable.Cast<object>().ToList();
            _ = shapes.Should().Contain(s => s.GetType().GetProperty("Name")!.GetValue(s)!.ToString() == "@name");
        }
    }
}
