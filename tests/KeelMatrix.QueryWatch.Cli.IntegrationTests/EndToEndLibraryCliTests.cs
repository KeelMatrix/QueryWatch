// Copyright (c) KeelMatrix
using FluentAssertions;
using KeelMatrix.QueryWatch.Reporting;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    /// <summary>
    /// End-to-end tests that generate a QueryWatch JSON with the library
    /// and then exercise the CLI against it. This mirrors a real user workflow.
    /// </summary>
    public class EndToEndLibraryCliTests {
        private static string CreateLibraryJson(int totalEvents, int sampleTop, out int predictableMatches) {
            predictableMatches = 0;
            using QueryWatchSession session = new();

            // One predictable event text so pattern budgets can match.
            session.Record("SELECT * FROM Users WHERE Name LIKE 'A%'", TimeSpan.FromMilliseconds(2));
            predictableMatches++;

            // A few more generic events.
            for (int i = 1; i < totalEvents; i++) {
                session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));
            }

            QueryWatchReport report = session.Stop();

            var path = Path.Combine(Path.GetTempPath(), "qwatch-e2e-" + Guid.NewGuid().ToString("N") + ".json");
            QueryWatchJson.ExportToFile(report, path, sampleTop: sampleTop);
            return path;
        }

        [Fact]
        public void LibraryJson_MaxQueries_Pass_Then_Fail() {
            var json = CreateLibraryJson(totalEvents: 2, sampleTop: 10, out _);

            (int okCode, string? okOut, string? okErr) = CliRunner.Run([
                "--input", json,
                "--max-queries", "3"
            ]);
            _ = okCode.Should().Be(0, okOut + Environment.NewLine + okErr);

            (int failCode, string _, string? failErr) = CliRunner.Run([
                "--input", json,
                "--max-queries", "1"
            ]);
            _ = failCode.Should().Be(4);
            _ = failErr.Should().Contain("Max queries exceeded");
        }

        [Fact]
        public void LibraryJson_PatternBudget_Pass_Then_Fail() {
            var json = CreateLibraryJson(totalEvents: 3, sampleTop: 10, out _);

            // Allow exactly the predictable match
            (int okCode, string? okOut, string? okErr) = CliRunner.Run([
                "--input", json,
                "--budget", "SELECT * FROM Users*=1"
            ]);
            _ = okCode.Should().Be(0, okOut + Environment.NewLine + okErr);

            // Now disallow it
            (int badCode, string _, string? badErr) = CliRunner.Run([
                "--input", json,
                "--budget", "SELECT * FROM Users*=0"
            ]);
            _ = badCode.Should().Be(4);
            _ = badErr.Should().Contain("Budget violations");
        }

        [Fact]
        public void Baseline_Write_Then_Compare_With_Tolerance() {
            var current1 = CreateLibraryJson(totalEvents: 3, sampleTop: 10, out _);
            var baselinePath = Path.Combine(Path.GetTempPath(), "qwatch-baseline-" + Guid.NewGuid().ToString("N"), "baseline.json");

            (int writeCode, string? writeOut, string? writeErr) = CliRunner.Run([
                "--input", current1,
                "--baseline", baselinePath,
                "--write-baseline"
            ]);
            _ = writeCode.Should().Be(0, writeOut + Environment.NewLine + writeErr);
            _ = File.Exists(baselinePath).Should().BeTrue();

            var current2 = CreateLibraryJson(totalEvents: 5, sampleTop: 10, out _);

            // Generous tolerance -> pass
            (int passCode, string? passOut, string? passErr) = CliRunner.Run([
                "--input", current2,
                "--baseline", baselinePath,
                "--baseline-allow-percent", "80"
            ]);
            _ = passCode.Should().Be(0, passOut + Environment.NewLine + passErr);

            // Tight tolerance -> fail with baseline regression code
            (int failCode, string _, string? failErr) = CliRunner.Run([
                "--input", current2,
                "--baseline", baselinePath,
                "--baseline-allow-percent", "10"
            ]);
            _ = failCode.Should().Be(5);
            _ = failErr.Should().Contain("Baseline regressions");
        }

        [Fact]
        public void RequireFullEvents_Fails_When_Sampled() {
            // Emit meta.sampleTop by sampling below total events.
            var json = CreateLibraryJson(totalEvents: 5, sampleTop: 2, out _);

            (int code, string _, string? err) = CliRunner.Run(["--input", json, "--require-full-events"]);
            _ = code.Should().Be(1);
            _ = err.Should().Contain("sampled");
        }
    }
}
