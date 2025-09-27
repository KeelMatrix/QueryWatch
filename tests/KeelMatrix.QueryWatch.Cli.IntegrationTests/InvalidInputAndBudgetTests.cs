#nullable enable
using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class InvalidInputAndBudgetTests {
        [Fact]
        public void Missing_Input_File_Returns_InputFileNotFound() {
            // Arrange: point to a clearly non-existent file
            var missing = Path.Combine(Path.GetTempPath(), "qwatch-missing", Guid.NewGuid().ToString("N") + ".json");

            // Act
            var (code, stdout, stderr) = CliRunner.Run(new[] { "--input", missing });

            // Assert
            code.Should().Be(2, stdout + Environment.NewLine + stderr); // ExitCodes.InputFileNotFound
            stderr.Should().Contain("No input JSON found.");
        }

        [Fact]
        public void Invalid_Json_Returns_JsonParseError() {
            // Arrange: create an invalid JSON file
            var tempDir = Path.Combine(Path.GetTempPath(), "qwatch-invalid-json-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempDir);
            var invalidPath = Path.Combine(tempDir, "invalid.json");
            File.WriteAllText(invalidPath, "{ not: valid json ]");

            try {
                // Act
                var (code, stdout, stderr) = CliRunner.Run(new[] { "--input", invalidPath });

                // Assert
                code.Should().Be(3, stdout + Environment.NewLine + stderr); // ExitCodes.JsonParseError
                stderr.Should().Contain("Failed to parse JSON");
            }
            finally {
                if (File.Exists(invalidPath)) File.Delete(invalidPath);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void Missing_Budget_Value_Shows_Parse_Error() {
            var f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            var (code, stdout, stderr) = CliRunner.Run(new[] {
                "--input", f,
                "--budget" // missing value
            });

            code.Should().Be(1, stdout + Environment.NewLine + stderr); // ExitCodes.InvalidArguments
            stderr.Should().Contain("Missing value for --budget");
        }

        [Fact]
        public void Invalid_Budget_Spec_Returns_InvalidArguments() {
            var f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            var (code, stdout, stderr) = CliRunner.Run(new[] {
                "--input", f,
                "--budget", "not-a-valid-spec" // lacks = and max
            });

            code.Should().Be(1, stdout + Environment.NewLine + stderr); // ExitCodes.InvalidArguments
            stderr.Should().Contain("Invalid --budget value").And.Contain("not-a-valid-spec");
        }
    }
}
