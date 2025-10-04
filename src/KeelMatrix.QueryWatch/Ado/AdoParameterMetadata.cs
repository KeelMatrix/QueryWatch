#nullable enable
using System.Data;
using System.Data.Common;
using System.Text.Json.Serialization;

namespace KeelMatrix.QueryWatch.Ado {
    /// <summary>
    /// Internal value object describing a single ADO parameter shape.
    /// Serialized as part of event metadata when capture policy is enabled.
    /// </summary>
    internal sealed class AdoParameterShape {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("dbType")]
        public string? DbType { get; set; }

        [JsonPropertyName("clrType")]
        public string? ClrType { get; set; }

        [JsonPropertyName("direction")]
        public string Direction { get; set; } = "Input";
    }

    internal static class AdoParameterMetadata {
        /// <summary>
        /// Build a metadata bag from the command's parameters without touching values.
        /// </summary>
        public static IReadOnlyDictionary<string, object?>? TryCapture(DbCommand command) {
            if (command is null) throw new ArgumentNullException(nameof(command));
            if (command.Parameters is null || command.Parameters.Count == 0)
                return null;

            var list = new List<AdoParameterShape>(command.Parameters.Count);
            foreach (DbParameter p in command.Parameters) {
                var shape = new AdoParameterShape {
                    Name = SafeName(p.ParameterName),
                    DbType = SafeDbType(p.DbType),
                    ClrType = MapClrType(p.DbType),
                    Direction = SafeDirection(p.Direction),
                };
                list.Add(shape);
            }

            if (list.Count == 0) return null;

            // Use a stable key name under the event-level meta.
            return new Dictionary<string, object?> {
                { "parameters", list }
            };
        }

        private static string SafeName(string? name)
            => string.IsNullOrWhiteSpace(name) ? "?" : name!;

        private static string SafeDbType(System.Data.DbType type)
            => type.ToString();

        private static string SafeDirection(ParameterDirection direction)
            => direction.ToString();

        private static string? MapClrType(System.Data.DbType dbType) => dbType switch {
            System.Data.DbType.AnsiString => typeof(string).FullName,
            System.Data.DbType.String => typeof(string).FullName,
            System.Data.DbType.AnsiStringFixedLength => typeof(string).FullName,
            System.Data.DbType.StringFixedLength => typeof(string).FullName,
            System.Data.DbType.Binary => typeof(byte[]).FullName,
            System.Data.DbType.Boolean => typeof(bool).FullName,
            System.Data.DbType.Byte => typeof(byte).FullName,
            System.Data.DbType.SByte => typeof(sbyte).FullName,
            System.Data.DbType.Int16 => typeof(short).FullName,
            System.Data.DbType.UInt16 => typeof(ushort).FullName,
            System.Data.DbType.Int32 => typeof(int).FullName,
            System.Data.DbType.UInt32 => typeof(uint).FullName,
            System.Data.DbType.Int64 => typeof(long).FullName,
            System.Data.DbType.UInt64 => typeof(ulong).FullName,
            System.Data.DbType.Currency => typeof(decimal).FullName,
            System.Data.DbType.Decimal => typeof(decimal).FullName,
            System.Data.DbType.Double => typeof(double).FullName,
            System.Data.DbType.Single => typeof(float).FullName,
            System.Data.DbType.Date => typeof(DateTime).FullName,
            System.Data.DbType.DateTime => typeof(DateTime).FullName,
#if NET6_0_OR_GREATER
            System.Data.DbType.DateTime2 => typeof(DateTime).FullName,
#endif
            System.Data.DbType.DateTimeOffset => typeof(DateTimeOffset).FullName,
            System.Data.DbType.Guid => typeof(Guid).FullName,
            System.Data.DbType.Object => typeof(object).FullName,
            System.Data.DbType.Time => typeof(TimeSpan).FullName,
            System.Data.DbType.VarNumeric => typeof(decimal).FullName,
            _ => null
        };
    }
}
