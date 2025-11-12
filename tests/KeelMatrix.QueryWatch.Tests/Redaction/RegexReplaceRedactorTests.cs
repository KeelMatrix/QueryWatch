using System.Text.RegularExpressions;
using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public partial class RegexReplaceRedactorTests {
        [Fact]
        public void Replaces_Matches_Using_Pattern() {
            RegexReplaceRedactor r = new(@"\d+", "***");
            _ = r.Redact("Id=123; Name='A'").Should().Be("Id=***; Name='A'");
        }

        [Fact]
        public void Supports_Precompiled_Regex_And_Handles_Null_Input() {
            Regex compiled = FooRegex();
            RegexReplaceRedactor r = new(compiled, "***");
            _ = r.Redact("FOO bar").Should().Be("*** bar");
            _ = r.Redact(null!).Should().Be(string.Empty);
        }

        [GeneratedRegex("foo", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
        private static partial Regex FooRegex();
    }
}
