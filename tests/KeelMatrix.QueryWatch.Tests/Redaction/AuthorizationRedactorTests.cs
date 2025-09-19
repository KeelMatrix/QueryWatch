using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class AuthorizationRedactorTests {
        [Fact]
        public void Masks_Bearer_Token() {
            var r = new AuthorizationRedactor();
            var input = "Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.abc.def";
            var red = r.Redact(input);
            red.Should().Be("Authorization: ***");
        }

        [Fact]
        public void Masks_Basic_Token_Ignoring_Case() {
            var r = new AuthorizationRedactor();
            var input = "authorization: basic dXNlcjpwYXNz";
            var red = r.Redact(input);
            red.Should().Be("Authorization: ***");
        }
    }
}
