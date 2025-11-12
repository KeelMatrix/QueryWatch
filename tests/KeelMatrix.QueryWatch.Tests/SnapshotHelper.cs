using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentAssertions;

namespace KeelMatrix.QueryWatch.Tests {
    /// <summary>
    /// Minimal snapshot testing helper.
    /// Writes JSON snapshots to a '__snapshots__' folder next to the test file.
    /// </summary>
    public static class SnapshotExtensions {
        private static readonly JsonSerializerOptions _jsonOptions = new() {
            WriteIndented = true
        };

        public static void ShouldMatchSnapshot(this object value,
            string? snapshotName = null, [CallerFilePath] string? callerFile = null) {
            string dir = Path.Combine(Path.GetDirectoryName(callerFile)!, "__snapshots__");
            _ = Directory.CreateDirectory(dir);

            snapshotName ??= $"snapshot_{Guid.NewGuid():N}.json";
            string snapshotPath = Path.Combine(dir, snapshotName);
            string json = JsonSerializer.Serialize(value, _jsonOptions);

            if (!File.Exists(snapshotPath)) {
                File.WriteAllText(snapshotPath, json);
                // First run initializes the snapshot
                return;
            }

            string expected = File.ReadAllText(snapshotPath);
            _ = json.Should().Be(expected, "snapshot should match stored expectations");
        }
    }
}
