using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchSessionConcurrencyTests {
        [Fact]
        public async Task Record_Is_ThreadSafe_For_Many_Writers() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            Task[] tasks = [.. Enumerable.Range(0, 100).Select(i => Task.Run(() => session.Record("Q" + i, TimeSpan.FromMilliseconds(1))))];
            await Task.WhenAll(tasks);

            QueryWatchReport report = session.Stop();
            _ = report.TotalQueries.Should().Be(100);
            _ = report.AverageDuration.Should().BeCloseTo(TimeSpan.FromMilliseconds(1), precision: TimeSpan.FromMilliseconds(1));
        }

        [Fact]
        public void Dispose_Then_Record_Should_Throw_ObjectDisposed() {
            QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Dispose();

            Action act = () => session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));
            _ = act.Should().Throw<ObjectDisposedException>();
        }
    }
}
