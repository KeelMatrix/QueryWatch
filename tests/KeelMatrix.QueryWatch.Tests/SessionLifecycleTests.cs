// Copyright (c) KeelMatrix

using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class SessionLifecycleTests {
        [Fact]
        public void Dispose_Sets_StoppedAt_Automatically() {
            using QueryWatchSession session = new();
            session.Dispose();
            _ = session.StoppedAt.Should().NotBeNull("disposing the session should finalize it");
        }

        [Fact]
        public void Stop_Twice_Must_Not_Throw() {
            using QueryWatchSession session = new();
            _ = session.Complete();
            Action again = () => session.Complete();
            _ = again.Should().NotThrow();
        }
    }
}
