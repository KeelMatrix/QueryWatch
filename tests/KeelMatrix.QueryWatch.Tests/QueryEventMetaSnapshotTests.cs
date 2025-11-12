using KeelMatrix.QueryWatch.Reporting;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryEventMetaSnapshotTests {
        [Fact]
        public void Json_Summary_Includes_Event_Parameters_Meta_Snapshot() {
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

            // Snapshot only the stable shape we care about (event-level meta)
            var projection = new {
                events = summary.Events.Select(e => e.Meta).ToArray()
            };

            // Stores under __snapshots__/<this-file>.event-params-meta.snap.json (see SnapshotHelper)
            projection.ShouldMatchSnapshot("event-params-meta");
        }
    }
}
