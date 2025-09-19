using FluentAssertions;
using KeelMatrix.QueryWatch.Redaction;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.Redaction {
    public class GuidRedactorTests {
        [Fact]
        public void Masks_Dashed_Guids() {
            var r = new GuidRedactor();
            var g = "123e4567-e89b-12d3-a456-426614174000";
            var input = $"/* id={g} */";
            var red = r.Redact(input);
            red.Should().NotContain(g);
            red.Should().Contain("***");
        }

        [Fact]
        public void Leaves_NonGuid_Text_Alone() {
            var r = new GuidRedactor();
            var input = "SELECT '123e4567e89b12d3a456426614174000'"; // no dashes -> not masked by GuidRedactor
            r.Redact(input).Should().Be(input);
        }
    }
}
