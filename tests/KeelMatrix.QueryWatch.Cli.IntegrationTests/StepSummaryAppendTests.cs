using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class StepSummaryAppendTests {
        [Fact]
        public void Appends_To_Existing_GitHub_Step_Summary() {
            var f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");
            var temp = Path.Combine(Path.GetTempPath(), "qwatch-step-summary-" + System.Guid.NewGuid().ToString("N") + ".md");
            try {
                System.IO.File.WriteAllText(temp, "PRELUDE\n");
                var (code, stdout, stderr) = CliRunner.Run(new[] { "--input", f, "--budget", "SELECT * FROM Users*=1" }, env: new[] { ("GITHUB_STEP_SUMMARY", temp) });
                code.Should().Be(4, stdout + System.Environment.NewLine + stderr);
                var md = System.IO.File.ReadAllText(temp);
                md.Should().Contain("PRELUDE");
                md.Should().Contain("# QueryWatch Gate").And.Contain("## Budgets");
                md.IndexOf("PRELUDE").Should().BeLessThan(md.IndexOf("# QueryWatch Gate"));
            }
            finally {
                if (System.IO.File.Exists(temp)) System.IO.File.Delete(temp);
            }
        }
    }
}
