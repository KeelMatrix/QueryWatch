using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class ConnectionStringPasswordRedactorTests {
        [Fact]
        public void Masks_Password_And_Pwd_Variants_With_Spaces() {
            ConnectionStringPasswordRedactor r = new();
            const string pass = "TopSecret";
            const string input = $"Server=.;User ID=sa;Password={pass};Database=App; Pwd = s3cr3t ;";
            string red = r.Redact(input);
            _ = red.Should().Contain("Password=***");
            _ = red.Should().Contain("Pwd=***");
            _ = red.Should().Contain("Server=.;").And.Contain("Database=App;");
            _ = red.Should().NotContain("TopSecret").And.NotContain("s3cr3t");
        }

        [Fact]
        public void Leaves_Strings_Without_Password_Unchanged() {
            ConnectionStringPasswordRedactor r = new();
            const string input = "Server=.;Integrated Security=true;";
            _ = r.Redact(input).Should().Be(input);
        }

        [Fact]
        public void Masks_Quoted_Passwords_With_Semicolons_Inside() {
            ConnectionStringPasswordRedactor r = new();
            const string input1 = "Password=\"sec;ret;value\";User Id=sa;";
            string red1 = r.Redact(input1);
            _ = red1.Should().Contain("Password=***;");
            _ = red1.Should().NotContain("sec;ret;value");

            const string input2 = "Pwd='p;a;s;s';Server=.;";
            string red2 = r.Redact(input2);
            _ = red2.Should().Contain("Pwd=***;");
            _ = red2.Should().NotContain("p;a;s;s");
        }
    }
}
