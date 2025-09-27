using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class PhoneRedactorTests {
        [Fact]
        public void Masks_Common_Phone_Formats() {
            var r = new PhoneRedactor();
            r.Redact("+1 (555) 012-3456").Should().Be("***");
            r.Redact("tel=+44 20 7946 0958").Should().Be("tel=***");
        }

        [Fact]
        public void Does_Not_Mask_Short_Numbers() {
            var r = new PhoneRedactor();
            r.Redact("code=123-45").Should().Be("code=123-45");
        }

        [Theory]
        [InlineData("(555) 123-4567")]
        [InlineData("555-123-4567")]
        [InlineData("555.123.4567")]
        [InlineData("+49-(030)-1234-5678")]
        [InlineData("+81 3 1234 5678")]
        public void Masks_International_And_Various_Separators(string number) {
            var r = new PhoneRedactor();
            r.Redact(number).Should().Be("***");
        }

        [Fact]
        public void Masks_Only_Phone_Leaving_Text_Intact() {
            var r = new PhoneRedactor();
            var input = "Call me at +1 650 555 0000 tomorrow.";
            r.Redact(input).Should().Be("Call me at *** tomorrow.");
        }

        [Fact]
        public void Masks_Multiple_Phone_Numbers_In_Same_String() {
            var r = new PhoneRedactor();
            var input = "US:+1 650 555 0000; UK:+44 20 7946 0958";
            r.Redact(input).Should().Be("US:***; UK:***");
        }

        [Fact]
        public void Does_Not_Mask_Within_Words_Or_Ids() {
            var r = new PhoneRedactor();
            r.Redact("order-1234567A").Should().Be("order-1234567A"); // digits touching a word char boundary
            r.Redact("abc+1234567xyz").Should().Be("abc+1234567xyz"); // '+' in the middle of a word-like token
        }

        [Fact]
        public void Leaves_Extension_Text_And_Masks_Main_Number() {
            var r = new PhoneRedactor();
            var input = "+1 555 123 4567 ext. 89";
            r.Redact(input).Should().Be("*** ext. 89");
        }

        [Fact]
        public void Idempotent_When_Applied_Twice() {
            var r = new PhoneRedactor();
            var once = r.Redact("+1 (555) 012-3456");
            var twice = r.Redact(once);
            twice.Should().Be(once);
        }
    }
}
