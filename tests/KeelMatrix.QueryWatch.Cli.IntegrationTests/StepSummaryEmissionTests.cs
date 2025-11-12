using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class StepSummaryEmissionTests {
        [Fact]
        public void Writes_GitHub_Step_Summary_When_Env_Var_Set() {
            string f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");
            string temp = Path.Combine(Path.GetTempPath(), "qwatch-step-summary-" + Guid.NewGuid().ToString("N") + ".md");

            try {
                (int code, string _, string _) = CliRunner.Run(
                    ["--input", f, "--budget", "SELECT * FROM Users*=1"],
                    env: [("GITHUB_STEP_SUMMARY", temp)]);

                _ = code.Should().Be(4);
                _ = File.Exists(temp).Should().BeTrue("CLI should write PR summary when GITHUB_STEP_SUMMARY is set");
                string md = File.ReadAllText(temp);
                _ = md.Should().Contain("# QueryWatch Gate");
                _ = md.Should().Contain("## Budgets");
            }
            finally {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }
}
