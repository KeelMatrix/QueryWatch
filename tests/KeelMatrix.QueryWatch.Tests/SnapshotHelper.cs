using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using FluentAssertions;

namespace KeelMatrix.QueryWatch.Tests
{
    /// <summary>
    /// Minimal snapshot testing helper. TODO: Remove if not needed.
    /// Writes JSON snapshots to a '__snapshots__' folder next to the test file.
    /// </summary>
    public static class SnapshotExtensions
    {
        public static void ShouldMatchSnapshot(
            this object value,
            string? snapshotName = null,
            [CallerFilePath] string? callerFile = null)
        {
            if (callerFile is null) throw new ArgumentNullException(nameof(callerFile));

            var dir = Path.Combine(Path.GetDirectoryName(callerFile)!, "__snapshots__");
            Directory.CreateDirectory(dir);

            snapshotName ??= $"snapshot_{Guid.NewGuid():N}.json";
            var snapshotPath = Path.Combine(dir, snapshotName);

            var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });

            if (!File.Exists(snapshotPath))
            {
                File.WriteAllText(snapshotPath, json);
                // First run initializes the snapshot
                return;
            }

            var expected = File.ReadAllText(snapshotPath);
            json.Should().Be(expected, "snapshot should match stored expectations");
        }
    }
}
