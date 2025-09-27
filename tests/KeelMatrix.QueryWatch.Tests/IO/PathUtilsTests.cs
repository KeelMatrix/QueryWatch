#nullable enable
using FluentAssertions;
using KeelMatrix.QueryWatch.IO;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.IO {
    public class PathUtilsTests {
        [Fact]
        public void Combine_SmokeTest_Equals_SystemCombine() {
            var expected = Path.Combine("a", "b", "c.txt");
            var actual = PathUtils.Combine("a", "b", "c.txt");
            actual.Should().Be(expected);
        }

        [Fact]
        public void Combine_With_Absolute_Base_Works() {
            var baseDir = Path.GetFullPath(".");
            var expected = Path.Combine(baseDir, "sub", "file.txt");
            var actual = PathUtils.Combine(baseDir, "sub", "file.txt");
            actual.Should().Be(expected);
        }
    }
}
