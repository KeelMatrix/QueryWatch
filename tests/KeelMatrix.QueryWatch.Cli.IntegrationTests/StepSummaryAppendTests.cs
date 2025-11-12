using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class StepSummaryAppendTests {
        [Fact]
        public void Appends_To_Existing_GitHub_Step_Summary() {
            string f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");
            string temp = Path.Combine(Path.GetTempPath(), "qwatch-step-summary-" + System.Guid.NewGuid().ToString("N") + ".md");
            try {
                System.IO.File.WriteAllText(temp, "PRELUDE\n");
                (int code, string? stdout, string? stderr) = CliRunner.Run(["--input", f, "--budget", "SELECT * FROM Users*=1"], env: [("GITHUB_STEP_SUMMARY", temp)]);
                _ = code.Should().Be(4, stdout + System.Environment.NewLine + stderr);
                string md = System.IO.File.ReadAllText(temp);
                _ = md.Should().Contain("PRELUDE");
                _ = md.Should().Contain("# QueryWatch Gate").And.Contain("## Budgets");
                _ = md.IndexOf("PRELUDE").Should().BeLessThan(md.IndexOf("# QueryWatch Gate"));
            }
            finally {
                if (System.IO.File.Exists(temp)) System.IO.File.Delete(temp);
            }
        }
    }
}
