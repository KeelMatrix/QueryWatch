using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class IpAddressRedactorTests {
        [Fact]
        public void Masks_IPv4() {
            var r = new IpAddressRedactor();
            r.Redact("client=192.168.1.10").Should().Be("client=***");
        }

        [Fact]
        public void Masks_IPv6_Generic() {
            var r = new IpAddressRedactor();
            r.Redact("addr=2001:0db8:85a3:0000:0000:8a2e:0370:7334").Should().Be("addr=***");
        }

        [Fact]
        public void Leaves_Non_IP_Text() {
            var r = new IpAddressRedactor();
            r.Redact("x:y").Should().Be("x:y");
        }

        [Fact]
        public void Masks_IPv6_Compressed_Forms() {
            var r = new IpAddressRedactor();
            r.Redact("addr=::1").Should().Be("addr=***");
            r.Redact("addr=2001:db8::7334").Should().Be("addr=***");
        }

        [Fact]
        public void Masks_IPv4_With_Port_Keeping_Port() {
            var r = new IpAddressRedactor();
            r.Redact("host=192.168.0.1:8080").Should().Be("host=***:8080");
        }
    }
}
