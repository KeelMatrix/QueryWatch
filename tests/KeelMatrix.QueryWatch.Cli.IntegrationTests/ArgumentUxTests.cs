using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class ArgumentUxTests {
        [Fact]
        public void Missing_Budget_Value_Shows_Friendly_Error() {
            string f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");
            (int code, string? stdout, string? stderr) = CliRunner.Run(["--input", f, "--budget"]);
            _ = code.Should().Be(1, stdout + System.Environment.NewLine + stderr);
            _ = stderr.Should().Contain("Missing value for --budget");
        }

        [Fact]
        public void WriteBaseline_Requires_Baseline_Path() {
            string f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "current_ok.json");
            (int code, string? stdout, string? stderr) = CliRunner.Run(["--input", f, "--write-baseline"]);
            _ = code.Should().Be(1, stdout + System.Environment.NewLine + stderr);
            _ = stderr.Should().Contain("Cannot use --write-baseline without --baseline");
        }
    }
}
