using System.Text.Json;
using FluentAssertions;
using KeelMatrix.QueryWatch.Contracts;
using KeelMatrix.QueryWatch.Reporting;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchContractsCompatibilityTests {
        [Fact]
        public void Contract_RoundTrips_With_SourceGen_Context() {
            Summary s = new() {
                Schema = QueryWatchJson.SchemaVersion,
                StartedAt = DateTimeOffset.UtcNow.AddMinutes(-1),
                StoppedAt = DateTimeOffset.UtcNow,
                TotalQueries = 2,
                TotalDurationMs = 12.34,
                AverageDurationMs = 6.17,
                Events = [
                    new EventSample { At = DateTimeOffset.UtcNow.AddSeconds(-2), DurationMs = 9, Text = "slow" },
                    new EventSample { At = DateTimeOffset.UtcNow.AddSeconds(-1), DurationMs = 3.34, Text = "fast" }
                ],
                Meta = new Dictionary<string, string> { ["library"] = "KeelMatrix.QueryWatch" }
            };

            string json = JsonSerializer.Serialize(s, QueryWatchJsonContext.Default.Summary);
            _ = json.Should().Contain("\"schema\"").And.Contain("\"events\"");

            var back = JsonSerializer.Deserialize(json, QueryWatchJsonContext.Default.Summary);
            _ = back.Should().NotBeNull();
            _ = back!.TotalQueries.Should().Be(2);
            _ = back.Events.Should().HaveCount(2);
        }

        [Fact]
        public void Golden_Schema_V1_Deserializes() {
            string json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "summary_schema_v1.json"));
            var s = JsonSerializer.Deserialize(json, QueryWatchJsonContext.Default.Summary);
            _ = s.Should().NotBeNull();
            _ = s!.Schema.Should().Be("1.0.0");
            _ = s.Events.Should().NotBeEmpty();
        }

        [Fact]
        public void Library_Summary_Serializes_Equivalent_Shape() {
            using var session = QueryWatcher.Start();
            session.Record("fast", TimeSpan.FromMilliseconds(4));
            session.Record("slow", TimeSpan.FromMilliseconds(12));
            var rep = session.Stop();

            QueryWatchJson.Summary libSummary = QueryWatchJson.ToSummary(rep, sampleTop: 1);
            // Convert library event meta (Dictionary<string, object?>?) to JsonElement values for the contract model.
            static Dictionary<string, JsonElement>? ToJsonMeta(System.Collections.Generic.IReadOnlyDictionary<string, object?>? meta) {
                if (meta is null) return null;
                Dictionary<string, JsonElement> dict = new(meta.Count);
                var options = new JsonSerializerOptions();
                foreach (var kv in meta) {
                    dict[kv.Key] = JsonSerializer.SerializeToElement(kv.Value, options);
                }
                return dict;
            }

            Summary contract = new() {
                Schema = libSummary.Schema,
                StartedAt = libSummary.StartedAt,
                StoppedAt = libSummary.StoppedAt,
                TotalQueries = libSummary.TotalQueries,
                TotalDurationMs = libSummary.TotalDurationMs,
                AverageDurationMs = libSummary.AverageDurationMs,
                Events = [.. libSummary.Events.Select(e => new EventSample { At = e.At, DurationMs = e.DurationMs, Text = e.Text, Meta = ToJsonMeta(e.Meta) })],
                Meta = new Dictionary<string, string>(libSummary.Meta)
            };

            string libJson = JsonSerializer.Serialize(libSummary); // library serializer
            string contractJson = JsonSerializer.Serialize(contract, QueryWatchJsonContext.Default.Summary);

            _ = libJson.Should().Contain("\"totalQueries\"").And.Contain("\"events\"");

            _ = contractJson.Should().Contain("\"totalQueries\"").And.Contain("\"events\"");

        }
    }
}
