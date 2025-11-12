using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class AuthorizationRedactorTests {
        [Fact]
        public void Masks_Bearer_Token_MultiLine() {
            AuthorizationRedactor r = new();
            const string input = "/*\nAuthorization: Bearer ey...abc.def\nOther: 123\n*/";
            string red = r.Redact(input);
            _ = red.Should().Contain("Authorization: ***");
            _ = red.Should().Contain("Other: 123");
        }

        [Fact]
        public void Masks_Basic_Token_Ignoring_Case() {
            AuthorizationRedactor r = new();
            const string input = "authorization: basic dXNlcjpwYXNz";
            _ = r.Redact(input).Should().Be("Authorization: ***");
        }
    }
}
