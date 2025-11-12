using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class AzureKeyLikeRedactorTests {
        [Fact]
        public void Masks_AccountKey_And_SharedAccessKey() {
            AzureKeyLikeRedactor r = new();
            const string input = "AccountKey=abcDEF123;SharedAccessKey=XYZ;Other=ok;";
            string red = r.Redact(input);
            _ = red.Should().Contain("AccountKey=***");
            _ = red.Should().Contain("SharedAccessKey=***");
            _ = red.Should().Contain("Other=ok");
            _ = red.Should().NotContain("abcDEF123").And.NotContain("XYZ");
        }

        [Fact]
        public void Masks_SharedAccessSignature_Case_Insensitive() {
            AzureKeyLikeRedactor r = new();
            const string input = "sharedaccesssignature=sv=2022-01-01&sig=abc";
            _ = r.Redact(input).Should().Be("SharedAccessSignature=***");
        }
    }
}
