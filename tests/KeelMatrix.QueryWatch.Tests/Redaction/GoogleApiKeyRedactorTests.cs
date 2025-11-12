using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class GoogleApiKeyRedactorTests {
        [Fact]
        public void Masks_ApiKey_With_AIZa_Prefix() {
            GoogleApiKeyRedactor r = new();
            string tail = new('A', 35);
            string key = "AIza" + tail;
            string input = $"/* {key} */";
            string red = r.Redact(input);
            _ = red.Should().NotContain(key).And.Contain("***");
        }

        [Fact]
        public void Does_Not_Mask_When_Length_Is_Wrong() {
            GoogleApiKeyRedactor r = new();
            string key = "AIza" + new string('B', 34); // one char short
            _ = r.Redact(key).Should().Be(key);
        }
    }
}
