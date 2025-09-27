using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class WhitespaceNormalizerRedactorTests {
        [Fact]
        public void Collapses_All_Whitespace_And_Trims() {
            var r = new WhitespaceNormalizerRedactor();
            var input = " \tSELECT \n *  \r\n  FROM   Users  ";
            r.Redact(input).Should().Be("SELECT * FROM Users");
        }
    }
}
