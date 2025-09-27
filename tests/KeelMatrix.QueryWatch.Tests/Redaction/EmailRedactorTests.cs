using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class EmailRedactorTests {
        [Fact]
        public void Masks_Emails_In_Free_Text() {
            var r = new EmailRedactor();
            var input = "/* contact: admin@example.com */ SELECT 1;";
            var red = r.Redact(input);
            red.Should().NotContain("admin@example.com").And.Contain("***");
        }

        [Fact]
        public void Leaves_Text_Without_Emails() {
            var r = new EmailRedactor();
            var input = "SELECT 1;";
            r.Redact(input).Should().Be(input);
        }

        [Fact]
        public void Handles_Plus_Tagging_And_Trailing_Punctuation() {
            var r = new EmailRedactor();
            var input = "Please email Admin+test@Example.COM, thanks.";
            var red = r.Redact(input);
            red.Should().NotContain("Admin+test@Example.COM");
            red.Should().Contain("***, thanks.");
        }

        [Fact]
        public void Idempotent_For_Emails() {
            var r = new EmailRedactor();
            var input = "user@example.com";
            var once = r.Redact(input);
            var twice = r.Redact(once);
            twice.Should().Be(once);
        }
    }
}
