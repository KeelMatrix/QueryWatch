using System.Text.Json;
using FluentAssertions;
using KeelMatrix.QueryWatch.Reporting;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchJsonEventMetaRoundTripTests {
        [Fact]
        public void Event_Meta_Parameters_RoundTrip_Serialize_Deserialize() {
            using var session = QueryWatcher.Start();

            var meta = new Dictionary<string, object?> {
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

            var summary = QueryWatchJson.ToSummary(session.Stop(), sampleTop: 1);

            var json = JsonSerializer.Serialize(summary);
            var roundtripped = JsonSerializer.Deserialize<QueryWatchJson.Summary>(json);

            roundtripped.Should().NotBeNull();
            var ev = roundtripped!.Events.Should().ContainSingle().Subject;
            ev.Meta.Should().NotBeNull();
            ev.Meta!.Should().ContainKey("parameters");

            var parameters = (JsonElement)ev.Meta["parameters"]!;
            parameters.ValueKind.Should().Be(JsonValueKind.Array);
            parameters.GetArrayLength().Should().Be(2);

            var first = parameters.EnumerateArray().First();
            first.GetProperty("name").GetString().Should().Be("@userId");
            first.GetProperty("dbType").GetString().Should().Be("Int32");
            first.GetProperty("clrType").GetString().Should().Be("System.Int32");
            first.GetProperty("direction").GetString().Should().Be("Input");
        }
    }
}
