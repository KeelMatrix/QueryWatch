using System;
using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests {
    public class QueryWatchRedactorOrderTests {
        private sealed class ReplaceFooWithBar : KeelMatrix.QueryWatch.IQueryTextRedactor {
            public string Redact(string input) => (input ?? string.Empty).Replace("foo", "bar");
        }

        private sealed class ReplaceBarWithBaz : KeelMatrix.QueryWatch.IQueryTextRedactor {
            public string Redact(string input) => (input ?? string.Empty).Replace("bar", "baz");
        }

        [Fact]
        public void Redactors_Are_Applied_In_Order() {
            var options = new KeelMatrix.QueryWatch.QueryWatchOptions { CaptureSqlText = true };
            options.Redactors.Add(new ReplaceFooWithBar());
            options.Redactors.Add(new ReplaceBarWithBaz());

            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start(options);
            session.Record("select * from foo", TimeSpan.FromMilliseconds(1));
            var report = session.Stop();

            report.Events[0].CommandText.Should().Contain("baz");
            report.Events[0].CommandText.Should().NotContain("foo");
            report.Events[0].CommandText.Should().NotContain("bar");
        }
    }
}
