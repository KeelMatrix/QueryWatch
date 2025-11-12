using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class BaselineToleranceTests {
        [Fact]
        public void Within_Tolerance_ExitCode_Ok() {
            string current = Path.Combine(AppContext.BaseDirectory, "Fixtures", "current_ok.json");
            string baseline = Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline.json");

            (int code, string? stdout, string? stderr) = CliRunner.Run([
                "--input", current,
                "--baseline", baseline,
                "--baseline-allow-percent", "10"
            ]);

            _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
        }

        [Fact]
        public void Beyond_Tolerance_ExitCode_BaselineRegression() {
            string current = Path.Combine(AppContext.BaseDirectory, "Fixtures", "current_bad.json");
            string baseline = Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline.json");

            (int code, string _, string? stderr) = CliRunner.Run([
                "--input", current,
                "--baseline", baseline,
                "--baseline-allow-percent", "10"
            ]);

            _ = code.Should().Be(5);
            _ = stderr.Should().Contain("Baseline regressions:");
        }
    }
}
