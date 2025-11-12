using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class RequireFullEventsSwitchTests {
        [Fact]
        public void Fails_When_Sampled_And_Flag_Set() {
            string f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json"); // has meta.sampleTop
            (int code, string? stdout, string? stderr) = CliRunner.Run(["--input", f, "--require-full-events"]);
            _ = code.Should().Be(1, stdout + System.Environment.NewLine + stderr);
            _ = stderr.Should().Contain("sampleTop");
        }
    }
}
