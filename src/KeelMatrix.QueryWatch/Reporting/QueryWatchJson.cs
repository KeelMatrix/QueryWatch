using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

#if NETSTANDARD2_0
namespace System.Runtime.CompilerServices
{
    // This type is required for C# 9.0 init-only setters support on .NET Standard 2.0.
    internal static class IsExternalInit {
        // Prevent instantiation.
        static IsExternalInit() { }
    }
}
#endif

namespace KeelMatrix.QueryWatch.Reporting {
    /// <summary>
    /// JSON export helpers.
    /// </summary>
    public static class QueryWatchJson {
        /// <summary>Current JSON schema version of the exported file.</summary>
        public const string SchemaVersion = "1.0.0";

        /// <summary>
        /// A compact, CI-friendly summary of a <see cref="QueryWatchReport"/>.
        /// </summary>
        public sealed class Summary {
            /// <summary>
            /// Schema version string (e.g., <c>1.0.0</c>).
            /// </summary>
            [JsonPropertyName("schema")]
            public string Schema { get; init; } = SchemaVersion;

            /// <summary>
            /// UTC timestamp when the session started.
            /// </summary>
            [JsonPropertyName("startedAt")]
            public DateTimeOffset StartedAt { get; init; }

            /// <summary>
            /// UTC timestamp when the session stopped.
            /// </summary>
            [JsonPropertyName("stoppedAt")]
            public DateTimeOffset StoppedAt { get; init; }

            /// <summary>
            /// Total number of queries recorded.
            /// </summary>
            [JsonPropertyName("totalQueries")]
            public int TotalQueries { get; init; }

            /// <summary>
            /// Total duration across all queries, in milliseconds.
            /// </summary>
            [JsonPropertyName("totalDurationMs")]
            public double TotalDurationMs { get; init; }

            /// <summary>
            /// Average duration per query, in milliseconds.
            /// </summary>
            [JsonPropertyName("averageDurationMs")]
            public double AverageDurationMs { get; init; }

            /// <summary>
            /// Top sample of events for quick inspection.
            /// </summary>
            [JsonPropertyName("events")]
            public IReadOnlyList<EventSample> Events { get; init; } = [];

            /// <summary>
            /// Arbitrary metadata associated with the report.
            /// </summary>
            [JsonPropertyName("meta")]
            public Dictionary<string, string> Meta { get; init; } = [];
        }

        /// <summary>
        /// A sampled event in the JSON export.
        /// </summary>
        public sealed class EventSample {
            /// <summary>
            /// UTC timestamp when the command finished.
            /// </summary>
            [JsonPropertyName("at")]
            public DateTimeOffset At { get; init; }

            /// <summary>
            /// Execution duration, in milliseconds.
            /// </summary>
            [JsonPropertyName("durationMs")]
            public double DurationMs { get; init; }

            /// <summary>
            /// Redacted / normalized SQL or command text.
            /// </summary>
            [JsonPropertyName("text")]
            public string Text { get; init; } = string.Empty;

            /// <summary>
            /// Optional event metadata (e.g., parameter shapes).
            /// </summary>
            [JsonPropertyName("meta")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public Dictionary<string, object?>? Meta { get; init; }
        }

        /// <summary>
        /// Creates a serializable summary from a report.
        /// </summary>
        /// <param name="report">Report to summarize.</param>
        /// <param name="sampleTop">Number of top events to include in the sample. Default: 5.</param>
        /// <returns>A summary model suitable for JSON serialization.</returns>
        public static Summary ToSummary(QueryWatchReport report, int sampleTop = 5) {
            if (report is null) throw new ArgumentNullException(nameof(report));
            if (sampleTop < 0) sampleTop = 0;

            var samples = report.Events
                .OrderByDescending(e => e.Duration.TotalMilliseconds)
                .Take(sampleTop)
                .Select(e => new EventSample {
                    At = e.At,
                    DurationMs = e.Duration.TotalMilliseconds,
                    Text = e.CommandText ?? string.Empty,
                    // Explicit ToDictionary to avoid the IDictionary<TK,TV> ctor resolution on netstandard2.0
                    Meta = e.Meta?.ToDictionary(kv => kv.Key, kv => kv.Value)
                })
                .ToArray();

            return new Summary {
                StartedAt = report.StartedAt,
                StoppedAt = report.StoppedAt,
                TotalQueries = report.TotalQueries,
                TotalDurationMs = report.TotalDuration.TotalMilliseconds,
                AverageDurationMs = report.AverageDuration.TotalMilliseconds,
                Events = samples,
                Meta = new Dictionary<string, string> {
                        { "library", "KeelMatrix.QueryWatch" },
                        { "sampleTop", sampleTop.ToString(System.Globalization.CultureInfo.InvariantCulture) }
                    }
            };
        }

        /// <summary>
        /// Exports a report summary to a JSON file.
        /// </summary>
        /// <param name="report">Report to export.</param>
        /// <param name="path">Destination file path.</param>
        /// <param name="sampleTop">Number of top events to include in the sample. Default: 5.</param>
        public static void ExportToFile(QueryWatchReport report, string path, int sampleTop = 5) {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
            Summary summary = ToSummary(report, sampleTop);

            // Record the effective sampling configuration in metadata for downstream tools.
            // This is additive metadata; it does not change the schema.
            summary.Meta["sampleTop"] = sampleTop.ToString(System.Globalization.CultureInfo.InvariantCulture);

            string? dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                _ = Directory.CreateDirectory(dir);
            }

            string json = JsonSerializer.Serialize(summary, _jsonOptions);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private static readonly JsonSerializerOptions _jsonOptions = new() {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
    }
}
