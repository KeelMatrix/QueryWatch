using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class GuidRedactorTests {
        [Fact]
        public void Masks_Canonical_Guids() {
            var r = new GuidRedactor();
            var g = "123e4567-e89b-12d3-a456-426614174000";
            r.Redact($"SELECT '{g}'").Should().NotContain(g).And.Contain("***");
        }

        [Fact]
        public void Leaves_Invalid_Shapes() {
            var r = new GuidRedactor();
            var txt = "123e4567e89b12d3a456426614174000"; // no dashes -> handled by UuidNoDashRedactor, not this one
            r.Redact(txt).Should().Be(txt);
        }
    }
}
