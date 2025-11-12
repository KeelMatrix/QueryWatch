using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class BaselineWriteTests {
        [Fact]
        public void WriteBaseline_Creates_File_And_Prints_Message() {
            // Arrange
            string f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "current_ok.json");
            string tempRoot = Path.Combine(Path.GetTempPath(), "qwatch-baseline-" + Guid.NewGuid().ToString("N"));
            string baselinePath = Path.Combine(tempRoot, "subdir", "baseline.json");

            try {
                // Act
                (int code, string? stdout, string? stderr) = CliRunner.Run([
                    "--input", f,
                    "--baseline", baselinePath,
                    "--write-baseline"
                ]);

                // Assert
                _ = code.Should().Be(0, stdout + Environment.NewLine + stderr);
                _ = File.Exists(baselinePath).Should().BeTrue("CLI should write baseline file when --write-baseline is provided and no baseline exists");
                _ = stdout.Should().Contain("Baseline written: ").And.Contain(baselinePath);
            }
            finally {
                if (Directory.Exists(tempRoot)) Directory.Delete(tempRoot, recursive: true);
            }
        }
    }
}
