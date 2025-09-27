#nullable enable
using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class BaselineWriteTests {
        [Fact]
        public void WriteBaseline_Creates_File_And_Prints_Message() {
            // Arrange
            var f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "current_ok.json");
            var tempRoot = Path.Combine(Path.GetTempPath(), "qwatch-baseline-" + Guid.NewGuid().ToString("N"));
            var baselinePath = Path.Combine(tempRoot, "subdir", "baseline.json");

            try {
                // Act
                var (code, stdout, stderr) = CliRunner.Run(new[] {
                    "--input", f,
                    "--baseline", baselinePath,
                    "--write-baseline"
                });

                // Assert
                code.Should().Be(0, stdout + Environment.NewLine + stderr);
                File.Exists(baselinePath).Should().BeTrue("CLI should write baseline file when --write-baseline is provided and no baseline exists");
                stdout.Should().Contain("Baseline written: ").And.Contain(baselinePath);
            }
            finally {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
