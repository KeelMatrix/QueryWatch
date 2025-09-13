using System;
using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests
{
    public class ReportFormattingTests
    {
        [Fact]
        public void ThrowIfViolations_Message_Should_Contain_Summary_Header()
        {
            var options = new KeelMatrix.QueryWatch.QueryWatchOptions { MaxQueries = 1 };
            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start(options);
            session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));
            session.Record("SELECT 2", TimeSpan.FromMilliseconds(2));
            var report = session.Stop();

            Action act = () => report.ThrowIfViolations();

            // Current skeleton message is a simple concatenation without a "Summary:" header → this should FAIL.
            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*Summary:*");
        }
    }
}
