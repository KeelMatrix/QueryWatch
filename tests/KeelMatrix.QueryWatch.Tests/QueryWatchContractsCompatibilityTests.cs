
using System.IO;
using System.Text.Json;
using FluentAssertions;
using KeelMatrix.QueryWatch.Contracts;
using KeelMatrix.QueryWatch.Reporting;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchContractsCompatibilityTests {
        [Fact]
        public void Contract_RoundTrips_With_SourceGen_Context() {
            var s = new Summary {
                Schema = QueryWatchJson.SchemaVersion,
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                StoppedAt = DateTimeOffset.UtcNow,
                TotalQueries = 2,
                TotalDurationMs = 12.34,
                AverageDurationMs = 6.17,
                Events = new[] {
                    new EventSample { At = DateTimeOffset.UtcNow.AddSeconds(-2), DurationMs = 9, Text = "slow" },
                    new EventSample { At = DateTimeOffset.UtcNow.AddSeconds(-1), DurationMs = 3.34, Text = "fast" }
                },
                Meta = new Dictionary<string, string> { ["library"] = "KeelMatrix.QueryWatch" }
            };

            var json = JsonSerializer.Serialize(s, QueryWatchJsonContext.Default.Summary);
            json.Should().Contain("\"schema\"").And.Contain("\"events\"");

            var back = JsonSerializer.Deserialize(json, QueryWatchJsonContext.Default.Summary);
            back.Should().NotBeNull();
            back!.TotalQueries.Should().Be(2);
            back.Events.Should().HaveCount(2);
        }

        [Fact]
        public void Golden_Schema_V1_Deserializes() {
            var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "summary_schema_v1.json"));
            var s = JsonSerializer.Deserialize(json, QueryWatchJsonContext.Default.Summary);
            s.Should().NotBeNull();
            s!.Schema.Should().Be("1.0.0");
            s.Events.Should().NotBeEmpty();
        }

        [Fact]
        public void Library_Summary_Serializes_Equivalent_Shape() {
            using var session = QueryWatcher.Start();
            session.Record("fast", TimeSpan.FromMilliseconds(4));
            session.Record("slow", TimeSpan.FromMilliseconds(12));
            var rep = session.Stop();

            var libSummary = QueryWatchJson.ToSummary(rep, sampleTop: 1);
            // Convert library event meta (Dictionary<string, object?>?) to JsonElement values for the contract model.
            Dictionary<string, JsonElement>? ToJsonMeta(System.Collections.Generic.IReadOnlyDictionary<string, object?>? meta) {
                if (meta is null) return null;
                var dict = new Dictionary<string, JsonElement>(meta.Count);
                foreach (var kv in meta) {
                    dict[kv.Key] = JsonSerializer.SerializeToElement(kv.Value, new JsonSerializerOptions());
                }
                return dict;
            }

            var contract = new Summary {
                Schema = libSummary.Schema,
                StartedAt = libSummary.StartedAt,
                StoppedAt = libSummary.StoppedAt,
                TotalQueries = libSummary.TotalQueries,
                TotalDurationMs = libSummary.TotalDurationMs,
                AverageDurationMs = libSummary.AverageDurationMs,
                Events = libSummary.Events.Select(e => new EventSample { At = e.At, DurationMs = e.DurationMs, Text = e.Text, Meta = ToJsonMeta(e.Meta) }).ToList(),
                Meta = new Dictionary<string, string>(libSummary.Meta)
            };

            var libJson = JsonSerializer.Serialize(libSummary); // library serializer
            var contractJson = JsonSerializer.Serialize(contract, QueryWatchJsonContext.Default.Summary);

            libJson.Should().Contain("\"totalQueries\"").And.Contain("\"events\"");

            contractJson.Should().Contain("\"totalQueries\"").And.Contain("\"events\"");

        }
    }
}
