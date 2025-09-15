#nullable enable
using System.Text.Json.Serialization;

namespace KeelMatrix.QueryWatch.Cli.Model {
    /// <summary>
    /// Compact representation of a QueryWatch report used by the CLI,
    /// matching the JSON written by KeelMatrix.QueryWatch.Reporting.QueryWatchJson.
    /// </summary>
    internal sealed class Summary {
        [JsonPropertyName("schema")]
        public string Schema { get; set; } = "1.0.0";

        [JsonPropertyName("startedAt")]
        public DateTimeOffset StartedAt { get; set; }

        [JsonPropertyName("stoppedAt")]
        public DateTimeOffset StoppedAt { get; set; }

        [JsonPropertyName("totalQueries")]
        public int TotalQueries { get; set; }

        [JsonPropertyName("totalDurationMs")]
        public double TotalDurationMs { get; set; }

        [JsonPropertyName("averageDurationMs")]
        public double AverageDurationMs { get; set; }

        [JsonPropertyName("events")]
        public List<EventSample> Events { get; set; } = new();

        [JsonPropertyName("meta")]
        public Dictionary<string, string> Meta { get; set; } = new();
    }

    internal sealed class EventSample {
        [JsonPropertyName("at")]
        public DateTimeOffset At { get; set; }

        [JsonPropertyName("durationMs")]
        public double DurationMs { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
