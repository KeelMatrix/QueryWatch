// Copyright (c) KeelMatrix
#nullable enable
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace KeelMatrix.QueryWatch.EfCore.Tests {
    public sealed class EfCoreMetadataPolicyTests {
        [Fact]
        public void Failure_Emits_Normalized_Failure_Meta_With_Provider() {
            using var connection = SqliteTestUtils.CreateOpenConnection();
            using var session = new QueryWatchSession();
            var options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (var ctx = new TestDbContext(options)) {
                // Force a failure
                var ex = Record.Exception(() => ctx.Database.ExecuteSqlRaw("SELECT * FROM __NoSuchTable__"));
                ex.Should().NotBeNull();
            }

            var report = session.Stop();
            report.Events.Should().NotBeEmpty();
            var ev = report.Events[^1];
            ev.Meta.Should().NotBeNull();
            ev.Meta!.Should().ContainKey("failed").WhoseValue.Should().Be(true);
            ev.Meta!.Should().ContainKey("exception");
            ev.Meta!.Should().ContainKey("provider").WhoseValue.Should().Be("efcore");
        }

        [Fact]
        public void CaptureParameterShape_TopLevel_Flag_Is_Respected_By_EF() {
            using var connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            var opts = new QueryWatchOptions { CaptureParameterShape = true };
            using var session = new QueryWatchSession(opts);
            var options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (var ctx = new TestDbContext(options)) {
                // Ensure some parameterized query occurs
                ctx.Things.Add(new Thing { Id = 1, Name = "n1" });
                ctx.SaveChanges();
                var _ = ctx.Things.Where(t => t.Id == 1).Count();
            }

            // Export to JSON to validate serialized shape
            var tmp = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "QueryWatchTests", System.Guid.NewGuid().ToString("N"), "ef-meta.json");
            KeelMatrix.QueryWatch.Reporting.QueryWatchJson.ExportToFile(session.Stop(), tmp, 5);
            System.IO.File.Exists(tmp).Should().BeTrue();
            using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(tmp));
            var root = doc.RootElement;
            var events = root.GetProperty("events");
            events.GetArrayLength().Should().BeGreaterThan(0);
            var withMeta = events.EnumerateArray().FirstOrDefault(e => e.TryGetProperty("meta", out var m) && m.TryGetProperty("parameters", out var arr) && arr.GetArrayLength() > 0);
            withMeta.ValueKind.Should().NotBe(System.Text.Json.JsonValueKind.Undefined);
        }

        [Fact]
        public void PerAdapter_Disable_Text_Capture_Works_For_EF() {
            using var connection = SqliteTestUtils.CreateOpenConnection();
            SqliteTestUtils.EnsureCreated(connection);

            var opts = new QueryWatchOptions {
                CaptureSqlText = true,
                DisableEfCoreTextCapture = true
            };
            using var session = new QueryWatchSession(opts);
            var options = (DbContextOptions<TestDbContext>)new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(connection)
                .UseQueryWatch(session)
                .Options;

            using (var ctx = new TestDbContext(options)) {
                ctx.Things.Add(new Thing { Id = 2, Name = "n2" });
                ctx.SaveChanges();
            }

            var report = session.Stop();
            report.Events.Should().NotBeEmpty();
            report.Events.All(e => string.IsNullOrEmpty(e.CommandText)).Should().BeTrue();
        }
    }
}
