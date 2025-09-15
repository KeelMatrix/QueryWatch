using System;
using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class SessionLifecycleTests {
        [Fact]
        public void Dispose_Sets_StoppedAt_Automatically() {
            var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            session.Dispose();

            // Current skeleton does not set StoppedAt on Dispose → this should FAIL.
            session.StoppedAt.Should().NotBeNull("disposing the session should finalize it");
        }

        [Fact]
        public void Stop_Twice_Throws() {
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start();
            var r1 = session.Stop();
            Action again = () => session.Stop();

            // Current skeleton allows repeated Stop calls → this should FAIL.
            again.Should().Throw<InvalidOperationException>();
        }
    }
}
