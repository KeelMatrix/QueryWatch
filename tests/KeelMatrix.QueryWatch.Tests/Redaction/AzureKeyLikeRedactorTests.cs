using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class AzureKeyLikeRedactorTests {
        [Fact]
        public void Masks_AccountKey_And_SharedAccessKey() {
            var r = new AzureKeyLikeRedactor();
            var input = "AccountKey=abcDEF123;SharedAccessKey=XYZ;Other=ok;";
            var red = r.Redact(input);
            red.Should().Contain("AccountKey=***");
            red.Should().Contain("SharedAccessKey=***");
            red.Should().Contain("Other=ok");
            red.Should().NotContain("abcDEF123").And.NotContain("XYZ");
        }

        [Fact]
        public void Masks_SharedAccessSignature_Case_Insensitive() {
            var r = new AzureKeyLikeRedactor();
            var input = "sharedaccesssignature=sv=2022-01-01&sig=abc";
            r.Redact(input).Should().Be("SharedAccessSignature=***");
        }
    }
}
