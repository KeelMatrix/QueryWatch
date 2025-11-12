using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class AwsAccessKeyRedactorTests {
        [Fact]
        public void Masks_AKIA_AccessKey() {
            AwsAccessKeyRedactor r = new();
            string key = "AKIA" + new string('A', 16);
            string input = $"/* {key} */ SELECT 1;";
            string red = r.Redact(input);
            _ = red.Should().NotContain(key);
            _ = red.Should().Contain("***");
        }

        [Fact]
        public void Does_Not_Mask_Short_Or_Invalid() {
            AwsAccessKeyRedactor r = new();
            string almost = "AKIA" + new string('A', 15); // one short
            _ = r.Redact(almost).Should().Be(almost);
            string noise = "AKIB" + new string('A', 16); // wrong prefix
            _ = r.Redact(noise).Should().Be(noise);
        }
    }
}
