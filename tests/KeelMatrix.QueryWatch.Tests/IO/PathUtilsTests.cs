using FluentAssertions;
using KeelMatrix.QueryWatch.IO;
using Xunit;

namespace KeelMatrix.QueryWatch.Tests.IO {
    public class PathUtilsTests {
        [Fact]
        public void Combine_SmokeTest_Equals_SystemCombine() {
            string expected = Path.Combine("a", "b", "c.txt");
            string actual = PathUtils.Combine("a", "b", "c.txt");
            _ = actual.Should().Be(expected);
        }

        [Fact]
        public void Combine_With_Absolute_Base_Works() {
            string baseDir = Path.GetFullPath(".");
            string expected = Path.Combine(baseDir, "sub", "file.txt");
            string actual = PathUtils.Combine(baseDir, "sub", "file.txt");
            _ = actual.Should().Be(expected);
        }
    }
}
