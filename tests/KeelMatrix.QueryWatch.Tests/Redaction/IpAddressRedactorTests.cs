using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class IpAddressRedactorTests {
        [Fact]
        public void Masks_Ipv4_Addresses() {
            var r = new IpAddressRedactor();
            var input = "client=192.168.1.10; SELECT 1;";
            var red = r.Redact(input);
            red.Should().NotContain("192.168.1.10");
            red.Should().Contain("***");
        }

        [Fact]
        public void Masks_Ipv6_Addresses_Conservative() {
            var r = new IpAddressRedactor();
            var input = "ip=2001:0db8:85a3:0000:0000:8a2e:0370:7334";
            var red = r.Redact(input);
            red.Should().NotContain("2001:0db8:85a3:0000:0000:8a2e:0370:7334");
            red.Should().Contain("***");
        }
    }
}
