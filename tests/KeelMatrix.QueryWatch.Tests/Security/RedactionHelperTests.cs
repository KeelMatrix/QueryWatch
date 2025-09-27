#nullable enable
using FluentAssertions;
using KeelMatrix.QueryWatch.Security;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Security {
    public class RedactionHelperTests {
        [Fact]
        public void Redacts_Email_Addresses() {
            var input = "Please contact john.doe+test@example.co.uk for details.";
            var redacted = RedactionHelper.Redact(input);
            redacted.Should().NotBeNull();
            redacted.Should().Contain("&lt;REDACTED_EMAIL&gt;");
        }

        [Fact]
        public void Redacts_Long_Tokens() {
            var token = "ABCdef1234567890xyzTokenValue"; // >= 20 chars, URL-safe
            var input = "token=" + token;
            var redacted = RedactionHelper.Redact(input);
            redacted.Should().Contain("&lt;REDACTED_TOKEN&gt;");
            redacted.Should().NotContain(token);
        }

        [Fact]
        public void Empty_String_Returns_Empty() {
            var redacted = RedactionHelper.Redact(string.Empty);
            redacted.Should().Be(string.Empty);
        }

        [Fact]
        public void No_Match_Returns_Original() {
            var input = "This string contains nothing sensitive.";
            var redacted = RedactionHelper.Redact(input);
            redacted.Should().Be(input);
        }
    }
}
