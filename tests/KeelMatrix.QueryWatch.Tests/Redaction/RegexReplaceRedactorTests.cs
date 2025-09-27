using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class RegexReplaceRedactorTests {
        [Fact]
        public void Replaces_Matches_Using_Pattern() {
            var r = new RegexReplaceRedactor(@"\d+", "***");
            r.Redact("Id=123; Name='A'").Should().Be("Id=***; Name='A'");
        }
    }
}
