using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class NoInputsFoundTests {
        [Fact]
        public void Lists_Tried_Paths_And_ExitCode_2() {
            string missing = Path.Combine(Path.GetTempPath(), "qwatch-no-inputs-" + Guid.NewGuid().ToString("N") + ".json");
            (int code, string? stdout, string? stderr) = CliRunner.Run(["--input", missing]);
            _ = code.Should().Be(2, stdout + Environment.NewLine + stderr);
            _ = stderr.Should().Contain("Missing: ").And.Contain(missing);
        }
    }
}
