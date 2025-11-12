using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class MultiFileAggregationTests {
        [Fact]
        public void Aggregates_Across_Multiple_Files_And_Respects_MaxQueries() {
            string f1 = Path.Combine(AppContext.BaseDirectory, "Fixtures", "agg_a.json");
            string f2 = Path.Combine(AppContext.BaseDirectory, "Fixtures", "agg_b.json");

            (int exitOk, string? stdoutOk, string? stderrOk) = CliRunner.Run([
                "--input", f1,
                "--input", f2,
                "--max-queries", "5"
            ]);
            _ = exitOk.Should().Be(0, stdoutOk + Environment.NewLine + stderrOk);
            _ = stdoutOk.Should().Contain("files 2");
            _ = stdoutOk.Should().Contain("Queries: 5");

            (int exitFail, string _, string? stderrFail) = CliRunner.Run([
                "--input", f1,
                "--input", f2,
                "--max-queries", "4"
            ]);
            _ = exitFail.Should().Be(4);
            _ = stderrFail.Should().Contain("Budget violations:");
        }
    }
}
