using FluentAssertions;
using KeelMatrix.QueryWatch.Security;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Security {
    public class RedactionHelperTests {
        [Fact]
        public void Redacts_Email_Addresses() {
            const string input = "Please contact john.doe+test@example.co.uk for details.";
            string redacted = RedactionHelper.Redact(input);
            _ = redacted.Should().NotBeNull();
            _ = redacted.Should().Contain("&lt;REDACTED_EMAIL&gt;");
        }

        [Fact]
        public void Redacts_Long_Tokens() {
            const string token = "ABCdef1234567890xyzTokenValue"; // >= 20 chars, URL-safe
            const string input = "token=" + token;
            string redacted = RedactionHelper.Redact(input);
            _ = redacted.Should().Contain("&lt;REDACTED_TOKEN&gt;");
            _ = redacted.Should().NotContain(token);
        }

        [Fact]
        public void Empty_String_Returns_Empty() {
            string redacted = RedactionHelper.Redact(string.Empty);
            _ = redacted.Should().Be(string.Empty);
        }

        [Fact]
        public void No_Match_Returns_Original() {
            const string input = "This string contains nothing sensitive.";
            string redacted = RedactionHelper.Redact(input);
            _ = redacted.Should().Be(input);
        }
    }
}
