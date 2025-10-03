#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    /// Small, stable JSON exporter for QueryWatch reports.
    /// </summary>
    public static class QueryWatchJson {
        /// <summary>Current JSON schema version of the exported file.</summary>
        public const string SchemaVersion = "1.0.0";

        /// <summary>
        /// A compact, CI-friendly summary of a <see cref="QueryWatchReport"/>.
        /// Keep this model stable across minor releases. Additive changes must preserve existing fields.
        /// </summary>
        public sealed class Summary {
            [JsonPropertyName("schema")]
            public string Schema { get; init; } = SchemaVersion;

            [JsonPropertyName("startedAt")]
            public DateTimeOffset StartedAt { get; init; }

            [JsonPropertyName("stoppedAt")]
            public DateTimeOffset StoppedAt { get; init; }

            [JsonPropertyName("totalQueries")]
            public int TotalQueries { get; init; }

            [JsonPropertyName("totalDurationMs")]
            public double TotalDurationMs { get; init; }

            [JsonPropertyName("averageDurationMs")]
            public double AverageDurationMs { get; init; }

            [JsonPropertyName("events")]
            public IReadOnlyList<EventSample> Events { get; init; } = Array.Empty<EventSample>();

            /// <summary>Optional product metadata.</summary>
            [JsonPropertyName("meta")]
            public Dictionary<string, string> Meta { get; init; } = new();
        }

        /// <summary>Minimal per-event sample to keep files small yet useful.</summary>
        public sealed class EventSample {
            [JsonPropertyName("at")]
            public DateTimeOffset At { get; init; }

            [JsonPropertyName("durationMs")]
            public double DurationMs { get; init; }

            [JsonPropertyName("text")]
            public string Text { get; init; } = string.Empty;

            /// <summary>
            /// Optional, additive event-level metadata. When ADO parameter capture policy is enabled,
            /// this will contain a <c>parameters</c> array with parameter names and types only.
            /// </summary>
            [JsonPropertyName("meta")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public Dictionary<string, object?>? Meta { get; init; }
        }

        /// <summary>Create a <see cref="Summary"/> object from a report.</summary>
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
                    Meta = e.Meta is null ? null : e.Meta.ToDictionary(kv => kv.Key, kv => kv.Value)
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
        /// Serialize and write a summary JSON file.
        /// </summary>
        /// <param name="report">The report to export.</param>
        /// <param name="path">Target file path (created or overwritten).</param>
        /// <param name="sampleTop">Number of top events (by duration) to include.</param>
        public static void ExportToFile(QueryWatchReport report, string path, int sampleTop = 5) {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Path is required.", nameof(path));
            var summary = ToSummary(report, sampleTop);

            // Record the effective sampling configuration in metadata for downstream tools.
            // This is additive metadata; it does not change the schema.
            summary.Meta["sampleTop"] = sampleTop.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) {
                Directory.CreateDirectory(dir);
            }

            var json = JsonSerializer.Serialize(summary, _jsonOptions);
            File.WriteAllText(path, json, Encoding.UTF8);
        }

        private static readonly JsonSerializerOptions _jsonOptions = new() {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.Never
        };
    }
}
