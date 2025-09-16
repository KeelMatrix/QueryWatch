using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class StepSummaryEmissionTests {
        [Fact]
        public void Writes_GitHub_Step_Summary_When_Env_Var_Set() {
            var f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");
            var temp = Path.Combine(Path.GetTempPath(), "qwatch-step-summary-" + Guid.NewGuid().ToString("N") + ".md");

            try {
                var (code, stdout, stderr) = CliRunner.Run(
                    new[] { "--input", f, "--budget", "SELECT * FROM Users*=1" },
                    env: new[] { ("GITHUB_STEP_SUMMARY", temp) });

                code.Should().Be(4);
                File.Exists(temp).Should().BeTrue("CLI should write PR summary when GITHUB_STEP_SUMMARY is set");
                var md = File.ReadAllText(temp);
                md.Should().Contain("# QueryWatch Gate");
                md.Should().Contain("## Budgets");
            }
            finally {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }
}
