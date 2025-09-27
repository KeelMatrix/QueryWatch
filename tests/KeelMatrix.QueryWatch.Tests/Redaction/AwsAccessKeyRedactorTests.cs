using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class AwsAccessKeyRedactorTests {
        [Fact]
        public void Masks_AKIA_AccessKey() {
            var r = new AwsAccessKeyRedactor();
            var key = "AKIA" + new string('A', 16);
            var input = $"/* {key} */ SELECT 1;";
            var red = r.Redact(input);
            red.Should().NotContain(key);
            red.Should().Contain("***");
        }

        [Fact]
        public void Does_Not_Mask_Short_Or_Invalid() {
            var r = new AwsAccessKeyRedactor();
            var almost = "AKIA" + new string('A', 15); // one short
            r.Redact(almost).Should().Be(almost);
            var noise = "AKIB" + new string('A', 16); // wrong prefix
            r.Redact(noise).Should().Be(noise);
        }
    }
}
