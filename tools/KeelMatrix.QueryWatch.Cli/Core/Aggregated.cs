#nullable enable
using KeelMatrix.QueryWatch.Cli.Model;

namespace KeelMatrix.QueryWatch.Cli.Core {
    internal sealed class Aggregated {
        public string Schema { get; private set; } = "1.0.0";
        public DateTimeOffset StartedAt { get; private set; }
        public DateTimeOffset StoppedAt { get; private set; }
        public int TotalQueries { get; private set; }
        public double TotalDurationMs { get; private set; }
        public double AverageDurationMs { get; private set; }
        public int SampledEventsCount => Events.Count;
        public int FileCount { get; private set; }
        public List<EventSample> Events { get; } = new();

        public static Aggregated From(IEnumerable<Summary> summaries) {
            var agg = new Aggregated();
            agg.FileCount = 0;
            var haveTimes = false;
            var totalDurMs = 0.0;
            var totalQueries = 0;

            foreach (var s in summaries) {
                agg.FileCount++;
                agg.Schema = s.Schema;
                if (!haveTimes) { agg.StartedAt = s.StartedAt; agg.StoppedAt = s.StoppedAt; haveTimes = true; }
                else {
                    if (s.StartedAt < agg.StartedAt) agg.StartedAt = s.StartedAt;
                    if (s.StoppedAt > agg.StoppedAt) agg.StoppedAt = s.StoppedAt;
                }

                totalQueries += s.TotalQueries;
                totalDurMs += s.TotalDurationMs;
                if (s.Events is not null && s.Events.Count > 0) {
                    agg.Events.AddRange(s.Events);
                }
            }

            agg.TotalQueries = totalQueries;
            agg.TotalDurationMs = totalDurMs;
            agg.AverageDurationMs = totalQueries == 0 ? 0.0 : (totalDurMs / totalQueries);
            agg.Events.Sort((a, b) => b.DurationMs.CompareTo(a.DurationMs));
            return agg;
        }

        public Summary ToSummary() => new Summary {
            Schema = Schema,
            StartedAt = StartedAt,
            StoppedAt = StoppedAt,
            TotalQueries = TotalQueries,
            TotalDurationMs = TotalDurationMs,
            AverageDurationMs = AverageDurationMs,
            Events = Events.ToList(),
            Meta = new Dictionary<string, string> { { "aggregatedFromFiles", FileCount.ToString() } }
        };
    }
}
