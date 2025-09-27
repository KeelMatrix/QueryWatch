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

        [Fact]
        public void Supports_Precompiled_Regex_And_Handles_Null_Input() {
            var compiled = new System.Text.RegularExpressions.Regex("foo", System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            var r = new RegexReplaceRedactor(compiled, "***");
            r.Redact("FOO bar").Should().Be("*** bar");
            r.Redact(null!).Should().Be(string.Empty);
        }
    }
}
