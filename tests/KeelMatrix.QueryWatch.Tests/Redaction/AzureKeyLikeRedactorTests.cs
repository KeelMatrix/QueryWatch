using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class AzureKeyLikeRedactorTests {
        [Fact]
        public void Masks_AccountKey_And_SharedAccessKey() {
            var r = new AzureKeyLikeRedactor();
            var input = "AccountKey=abcDEF123;SharedAccessKey=XYZ;";
            var red = r.Redact(input);
            red.Should().Contain("AccountKey=***");
            red.Should().Contain("SharedAccessKey=***");
            red.Should().NotContain("abcDEF123");
            red.Should().NotContain("XYZ");
        }

        [Fact]
        public void Masks_SharedAccessSignature() {
            var r = new AzureKeyLikeRedactor();
            var input = "SharedAccessSignature=sv=2022-01-01&sig=abc";
            var red = r.Redact(input);
            red.Should().Contain("SharedAccessSignature=***");
        }
    }
}
