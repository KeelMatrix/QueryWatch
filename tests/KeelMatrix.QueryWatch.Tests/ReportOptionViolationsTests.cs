using System;
using FluentAssertions;
using KeelMatrix.QueryWatch;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests
{
    public class ReportOptionViolationsTests
    {
        [Fact]
        public void ThrowIfViolations_Respects_MaxQueries()
        {
            var options = new QueryWatchOptions { MaxQueries = 1 };
            using var session = QueryWatcher.Start(options);
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(2));
            var report = session.Stop();

            Action act = () => report.ThrowIfViolations();

            act.Should().Throw<QueryWatchViolationException>()
               .Which.Message.Should().Contain("MaxQueries=1").And.Contain("Summary:");
        }

        [Fact]
        public void ThrowIfViolations_Respects_MaxAverageDuration()
        {
            var options = new QueryWatchOptions { MaxAverageDuration = TimeSpan.FromMilliseconds(5) };
            using var session = QueryWatcher.Start(options);
            // Avg = (8 + 4) / 2 = 6 ms > 5 ms
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(8));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(4));
            var report = session.Stop();

            Action act = () => report.ThrowIfViolations();

            act.Should().Throw<QueryWatchViolationException>()
               .Which.Message.Should().Contain("MaxAverageDuration=").And.Contain("Summary:");
        }

        [Fact]
        public void ThrowIfViolations_Respects_MaxTotalDuration()
        {
            var options = new QueryWatchOptions { MaxTotalDuration = TimeSpan.FromMilliseconds(5) };
            using var session = QueryWatcher.Start(options);
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(4));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(3)); // total 7ms > 5ms
            var report = session.Stop();

            Action act = () => report.ThrowIfViolations();

            act.Should().Throw<QueryWatchViolationException>()
               .Which.Message.Should().Contain("MaxTotalDuration=").And.Contain("Summary:");
        }

        [Fact]
        public void ThrowIfViolations_NoViolations_DoesNotThrow()
        {
            var options = new QueryWatchOptions
            {
                MaxQueries = 5,
                MaxAverageDuration = TimeSpan.FromMilliseconds(10),
                MaxTotalDuration = TimeSpan.FromMilliseconds(100)
            };
            using var session = QueryWatcher.Start(options);
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(3));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(4));
            var report = session.Stop();

            Action act = () => report.ThrowIfViolations();

            act.Should().NotThrow();
        }

        [Fact]
        public void Helper_Asserts_DoNotThrow_When_UnderLimits()
        {
            using var session = QueryWatcher.Start();
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(2));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(2));
            var report = session.Stop();

            report.Invoking(r => r.ShouldHaveExecutedAtMost(2)).Should().NotThrow();
            report.Invoking(r => r.ShouldHaveMaxAverageTime(TimeSpan.FromMilliseconds(3))).Should().NotThrow();
            report.Invoking(r => r.ShouldHaveMaxTotalTime(TimeSpan.FromMilliseconds(5))).Should().NotThrow();
        }

        [Fact]
        public void Helper_Asserts_Throw_When_OverLimits()
        {
            using var session = QueryWatcher.Start();
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(10));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(10));
            var report = session.Stop();

            report.Invoking(r => r.ShouldHaveExecutedAtMost(1))
                  .Should().Throw<QueryWatchViolationException>();
            report.Invoking(r => r.ShouldHaveMaxAverageTime(TimeSpan.FromMilliseconds(5)))
                  .Should().Throw<QueryWatchViolationException>();
            report.Invoking(r => r.ShouldHaveMaxTotalTime(TimeSpan.FromMilliseconds(10)))
                  .Should().Throw<QueryWatchViolationException>();
        }
    }
}
