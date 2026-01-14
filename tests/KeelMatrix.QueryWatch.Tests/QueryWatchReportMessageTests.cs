// Copyright (c) KeelMatrix

using FluentAssertions;
using KeelMatrix.QueryWatch.Assertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchReportMessageTests {
        [Fact]
        public void ThrowIfViolations_Aggregates_All_Problems_And_Prefixes_With_Summary_Header() {
            using QueryWatchSession session = new();

            // Two slow queries → violate all three
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(2));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(2));
            QueryWatchReport report = session.Complete();

            Action act = () => {
                report.ShouldHaveExecutedAtMost(1);
                report.ShouldHaveMaxAverageTime(TimeSpan.FromMilliseconds(1));
                report.ShouldHaveMaxTotalTime(TimeSpan.FromMilliseconds(1));
            };

            _ = act.Should().Throw<KeelMatrix.QueryWatch.QueryWatchViolationException>()
               .Which.Message.Should().Contain("Summary:")
                               .And.Contain("MaxQueries=")
                               .And.Contain("MaxAverageDuration=")
                               .And.Contain("MaxTotalDuration=");
        }
    }
}
