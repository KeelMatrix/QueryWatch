// Copyright (c) KeelMatrix

using FluentAssertions;
using KeelMatrix.QueryWatch.Assertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class ReportFormattingTests {
        [Fact]
        public void ThrowIfViolations_Message_Should_Contain_Summary_Header() {
            using QueryWatchSession session = new();
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(2));
            QueryWatchReport report = session.Complete();

            Action act = () => report.ShouldHaveExecutedAtMost(1);

            _ = act.Should().Throw<KeelMatrix.QueryWatch.QueryWatchViolationException>()
               .Which.Message.Should().Contain("Summary:");
        }
    }
}
