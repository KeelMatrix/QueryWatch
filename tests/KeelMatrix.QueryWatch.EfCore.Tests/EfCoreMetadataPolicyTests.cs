// Copyright (c) KeelMatrix
using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KeelMatrix.QueryWatch.EfCore.Tests {
    public sealed class EfCoreMetadataPolicyTests {
        [Fact]
        public void Failure_Emits_Normalized_Failure_Meta_With_Provider() {
            using SqliteConnection connection = SqliteTestUtils.CreateOpenConnection();
            using QueryWatchSession session = new();
            DbContextOptions<TestDbContext> options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (TestDbContext ctx = new(options)) {
                // Force a failure
                Exception ex = Record.Exception(() => ctx.Database.ExecuteSqlRaw("SELECT * FROM __NoSuchTable__"));
                _ = ex.Should().NotBeNull();
            }

            QueryWatchReport report = session.Stop();
            _ = report.Events.Should().NotBeEmpty();
            QueryEvent ev = report.Events[^1];
            _ = ev.Meta.Should().NotBeNull();
            _ = ev.Meta!.Should().ContainKey("failed").WhoseValue.Should().Be(true);
            _ = ev.Meta!.Should().ContainKey("exception");
            _ = ev.Meta!.Should().ContainKey("provider").WhoseValue.Should().Be("efcore");
        }

        [Fact]
        public void CaptureParameterShape_TopLevel_Flag_Is_Respected_By_EF() {
            using SqliteConnection connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            QueryWatchOptions opts = new() { CaptureParameterShape = true };
            using QueryWatchSession session = new(opts);
            DbContextOptions<TestDbContext> options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (TestDbContext ctx = new(options)) {
                // Ensure some parameterized query occurs
                _ = ctx.Things.Add(new Thing { Id = 1, Name = "n1" });
                _ = ctx.SaveChanges();
                _ = ctx.Things.Where(t => t.Id == 1).Count();
            }

            // Export to JSON to validate serialized shape
            var tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "QueryWatchTests", System.Guid.NewGuid().ToString("N"), "ef-meta.json");
            KeelMatrix.QueryWatch.Reporting.QueryWatchJson.ExportToFile(session.Stop(), tmp, 5);
            _ = System.IO.File.Exists(tmp).Should().BeTrue();
            using JsonDocument doc = JsonDocument.Parse(System.IO.File.ReadAllText(tmp));
            JsonElement root = doc.RootElement;
            JsonElement events = root.GetProperty("events");
            _ = events.GetArrayLength().Should().BeGreaterThan(0);
            JsonElement withMeta = events.EnumerateArray().FirstOrDefault(e => e.TryGetProperty("meta", out JsonElement m) && m.TryGetProperty("parameters", out JsonElement arr) && arr.GetArrayLength() > 0);
            _ = withMeta.ValueKind.Should().NotBe(System.Text.Json.JsonValueKind.Undefined);
        }

        [Fact]
        public void PerAdapter_Disable_Text_Capture_Works_For_EF() {
            using SqliteConnection connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            QueryWatchOptions opts = new() {
                CaptureSqlText = true,
                DisableEfCoreTextCapture = true
            };
            using QueryWatchSession session = new(opts);
            DbContextOptions<TestDbContext> options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (TestDbContext ctx = new(options)) {
                _ = ctx.Things.Add(new Thing { Id = 2, Name = "n2" });
                _ = ctx.SaveChanges();
            }

            QueryWatchReport report = session.Stop();
            _ = report.Events.Should().NotBeEmpty();
            _ = report.Events.All(e => string.IsNullOrEmpty(e.CommandText)).Should().BeTrue();
        }
    }
}
