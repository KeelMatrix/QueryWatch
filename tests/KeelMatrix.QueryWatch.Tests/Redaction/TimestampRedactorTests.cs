using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class TimestampRedactorTests {
        [Fact]
        public void Masks_Iso8601_Timestamps_With_Or_Without_Fraction_And_Offset() {
            TimestampRedactor r = new();
            const string input = "2025-09-19T13:45:30Z and 2025-09-19T13:45:30.123+02:00";
            string red = r.Redact(input);
            _ = red.Should().Be("*** and ***");
        }

        [Fact]
        public void Masks_Unix_Seconds_10_11_Digits() {
            TimestampRedactor r = new();
            const string input = "epoch=1726750000 next=17267500001"; // 10 and 11 digits in expected range
            string red = r.Redact(input);
            _ = red.Should().NotContain("1726750000");
            _ = red.Should().NotContain("17267500001");
        }

        [Fact]
        public void Does_Not_Mask_Unix_Milliseconds_13_Digits() {
            TimestampRedactor r = new();
            const string input = "ts=1726750000000"; // 13 digits (ms), should not be treated as seconds
            _ = r.Redact(input).Should().Be(input);
        }

        [Fact]
        public void Does_Not_Mask_Too_Short_Unix_Seconds() {
            TimestampRedactor r = new();
            const string input = "ts=123456789"; // 9 digits
            _ = r.Redact(input).Should().Be(input);
        }
    }
}
