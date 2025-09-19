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
        public void Leaves_NonMatching_Text_Unchanged() {
            var r = new AwsAccessKeyRedactor();
            var input = "SELECT 1;";
            r.Redact(input).Should().Be(input);
        }
    }
}
