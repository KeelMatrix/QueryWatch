// Copyright (c) KeelMatrix

using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchSessionLifecycleTests {
        [Fact]
        public void Complete_Returns_Same_Cached_Report_Instance() {
            using QueryWatchSession session = new();

            session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));

            QueryWatchReport r1 = session.Complete();
            QueryWatchReport r2 = session.Complete();

            _ = ReferenceEquals(r1, r2).Should().BeTrue("Complete must return cached snapshot");
        }

        [Fact]
        public void Dispose_Before_Complete_Stops_Session_But_Report_Is_Not_Retrievable() {
            QueryWatchSession session = new();
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));

            session.Dispose();

            _ = session.StoppedAt.Should().NotBeNull("Dispose must still stop the session"); // contract
            Action act = () => session.Complete();
            _ = act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void Dispose_After_Complete_Is_Idempotent() {
            using QueryWatchSession session = new();

            session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));
            _ = session.Complete();

            Action act = () => session.Dispose();
            _ = act.Should().NotThrow("Dispose must be safe after Complete");
        }

        [Fact]
        public void Record_After_Dispose_Throws_ObjectDisposed() {
            QueryWatchSession session = new();
            session.Dispose();

            Action act = () => session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));
            _ = act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task Complete_Sets_StoppedAt_Exactly_Once() {
            using QueryWatchSession session = new();

            _ = session.StoppedAt.Should().BeNull();

            QueryWatchReport r1 = session.Complete();
            DateTimeOffset? stopped1 = session.StoppedAt;

            _ = stopped1.Should().NotBeNull();

            await Task.Delay(10);

            QueryWatchReport r2 = session.Complete();
            DateTimeOffset? stopped2 = session.StoppedAt;

            _ = stopped2.Should().Be(stopped1, "StoppedAt must not change after first stop");
            _ = ReferenceEquals(r1, r2).Should().BeTrue();
        }

        [Fact]
        public void Report_Is_Snapshot_And_Does_Not_Change_After_Completion() {
            using QueryWatchSession session = new();

            session.Record("A", TimeSpan.FromMilliseconds(1));
            QueryWatchReport report = session.Complete();

            int countAtComplete = report.TotalQueries;

            Action act = () => session.Record("B", TimeSpan.FromMilliseconds(1));
            _ = act.Should().Throw<InvalidOperationException>();

            _ = report.TotalQueries.Should().Be(countAtComplete, "snapshot must remain immutable");
        }

        [Fact]
        public void Complete_With_No_Events_Produces_Valid_Report() {
            using QueryWatchSession session = new();
            QueryWatchReport report = session.Complete();

            _ = report.Events.Should().NotBeNull();
            _ = report.Events.Should().BeEmpty();
            _ = report.TotalQueries.Should().Be(0);
            _ = report.TotalDuration.Should().Be(TimeSpan.Zero);
            _ = report.AverageDuration.Should().Be(TimeSpan.Zero);
            _ = report.StartedAt.Should().BeBefore(report.StoppedAt);
        }

        [Fact]
        public void Report_Timestamps_Are_Consistent_With_Session() {
            using QueryWatchSession session = new();

            DateTimeOffset started = session.StartedAt;

            session.Record("SELECT 1", TimeSpan.FromMilliseconds(2));
            QueryWatchReport report = session.Complete();

            _ = report.StartedAt.Should().Be(started);
            _ = report.StoppedAt.Should().Be(session.StoppedAt);
            _ = report.StoppedAt.Should().BeAfter(report.StartedAt);
        }

        [Fact]
        public void Concurrent_Record_Operations_Do_Not_Corrupt_State() {
            using QueryWatchSession session = new();

            Parallel.For(0, 1_000, i =>
                session.Record($"Q{i}", TimeSpan.FromMilliseconds(1)));

            QueryWatchReport report = session.Complete();

            _ = report.TotalQueries.Should().Be(1000);
            _ = report.Events.Select(e => e.CommandText)
                .Distinct()
                .Count()
                .Should()
                .Be(1000, "all events should be captured exactly once");
        }
    }
}
