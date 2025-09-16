using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class MultiFileAggregationTests {
        [Fact]
        public void Aggregates_Across_Multiple_Files_And_Respects_MaxQueries() {
            var f1 = Path.Combine(AppContext.BaseDirectory, "Fixtures", "agg_a.json");
            var f2 = Path.Combine(AppContext.BaseDirectory, "Fixtures", "agg_b.json");

            var (exitOk, stdoutOk, stderrOk) = CliRunner.Run(new[] {
                "--input", f1,
                "--input", f2,
                "--max-queries", "5"
            });
            exitOk.Should().Be(0, stdoutOk + Environment.NewLine + stderrOk);
            stdoutOk.Should().Contain("files 2");
            stdoutOk.Should().Contain("Queries: 5");

            var (exitFail, stdoutFail, stderrFail) = CliRunner.Run(new[] {
                "--input", f1,
                "--input", f2,
                "--max-queries", "4"
            });
            exitFail.Should().Be(4);
            stderrFail.Should().Contain("Budget violations:");
        }
    }
}
