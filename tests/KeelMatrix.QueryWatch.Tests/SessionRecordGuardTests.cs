using System;
using FluentAssertions;
using KeelMatrix.QueryWatch;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests
{
    public class SessionRecordGuardTests
    {
        [Fact]
        public void Record_After_Stop_Throws()
        {
            using var session = QueryWatcher.Start();
            var report = session.Stop();
            Action act = () => session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("*has been stopped*");
        }

        [Fact]
        public void Record_When_CaptureSqlText_False_Stores_Empty_Text()
        {
            var options = new QueryWatchOptions { CaptureSqlText = false };
            using var session = QueryWatcher.Start(options);
            session.Record("SELECT secret FROM t", TimeSpan.FromMilliseconds(2));
            var report = session.Stop();

            report.Events.Should().HaveCount(1);
            report.Events[0].CommandText.Should().Be(string.Empty);
        }
    }
}
