using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class PatternBudgetTests {
        [Fact]
        public void Exceeds_Pattern_Budget_Returns_BudgetExceeded() {
            var f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");
            var (code, stdout, stderr) = CliRunner.Run(new[]
            {
                "--input", f,
                "--budget", "SELECT * FROM Users*=1"
            });

            // For pattern budgets the CLI returns 4 when the count exceeds the budget.
            // It may not emit the "Budget violations:" banner to stderr (used for hard budgets),
            // so we assert exit code is 4 and that some output was produced.
            code.Should().Be(4, "pattern budget must fail the build when exceeded");
            (stdout.Length + stderr.Length).Should().BeGreaterThan(0, "CLI should emit a summary to stdout or diagnostics to stderr");
        }

        [Fact]
        public void Meets_Pattern_Budget_Returns_Ok() {
            var f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");
            var (code, stdout, stderr) = CliRunner.Run(new[]
            {
                "--input", f,
                "--budget", "SELECT * FROM Users*=2"
            });

            code.Should().Be(0, stdout + Environment.NewLine + stderr);
        }
    }
}
