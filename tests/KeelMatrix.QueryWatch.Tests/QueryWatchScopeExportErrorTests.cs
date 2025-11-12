using FluentAssertions;
using KeelMatrix.QueryWatch.Testing;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchScopeExportErrorTests {
        [Fact]
        public void Dispose_Swallows_Export_Errors_And_Still_Asserts_Thresholds() {
            string root = Path.Combine(Path.GetTempPath(), "QueryWatchTests", Guid.NewGuid().ToString("N"));
            _ = Directory.CreateDirectory(root);

            // Create a *file* named 'artifacts' so that using it as a directory will fail consistently cross-platform.
            string fakeDir = Path.Combine(root, "artifacts");
            File.WriteAllText(fakeDir, "not a directory");

            string path = Path.Combine(fakeDir, "qwatch.report.json");

            using QueryWatchSession session = QueryWatcher.Start();
            var scope = new QueryWatchScope(session, maxQueries: 1, exportJsonPath: path, sampleTop: 2);

            session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(2));

            Action act = scope.Dispose;

            _ = act.Should().Throw<QueryWatchViolationException>(
                "budget failure should be reported even if JSON export fails"
            );

            _ = File.Exists(path).Should().BeFalse("export should fail and be swallowed due to invalid directory path");
        }
    }
}
