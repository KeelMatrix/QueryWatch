using KeelMatrix.QueryWatch.Contracts;

namespace KeelMatrix.QueryWatch.Cli.Core {
    public sealed class Aggregated {
        public int Files { get; private set; }
        public int TotalQueries { get; private set; }
        public double TotalDurationMs { get; private set; }
        public double AverageDurationMs { get; private set; }
        public List<EventSample> Events { get; } = [];

        public int SampledEventsCount => Events?.Count ?? 0;

        public static Aggregated From(IEnumerable<Summary> inputs) {
            List<Summary> list = [.. inputs];
            Aggregated agg = new() {
                Files = list.Count,
                TotalQueries = list.Sum(s => s.TotalQueries),
                TotalDurationMs = list.Sum(s => s.TotalDurationMs)
            };
            agg.AverageDurationMs = agg.TotalQueries == 0 ? 0 : agg.TotalDurationMs / agg.TotalQueries;
            foreach (Summary? s in list.Where(s => s.Events is not null))
                agg.Events.AddRange(s.Events);

            return agg;
        }

        public Summary ToSummary(
            DateTimeOffset? startedAt = null,
            DateTimeOffset? stoppedAt = null,
            IReadOnlyDictionary<string, string>? meta = null,
            bool includeEvents = true) {
            return new Summary {
                Schema = "1.0.0",
                StartedAt = startedAt ?? DateTimeOffset.UtcNow,
                StoppedAt = stoppedAt ?? DateTimeOffset.UtcNow,
                TotalQueries = TotalQueries,
                TotalDurationMs = TotalDurationMs,
                AverageDurationMs = AverageDurationMs,
                Events = includeEvents ? [.. Events] : Array.Empty<EventSample>(),
                Meta = meta is null ? [] : new Dictionary<string, string>(meta)
            };
        }
    }
}
