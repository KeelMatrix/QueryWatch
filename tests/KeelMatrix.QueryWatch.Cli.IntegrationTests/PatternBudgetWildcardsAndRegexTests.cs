using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class PatternBudgetWildcardsAndRegexTests {
        [Fact]
        public void Wildcard_QuestionMark_Matches_Single_Character_And_Ignores_Case() {
            var f = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            // Lowercase pattern, '?' for one char in "Users", '*' for the rest of the line
            var (ok, stdoutOk, stderrOk) = CliRunner.Run(new[] {
                "--input", f,
                "--budget", "select * from user?* = 2".Replace(" ", "") // avoid quoting/space headaches
            });
            ok.Should().Be(0, stdoutOk + Environment.NewLine + stderrOk);

            // Overly strict budget should fail (2 matches > 1 allowed)
            var (fail, stdoutFail, stderrFail) = CliRunner.Run(new[] {
                "--input", f,
                "--budget", "SELECT*FROM*User?*=1"
            });
            fail.Should().Be(4, stdoutFail + Environment.NewLine + stderrFail);
        }

        [Fact]
        public void Regex_Budget_Is_Case_Insensitive_And_Anchored_From_Start() {
            var f = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            // Should match both SELECTs in pattern.json
            var (ok, so1, se1) = CliRunner.Run(new[] {
                "--input", f,
                "--budget", @"regex:^select\s+\*\s+from\s+users\b.*=2"
            });
            ok.Should().Be(0, so1 + Environment.NewLine + se1);

            // Only allow 1 -> should fail because there are 2 matches
            var (bad, so2, se2) = CliRunner.Run(new[] {
                "--input", f,
                "--budget", @"regex:^SELECT\s+\*\s+FROM\s+Users\b.*=1"
            });
            bad.Should().Be(4, so2 + Environment.NewLine + se2);
        }

        [Fact]
        public void Invalid_Regex_Budget_Returns_InvalidArguments() {
            var f = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            var spec = "regex:(unclosed=2";
            var (code, stdout, stderr) = CliRunner.Run(new[] { "--input", f, "--budget", spec });
            code.Should().Be(1, stdout + Environment.NewLine + stderr); // ExitCodes.InvalidArguments
            stderr.Should().Contain($"Invalid --budget value '{spec}'").And.Contain("Invalid regex");
        }

        [Fact]
        public void Zero_Allowed_Count_Fails_When_There_Are_Matches() {
            var f = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            var (code, stdout, stderr) = CliRunner.Run(new[] {
                "--input", f,
                "--budget", "SELECT * FROM Users*=0"
            });
            code.Should().Be(4, stdout + Environment.NewLine + stderr); // BudgetExceeded
        }

        [Fact]
        public void Multiple_Budgets_One_Over_Still_Triggers_Failure() {
            var f = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            var (code, stdout, stderr) = CliRunner.Run(new[] {
                "--input", f,
                "--budget", "SELECT * FROM Users*=1",   // over (2 > 1)
                "--budget", "INSERT INTO Users*=2"      // under (1 <= 2)
            });
            code.Should().Be(4, stdout + Environment.NewLine + stderr);
        }
    }
}
