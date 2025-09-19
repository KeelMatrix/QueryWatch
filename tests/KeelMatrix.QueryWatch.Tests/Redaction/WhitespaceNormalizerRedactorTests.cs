using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class WhitespaceNormalizerRedactorTests {
        [Fact]
        public void Collapses_All_Whitespace_To_Single_Spaces_And_Trims() {
            var r = new WhitespaceNormalizerRedactor();
            var input = "  SELECT\n\t*  FROM   Users  WHERE  Id = 1 \r\n";
            var red = r.Redact(input);
            red.Should().Be("SELECT * FROM Users WHERE Id = 1");
        }

        [Fact]
        public void Empty_Input_Returns_Empty_String() {
            var r = new WhitespaceNormalizerRedactor();
            r.Redact(string.Empty).Should().Be(string.Empty);
        }
    }
}
