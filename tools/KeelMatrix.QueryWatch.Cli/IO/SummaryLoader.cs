#nullable enable
using System.Text.Json;
using KeelMatrix.QueryWatch.Cli.Model;

namespace KeelMatrix.QueryWatch.Cli.IO {
    internal static class SummaryLoader {
        public static async Task<IReadOnlyList<Summary>> LoadAsync(IEnumerable<string> paths) {
            var found = new List<Summary>();
            var notFound = new List<string>();

            foreach (var path in paths) {
                if (!File.Exists(path)) {
                    notFound.Add(path);
                    continue;
                }

                try {
                    await using var stream = File.OpenRead(path);
                    var s = await JsonSerializer.DeserializeAsync<Summary>(stream).ConfigureAwait(false);
                    if (s is null) throw new InvalidOperationException($"Summary is null: {path}");
                    found.Add(s);
                }
                catch (Exception ex) {
                    throw new JsonException($"Failed to parse JSON '{path}': {ex.Message}", ex);
                }
            }

            if (found.Count == 0) {
                var msg = "No input JSON found." + (notFound.Count > 0 ? " Missing: " + string.Join(", ", notFound) : string.Empty);
                throw new FileNotFoundException(msg);
            }

            return found;
        }
    }
}
