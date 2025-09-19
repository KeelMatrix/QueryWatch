using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class GoogleApiKeyRedactorTests {
        [Fact]
        public void Masks_Google_Api_Key() {
            var r = new GoogleApiKeyRedactor();
            var key = "AIza" + new string('A', 35);
            var input = $"/* key={key} */";
            var red = r.Redact(input);
            red.Should().NotContain(key);
            red.Should().Contain("***");
        }
    }
}
