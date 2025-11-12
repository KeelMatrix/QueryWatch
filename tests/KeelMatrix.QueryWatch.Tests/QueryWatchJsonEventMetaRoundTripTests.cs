using System.Text.Json;
using FluentAssertions;
using KeelMatrix.QueryWatch.Reporting;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchJsonEventMetaRoundTripTests {
        [Fact]
        public void Event_Meta_Parameters_RoundTrip_Serialize_Deserialize() {
            using QueryWatchSession session = QueryWatcher.Start();

            Dictionary<string, object?> meta = new() {
                ["parameters"] = new object?[] {
                    new Dictionary<string, object?> {
                        ["name"] = "@userId",
                        ["dbType"] = "Int32",
                        ["clrType"] = "System.Int32",
                        ["direction"] = "Input"
                    },
                    new Dictionary<string, object?> {
                        ["name"] = "@tenant",
                        ["dbType"] = "String",
                        ["clrType"] = "System.String",
                        ["direction"] = "Input"
                    }
                }
            };

            session.Record(
                "SELECT * FROM Users WHERE Id = @userId AND Tenant = @tenant",
                TimeSpan.FromMilliseconds(3),
                meta
            );

            QueryWatchJson.Summary summary = QueryWatchJson.ToSummary(session.Stop(), sampleTop: 1);

            var json = JsonSerializer.Serialize(summary);
            QueryWatchJson.Summary? roundtripped = JsonSerializer.Deserialize<QueryWatchJson.Summary>(json);

            _ = roundtripped.Should().NotBeNull();
            QueryWatchJson.EventSample ev = roundtripped!.Events.Should().ContainSingle().Subject;
            _ = ev.Meta.Should().NotBeNull();
            _ = ev.Meta!.Should().ContainKey("parameters");

            JsonElement parameters = (JsonElement)ev.Meta!["parameters"]!;
            _ = parameters.ValueKind.Should().Be(JsonValueKind.Array);
            _ = parameters.GetArrayLength().Should().Be(2);

            JsonElement first = parameters.EnumerateArray().First();
            _ = first.GetProperty("name").GetString().Should().Be("@userId");
            _ = first.GetProperty("dbType").GetString().Should().Be("Int32");
            _ = first.GetProperty("clrType").GetString().Should().Be("System.Int32");
            _ = first.GetProperty("direction").GetString().Should().Be("Input");
        }
    }
}
