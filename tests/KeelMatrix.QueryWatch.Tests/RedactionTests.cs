using System;
using FluentAssertions;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests
{
    public class RedactionTests
    {
        private sealed class DigitsToAsterisks : KeelMatrix.QueryWatch.IQueryTextRedactor
        {
            public string Redact(string input) => System.Text.RegularExpressions.Regex.Replace(input ?? string.Empty, @"\d", "*");
        }

        [Fact]
        public void Record_Applies_Redactors_To_CommandText()
        {
            var options = new KeelMatrix.QueryWatch.QueryWatchOptions {
                CaptureSqlText = true
            };
            options.Redactors.Add(new DigitsToAsterisks());

            using var session = KeelMatrix.QueryWatch.QueryWatcher.Start(options);
            session.Record("SELECT * FROM Users WHERE Id=123", TimeSpan.FromMilliseconds(5));
            var report = session.Stop();

            report.Events.Should().HaveCount(1);
            var recorded = report.Events[0].CommandText;

            // Expected: digits are masked. Current skeleton does NOT implement this → this assertion should FAIL.
            recorded.Should().NotContain("123");
            recorded.Should().Contain("***");
        }
    }
}
