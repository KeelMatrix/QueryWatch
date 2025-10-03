#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using KeelMatrix.QueryWatch.Contracts;

namespace KeelMatrix.QueryWatch.Cli.IO {
    /// <summary>
    /// Loads QueryWatch summary JSON files with friendly error handling so the CLI
    /// can map failures to a deterministic exit code instead of crashing the process.
    /// </summary>
    internal static class SummaryLoader {
        public static async Task<IReadOnlyList<Summary>> LoadAsync(IEnumerable<string> paths) {
            ArgumentNullException.ThrowIfNull(paths);
            var list = new List<Summary>();
            foreach (var p in paths) {
                try {
                    await using var fs = File.OpenRead(p);
                    // Use the shared source-generated context from the contracts package
                    var s = await JsonSerializer.DeserializeAsync(fs, QueryWatchJsonContext.Default.Summary).ConfigureAwait(false);
                    if (s is null) throw new JsonException("File did not contain a valid summary payload.");
                    list.Add(s);
                }
                catch (FileNotFoundException ex) {
                    throw new InputFileNotFoundException($"No input JSON found. Missing: {p}", ex);
                }
                catch (DirectoryNotFoundException ex) {
                    throw new InputFileNotFoundException($"No input JSON found. Missing: {p}", ex);
                }
                catch (JsonException ex) {
                    // Make message stable for tests and CI logs.
                    throw new JsonParseException($"Failed to parse JSON '{p}': {ex.Message}", ex);
                }
                catch (IOException ex) {
                    throw new JsonParseException($"Failed to read JSON '{p}': {ex.Message}", ex);
                }
            }
            return list;
        }
    }

    /// <summary>Thrown when an input file path does not exist.</summary>
    internal sealed class InputFileNotFoundException : Exception {
        public InputFileNotFoundException(string message, Exception? inner) : base(message, inner) { }
    }

    /// <summary>Thrown when JSON parsing fails for an input file.</summary>
    internal sealed class JsonParseException : Exception {
        public JsonParseException(string message, Exception? inner) : base(message, inner) { }
    }
}
