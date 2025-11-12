using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class SessionRecordGuardTests {
        [Fact]
        public void Record_After_Stop_Throws() {
            using QueryWatchSession session = QueryWatcher.Start();
            _ = session.Stop();
            Action act = () => session.Record("SELECT 1", TimeSpan.FromMilliseconds(1));

            _ = act.Should().Throw<InvalidOperationException>()
               .WithMessage("*has been stopped*");
        }

        [Fact]
        public void Record_When_CaptureSqlText_False_Stores_Empty_Text() {
            QueryWatchOptions options = new() { CaptureSqlText = false };
            using QueryWatchSession session = QueryWatcher.Start(options);
            session.Record("SELECT secret FROM t", TimeSpan.FromMilliseconds(2));
            QueryWatchReport report = session.Stop();

            _ = report.Events.Should().HaveCount(1);
            _ = report.Events[0].CommandText.Should().Be(string.Empty);
        }
    }
}
