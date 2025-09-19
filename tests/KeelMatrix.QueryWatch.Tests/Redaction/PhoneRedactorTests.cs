using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class PhoneRedactorTests {
        [Fact]
        public void Masks_International_Style_Phone_Number() {
            var r = new PhoneRedactor();
            var input = "Call me at +1 202-555-0176 to confirm.";
            var red = r.Redact(input);
            red.Should().NotContain("+1 202-555-0176");
            red.Should().Contain("***");
        }

        [Fact]
        public void Ignores_Short_Number_Like_Extension() {
            var r = new PhoneRedactor();
            var input = "Ext 1234";
            r.Redact(input).Should().Be(input);
        }
    }
}
