using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class GoogleApiKeyRedactorTests {
        [Fact]
        public void Masks_ApiKey_With_AIZa_Prefix() {
            var r = new GoogleApiKeyRedactor();
            var tail = new string('A', 35);
            var key = "AIza" + tail;
            var input = $"/* {key} */";
            var red = r.Redact(input);
            red.Should().NotContain(key).And.Contain("***");
        }

        [Fact]
        public void Does_Not_Mask_When_Length_Is_Wrong() {
            var r = new GoogleApiKeyRedactor();
            var key = "AIza" + new string('B', 34); // one char short
            r.Redact(key).Should().Be(key);
        }
    }
}
