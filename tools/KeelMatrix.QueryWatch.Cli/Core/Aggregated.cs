using KeelMatrix.QueryWatch.Contracts;

namespace KeelMatrix.QueryWatch.Cli.Core {
    public sealed class Aggregated {
        public int Files { get; private set; }
        public int TotalQueries { get; private set; }
        public double TotalDurationMs { get; private set; }
        public double AverageDurationMs { get; private set; }
        public List<EventSample> Events { get; } = new();

        public int SampledEventsCount => Events?.Count ?? 0;

        public static Aggregated From(IEnumerable<Summary> inputs) {
            var list = inputs.ToList();
            var agg = new Aggregated();
            agg.Files = list.Count;
            agg.TotalQueries = list.Sum(s => s.TotalQueries);
            agg.TotalDurationMs = list.Sum(s => s.TotalDurationMs);
            agg.AverageDurationMs = agg.TotalQueries == 0 ? 0 : agg.TotalDurationMs / agg.TotalQueries;
            foreach (var s in list) {
                if (s.Events is not null) agg.Events.AddRange(s.Events);
            }
            return agg;
        }

        // New: allow projecting Aggregated back to a Summary
        public Summary ToSummary(
            DateTimeOffset? startedAt = null,
            DateTimeOffset? stoppedAt = null,
            IReadOnlyDictionary<string, string>? meta = null,
            bool includeEvents = true) {
            return new Summary {
                Schema = "1.0.0",
                StartedAt = startedAt ?? DateTimeOffset.UtcNow,
                StoppedAt = stoppedAt ?? DateTimeOffset.UtcNow,
                TotalQueries = this.TotalQueries,
                TotalDurationMs = this.TotalDurationMs,
                AverageDurationMs = this.AverageDurationMs,
                Events = includeEvents ? this.Events.ToArray() : Array.Empty<EventSample>(),
                Meta = meta is null ? new Dictionary<string, string>() : new Dictionary<string, string>(meta)
            };
        }
    }
}
