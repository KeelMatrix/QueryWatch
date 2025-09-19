using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class EmailRedactorTests {
        [Fact]
        public void Masks_Email_Addresses() {
            var r = new EmailRedactor();
            var input = "/* contact: admin@example.com */ SELECT 1;";
            var red = r.Redact(input);
            red.Should().NotContain("admin@example.com");
            red.Should().Contain("***");
        }

        [Fact]
        public void Leaves_Text_Without_Emails_Unchanged() {
            var r = new EmailRedactor();
            var input = "SELECT 1;";
            r.Redact(input).Should().Be(input);
        }
    }
}
