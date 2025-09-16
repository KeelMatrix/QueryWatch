using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class BaselineToleranceTests {
        [Fact]
        public void Within_Tolerance_ExitCode_Ok() {
            var current = Path.Combine(AppContext.BaseDirectory, "Fixtures", "current_ok.json");
            var baseline = Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline.json");

            var (code, stdout, stderr) = CliRunner.Run(new[] {
                "--input", current,
                "--baseline", baseline,
                "--baseline-allow-percent", "10"
            });

            code.Should().Be(0, stdout + Environment.NewLine + stderr);
        }

        [Fact]
        public void Beyond_Tolerance_ExitCode_BaselineRegression() {
            var current = Path.Combine(AppContext.BaseDirectory, "Fixtures", "current_bad.json");
            var baseline = Path.Combine(AppContext.BaseDirectory, "Fixtures", "baseline.json");

            var (code, stdout, stderr) = CliRunner.Run(new[] {
                "--input", current,
                "--baseline", baseline,
                "--baseline-allow-percent", "10"
            });

            code.Should().Be(5);
            stderr.Should().Contain("Baseline regressions:");
        }
    }
}
