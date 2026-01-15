// Copyright (c) KeelMatrix

using FluentAssertions;
using KeelMatrix.QueryWatch.Assertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchReportMessageTests {
        [Fact]
        public void Assertions_Throw_On_First_Violation() {
            using QueryWatchSession session = new();

            // Two slow queries → violate all three
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(2));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(2));
            QueryWatchReport report = session.Complete();

            Action act = () => report.ShouldHaveExecutedAtMost(1);
            act.Should().Throw<QueryWatchViolationException>();

            Action act2 = () => report.ShouldHaveMaxAverageTime(TimeSpan.FromMilliseconds(1));
            act2.Should().Throw<QueryWatchViolationException>();

            Action act3 = () => report.ShouldHaveMaxTotalTime(TimeSpan.FromMilliseconds(1));
            act3.Should().Throw<QueryWatchViolationException>();
        }
    }
}
