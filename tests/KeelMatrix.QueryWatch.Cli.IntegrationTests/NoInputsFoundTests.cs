#nullable enable
using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class NoInputsFoundTests {
        [Fact]
        public void Lists_Tried_Paths_And_ExitCode_2() {
            var missing = Path.Combine(Path.GetTempPath(), "qwatch-no-inputs-" + System.Guid.NewGuid().ToString("N") + ".json");
            var (code, stdout, stderr) = CliRunner.Run(new[] { "--input", missing });
            code.Should().Be(2, stdout + System.Environment.NewLine + stderr);
            stderr.Should().Contain("Missing: ").And.Contain(missing);
        }
    }
}
