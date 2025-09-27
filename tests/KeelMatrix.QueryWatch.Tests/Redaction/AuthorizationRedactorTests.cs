using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class AuthorizationRedactorTests {
        [Fact]
        public void Masks_Bearer_Token_MultiLine() {
            var r = new AuthorizationRedactor();
            var input = "/*\nAuthorization: Bearer ey...abc.def\nOther: 123\n*/";
            var red = r.Redact(input);
            red.Should().Contain("Authorization: ***");
            red.Should().Contain("Other: 123");
        }

        [Fact]
        public void Masks_Basic_Token_Ignoring_Case() {
            var r = new AuthorizationRedactor();
            var input = "authorization: basic dXNlcjpwYXNz";
            r.Redact(input).Should().Be("Authorization: ***");
        }
    }
}
