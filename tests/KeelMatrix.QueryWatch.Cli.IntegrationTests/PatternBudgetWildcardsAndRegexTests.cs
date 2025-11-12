using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class PatternBudgetWildcardsAndRegexTests {
        [Fact]
        public void Wildcard_QuestionMark_Matches_Single_Character_And_Ignores_Case() {
            string f = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            // Lowercase pattern, '?' for one char in "Users", '*' for the rest of the line
            (int ok, string? stdoutOk, string? stderrOk) = CliRunner.Run([
                "--input", f,
                "--budget", "select * from user?* = 2".Replace(" ", "") // avoid quoting/space headaches
            ]);
            _ = ok.Should().Be(0, stdoutOk + Environment.NewLine + stderrOk);

            // Overly strict budget should fail (2 matches > 1 allowed)
            (int fail, string? stdoutFail, string? stderrFail) = CliRunner.Run([
                "--input", f,
                "--budget", "SELECT*FROM*User?*=1"
            ]);
            _ = fail.Should().Be(4, stdoutFail + Environment.NewLine + stderrFail);
        }

        [Fact]
        public void Regex_Budget_Is_Case_Insensitive_And_Anchored_From_Start() {
            string f = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            // Should match both SELECTs in pattern.json
            (int ok, string? so1, string? se1) = CliRunner.Run([
                "--input", f,
                "--budget", @"regex:^select\s+\*\s+from\s+users\b.*=2"
            ]);
            _ = ok.Should().Be(0, so1 + Environment.NewLine + se1);

            // Only allow 1 -> should fail because there are 2 matches
            (int bad, string? so2, string? se2) = CliRunner.Run([
                "--input", f,
                "--budget", @"regex:^SELECT\s+\*\s+FROM\s+Users\b.*=1"
            ]);
            _ = bad.Should().Be(4, so2 + Environment.NewLine + se2);
        }

        [Fact]
        public void Invalid_Regex_Budget_Returns_InvalidArguments() {
            string f = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            const string spec = "regex:(unclosed=2";
            (int code, string? stdout, string? stderr) = CliRunner.Run(["--input", f, "--budget", spec]);
            _ = code.Should().Be(1, stdout + Environment.NewLine + stderr); // ExitCodes.InvalidArguments
            _ = stderr.Should().Contain($"Invalid --budget value '{spec}'").And.Contain("Invalid regex");
        }

        [Fact]
        public void Zero_Allowed_Count_Fails_When_There_Are_Matches() {
            string f = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            (int code, string? stdout, string? stderr) = CliRunner.Run([
                "--input", f,
                "--budget", "SELECT * FROM Users*=0"
            ]);
            _ = code.Should().Be(4, stdout + Environment.NewLine + stderr); // BudgetExceeded
        }

        [Fact]
        public void Multiple_Budgets_One_Over_Still_Triggers_Failure() {
            string f = System.IO.Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            (int code, string? stdout, string? stderr) = CliRunner.Run([
                "--input", f,
                "--budget", "SELECT * FROM Users*=1",   // over (2 > 1)
                "--budget", "INSERT INTO Users*=2"      // under (1 <= 2)
            ]);
            _ = code.Should().Be(4, stdout + Environment.NewLine + stderr);
        }
    }
}
