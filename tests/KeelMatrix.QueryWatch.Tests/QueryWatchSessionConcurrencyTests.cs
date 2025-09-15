using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchSessionConcurrencyTests {
        [Fact]
        public async Task Record_Is_ThreadSafe_For_Many_Writers() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            var tasks = Enumerable.Range(0, 100)
                                  .Select(i => Task.Run(() => session.Record("Q" + i, TimeSpan.FromMilliseconds(1))))
                                  .ToArray();
            await Task.WhenAll(tasks);

            var report = session.Stop();
            report.TotalQueries.Should().Be(100);
            report.AverageDuration.Should().BeCloseTo(TimeSpan.FromMilliseconds(1), precision: TimeSpan.FromMilliseconds(1));
        }

        [Fact]
        public void Dispose_Then_Record_Should_Throw_ObjectDisposed() {
            var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Dispose();

            Action act = () => session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));
            act.Should().Throw<ObjectDisposedException>();
        }
    }
}
