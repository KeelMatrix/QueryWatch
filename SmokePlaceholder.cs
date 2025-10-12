using KeelMatrix.QueryWatch.Providers.SmokeTests;

using Xunit;
namespace KeelMatrix.QueryWatch.Providers.SmokeTests
{
    // Ensures solution-root filtering finds at least one test in every project.
    public static class SmokePlaceholder
    {
        [Fact(Skip = "Local run: placeholder to suppress 'No test matches' warnings.")]
        public static void __smoke_placeholder__() { }
    }
}
