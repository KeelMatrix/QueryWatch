using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class ConnectionStringPasswordRedactorTests {
        [Fact]
        public void Masks_Password_And_Pwd_Variants_With_Spaces() {
            var r = new ConnectionStringPasswordRedactor();
            var input = "Server=.;User ID=sa;Password=TopSecret;Database=App; Pwd = s3cr3t ;";
            var red = r.Redact(input);
            red.Should().Contain("Password=***");
            red.Should().Contain("Pwd=***");
            red.Should().Contain("Server=.;").And.Contain("Database=App;");
            red.Should().NotContain("TopSecret").And.NotContain("s3cr3t");
        }

        [Fact]
        public void Leaves_Strings_Without_Password_Unchanged() {
            var r = new ConnectionStringPasswordRedactor();
            var input = "Server=.;Integrated Security=true;";
            r.Redact(input).Should().Be(input);
        }
    }
}
