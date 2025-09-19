using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class ConnectionStringPasswordRedactorTests {
        [Fact]
        public void Masks_Password_And_Pwd_Variants() {
            var r = new ConnectionStringPasswordRedactor();
            var input = "Server=.;User ID=sa;Password=TopSecret;Database=App; Pwd = s3cr3t ;";
            var red = r.Redact(input);
            red.Should().Contain("Password=***");
            red.Should().Contain("Pwd=***");
            red.Should().NotContain("TopSecret");
            red.Should().NotContain("s3cr3t");
        }
    }
}
