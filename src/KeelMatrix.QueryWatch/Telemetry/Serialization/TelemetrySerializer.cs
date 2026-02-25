// Copyright (c) KeelMatrix

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using KeelMatrix.QueryWatch.Telemetry.Events;

namespace KeelMatrix.QueryWatch.Telemetry.Serialization {
    /// <summary>
    /// Serializes telemetry events into compact JSON payloads.
    /// </summary>
    internal static class TelemetrySerializer {
        private static readonly JsonSerializerOptions Options = new() {
#if NET8_0_OR_GREATER
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
#else
            PropertyNamingPolicy = SnakeCaseLowerNamingPolicy.Instance,
#endif
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        /// <summary>
        /// Serializes the given telemetry event to a JSON string.
        /// </summary>
        public static string? Serialize(TelemetryEventBase telemetryEvent) {
            if (!TelemetrySchemaValidator.IsValid(telemetryEvent))
                return null;

            var json = JsonSerializer.Serialize(telemetryEvent, telemetryEvent.GetType(), Options);
            if (Encoding.UTF8.GetByteCount(json) > TelemetryConfig.MaxPayloadBytes)
                return null;

            return json;
        }

#if !NET8_0_OR_GREATER
        private sealed class SnakeCaseLowerNamingPolicy : JsonNamingPolicy
        {
            public static readonly SnakeCaseLowerNamingPolicy Instance = new();

            public override string ConvertName(string name)
            {
                if (string.IsNullOrEmpty(name))
                    return name;

                // Fast path: if there are no uppercase letters, return as-is.
                bool hasUpper = false;
                for (int i = 0; i < name.Length; i++)
                {
                    char c = name[i];
                    if (c >= 'A' && c <= 'Z')
                    {
                        hasUpper = true;
                        break;
                    }
                }
                if (!hasUpper)
                    return name;

                // Convert PascalCase/camelCase to snake_case (lower).
                // Handles transitions like "HttpStatusCode" -> "http_status_code"
                // and "ProjectID" -> "project_id".
                var sb = new StringBuilder(name.Length + 8);

                for (int i = 0; i < name.Length; i++)
                {
                    char c = name[i];

                    bool isUpper = c >= 'A' && c <= 'Z';
                    bool isLower = c >= 'a' && c <= 'z';
                    bool isDigit = c >= '0' && c <= '9';

                    if (isUpper)
                    {
                        // Insert '_' on word boundary:
                        // - not at the start
                        // - and when previous is lower/digit
                        // - or when previous is upper but next is lower (e.g., "IDValue" -> "id_value")
                        if (i > 0)
                        {
                            char prev = name[i - 1];
                            bool prevLower = prev >= 'a' && prev <= 'z';
                            bool prevDigit = prev >= '0' && prev <= '9';
                            bool prevUpper = prev >= 'A' && prev <= 'Z';

                            bool nextLower = false;
                            if (i + 1 < name.Length)
                            {
                                char next = name[i + 1];
                                nextLower = next >= 'a' && next <= 'z';
                            }

                            if (prevLower || prevDigit || (prevUpper && nextLower))
                                sb.Append('_');
                        }

                        sb.Append((char)(c + 32)); // to lower ASCII
                        continue;
                    }

                    if (isLower || isDigit)
                    {
                        sb.Append(c);
                        continue;
                    }

                    // Any other character: preserve as-is (shouldn't occur for property names).
                    sb.Append(c);
                }

                return sb.ToString();
            }
        }
#endif
    }
}
