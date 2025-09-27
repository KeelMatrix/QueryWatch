using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class RequireFullEventsSwitchTests {
        [Fact]
        public void Fails_When_Sampled_And_Flag_Set() {
            var f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json"); // has meta.sampleTop
            var (code, stdout, stderr) = CliRunner.Run(new[] { "--input", f, "--require-full-events" });
            code.Should().Be(1, stdout + System.Environment.NewLine + stderr);
            stderr.Should().Contain("sampleTop");
        }
    }
}
