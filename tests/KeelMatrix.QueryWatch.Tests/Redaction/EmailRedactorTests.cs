using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class EmailRedactorTests {
        [Fact]
        public void Masks_Emails_In_Free_Text() {
            EmailRedactor r = new();
            const string input = "/* contact: admin@example.com */ SELECT 1;";
            string red = r.Redact(input);
            _ = red.Should().NotContain("admin@example.com").And.Contain("***");
        }

        [Fact]
        public void Leaves_Text_Without_Emails() {
            EmailRedactor r = new();
            const string input = "SELECT 1;";
            _ = r.Redact(input).Should().Be(input);
        }

        [Fact]
        public void Handles_Plus_Tagging_And_Trailing_Punctuation() {
            EmailRedactor r = new();
            const string input = "Please email Admin+test@Example.COM, thanks.";
            string red = r.Redact(input);
            _ = red.Should().NotContain("Admin+test@Example.COM");
            _ = red.Should().Contain("***, thanks.");
        }

        [Fact]
        public void Idempotent_For_Emails() {
            EmailRedactor r = new();
            const string input = "user@example.com";
            string once = r.Redact(input);
            string twice = r.Redact(once);
            _ = twice.Should().Be(once);
        }
    }
}
