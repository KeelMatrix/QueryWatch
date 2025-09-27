using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class ArgumentUxTests {
        [Fact]
        public void Missing_Budget_Value_Shows_Friendly_Error() {
            var f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");
            var (code, stdout, stderr) = CliRunner.Run(new[] { "--input", f, "--budget" });
            code.Should().Be(1, stdout + System.Environment.NewLine + stderr);
            stderr.Should().Contain("Missing value for --budget");
        }

        [Fact]
        public void WriteBaseline_Requires_Baseline_Path() {
            var f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "current_ok.json");
            var (code, stdout, stderr) = CliRunner.Run(new[] { "--input", f, "--write-baseline" });
            code.Should().Be(1, stdout + System.Environment.NewLine + stderr);
            stderr.Should().Contain("Cannot use --write-baseline without --baseline");
        }
    }
}
