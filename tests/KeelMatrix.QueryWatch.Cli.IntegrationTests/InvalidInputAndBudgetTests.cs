using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Cli.IntegrationTests {
    public class InvalidInputAndBudgetTests {
        [Fact]
        public void Missing_Input_File_Returns_InputFileNotFound() {
            // Arrange: point to a clearly non-existent file
            string missing = Path.Combine(Path.GetTempPath(), "qwatch-missing", Guid.NewGuid().ToString("N") + ".json");

            // Act
            (int code, string? stdout, string? stderr) = CliRunner.Run(["--input", missing]);

            // Assert
            _ = code.Should().Be(2, stdout + Environment.NewLine + stderr); // ExitCodes.InputFileNotFound
            _ = stderr.Should().Contain("No input JSON found.");
        }

        [Fact]
        public void Invalid_Json_Returns_JsonParseError() {
            // Arrange: create an invalid JSON file
            string tempDir = Path.Combine(Path.GetTempPath(), "qwatch-invalid-json-" + Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(tempDir);
            string invalidPath = Path.Combine(tempDir, "invalid.json");
            File.WriteAllText(invalidPath, "{ not: valid json ]");

            try {
                // Act
                (int code, string? stdout, string? stderr) = CliRunner.Run(["--input", invalidPath]);

                // Assert
                _ = code.Should().Be(3, stdout + Environment.NewLine + stderr); // ExitCodes.JsonParseError
                _ = stderr.Should().Contain("Failed to parse JSON");
            }
            finally {
                if (File.Exists(invalidPath)) File.Delete(invalidPath);
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
            }
        }

        [Fact]
        public void Missing_Budget_Value_Shows_Parse_Error() {
            string f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            (int code, string? stdout, string? stderr) = CliRunner.Run([
                "--input", f,
                "--budget" // missing value
            ]);

            _ = code.Should().Be(1, stdout + Environment.NewLine + stderr); // ExitCodes.InvalidArguments
            _ = stderr.Should().Contain("Missing value for --budget");
        }

        [Fact]
        public void Invalid_Budget_Spec_Returns_InvalidArguments() {
            string f = Path.Combine(AppContext.BaseDirectory, "Fixtures", "pattern.json");

            (int code, string? stdout, string? stderr) = CliRunner.Run([
                "--input", f,
                "--budget", "not-a-valid-spec" // lacks = and max
            ]);

            _ = code.Should().Be(1, stdout + Environment.NewLine + stderr); // ExitCodes.InvalidArguments
            _ = stderr.Should().Contain("Invalid --budget value").And.Contain("not-a-valid-spec");
        }
    }
}
