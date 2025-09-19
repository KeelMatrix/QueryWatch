using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class RegexReplaceRedactorTests {
        [Fact]
        public void Replaces_Matches_Case_Insensitive() {
            var r = new RegexReplaceRedactor(@"token\s*=\s*\w+", "***");
            var input = "TOKEN=abc Token=XYZ";
            r.Redact(input).Should().Be("*** ***");
        }

        [Fact]
        public void Empty_Input_Returns_Empty() {
            var r = new RegexReplaceRedactor("foo", "***");
            r.Redact(string.Empty).Should().Be(string.Empty);
        }
    }
}
