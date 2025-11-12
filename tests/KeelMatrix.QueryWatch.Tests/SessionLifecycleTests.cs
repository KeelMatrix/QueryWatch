using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class SessionLifecycleTests {
        [Fact]
        public void Dispose_Sets_StoppedAt_Automatically() {
            QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Dispose();
            _ = session.StoppedAt.Should().NotBeNull("disposing the session should finalize it");
        }

        [Fact]
        public void Stop_Twice_Throws() {
            using QueryWatchSession session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            _ = session.Stop();
            Action again = () => session.Stop();
            _ = again.Should().Throw<InvalidOperationException>();
        }
    }
}
