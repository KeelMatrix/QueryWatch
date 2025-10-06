using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class SessionLifecycleTests {
        [Fact]
        public void Dispose_Sets_StoppedAt_Automatically() {
            var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Dispose();
            session.StoppedAt.Should().NotBeNull("disposing the session should finalize it");
        }

        [Fact]
        public void Stop_Twice_Throws() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            _ = session.Stop();
            Action again = () => session.Stop();
            again.Should().Throw<InvalidOperationException>();
        }
    }
}
