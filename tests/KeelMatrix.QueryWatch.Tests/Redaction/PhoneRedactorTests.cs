using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class PhoneRedactorTests {
        [Fact]
        public void Masks_Common_Phone_Formats() {
            PhoneRedactor r = new();
            _ = r.Redact("+1 (555) 012-3456").Should().Be("***");
            _ = r.Redact("tel=+44 20 7946 0958").Should().Be("tel=***");
        }

        [Fact]
        public void Does_Not_Mask_Short_Numbers() {
            PhoneRedactor r = new();
            _ = r.Redact("code=123-45").Should().Be("code=123-45");
        }

        [Theory]
        [InlineData("(555) 123-4567")]
        [InlineData("555-123-4567")]
        [InlineData("555.123.4567")]
        [InlineData("+49-(030)-1234-5678")]
        [InlineData("+81 3 1234 5678")]
        public void Masks_International_And_Various_Separators(string number) {
            PhoneRedactor r = new();
            _ = r.Redact(number).Should().Be("***");
        }

        [Fact]
        public void Masks_Only_Phone_Leaving_Text_Intact() {
            PhoneRedactor r = new();
            const string input = "Call me at +1 650 555 0000 tomorrow.";
            _ = r.Redact(input).Should().Be("Call me at *** tomorrow.");
        }

        [Fact]
        public void Masks_Multiple_Phone_Numbers_In_Same_String() {
            PhoneRedactor r = new();
            const string input = "US:+1 650 555 0000; UK:+44 20 7946 0958";
            _ = r.Redact(input).Should().Be("US:***; UK:***");
        }

        [Fact]
        public void Does_Not_Mask_Within_Words_Or_Ids() {
            PhoneRedactor r = new();
            _ = r.Redact("order-1234567A").Should().Be("order-1234567A"); // digits touching a word char boundary
            _ = r.Redact("abc+1234567xyz").Should().Be("abc+1234567xyz"); // '+' in the middle of a word-like token
        }

        [Fact]
        public void Leaves_Extension_Text_And_Masks_Main_Number() {
            PhoneRedactor r = new();
            const string input = "+1 555 123 4567 ext. 89";
            _ = r.Redact(input).Should().Be("*** ext. 89");
        }

        [Fact]
        public void Idempotent_When_Applied_Twice() {
            PhoneRedactor r = new();
            string once = r.Redact("+1 (555) 012-3456");
            string twice = r.Redact(once);
            _ = twice.Should().Be(once);
        }
    }
}
