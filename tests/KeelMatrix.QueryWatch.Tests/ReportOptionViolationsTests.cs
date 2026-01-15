// Copyright (c) KeelMatrix

using FluentAssertions;
using KeelMatrix.QueryWatch.Assertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class ReportOptionViolationsTests {
        [Fact]
        public void ShouldHaveExecutedAtMost_Respects_MaxQueries() {
            using QueryWatchSession session = new();
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(2));
            QueryWatchReport report = session.Complete();

            Action act = () => report.ShouldHaveExecutedAtMost(1);

            _ = act.Should().Throw<QueryWatchViolationException>()
               .Which.Message.Should().Contain("Expected ≤1 queries");
        }

        [Fact]
        public void NoBudgetViolations_DoesNotThrow() {
            using QueryWatchSession session = new();
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(3));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(4));
            QueryWatchReport report = session.Complete();

            Action act = () => {
                report.ShouldHaveExecutedAtMost(5);
                report.ShouldHaveMaxAverageTime(TimeSpan.FromMilliseconds(10));
                report.ShouldHaveMaxTotalTime(TimeSpan.FromMilliseconds(100));
            };

            _ = act.Should().NotThrow();
        }

        [Fact]
        public void Helper_Asserts_DoNotThrow_When_UnderLimits() {
            using QueryWatchSession session = new();
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(2));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(2));
            QueryWatchReport report = session.Complete();

            _ = report.Invoking(r => r.ShouldHaveExecutedAtMost(2)).Should().NotThrow();
            _ = report.Invoking(r => r.ShouldHaveMaxAverageTime(TimeSpan.FromMilliseconds(3))).Should().NotThrow();
            _ = report.Invoking(r => r.ShouldHaveMaxTotalTime(TimeSpan.FromMilliseconds(5))).Should().NotThrow();
        }

        [Fact]
        public void Helper_Asserts_Throw_When_OverLimits() {
            using QueryWatchSession session = new();
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(10));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(10));
            QueryWatchReport report = session.Complete();

            _ = report.Invoking(r => r.ShouldHaveExecutedAtMost(1))
                  .Should().Throw<QueryWatchViolationException>();
            _ = report.Invoking(r => r.ShouldHaveMaxAverageTime(TimeSpan.FromMilliseconds(5)))
                  .Should().Throw<QueryWatchViolationException>();
            _ = report.Invoking(r => r.ShouldHaveMaxTotalTime(TimeSpan.FromMilliseconds(10)))
                  .Should().Throw<QueryWatchViolationException>();
        }
    }
}
