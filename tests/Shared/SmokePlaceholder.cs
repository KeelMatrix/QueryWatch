using Xunit;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests {
    /// <summary>
    /// Ensures each test project has at least one discovered test for broad filters.
    /// Kept skipped locally to avoid “No test matches” noise.
    /// </summary>
    public static class SmokePlaceholder {
#pragma warning disable xUnit1004 // Test methods should not be skipped
        [Fact(Skip = "Local run: placeholder to suppress 'No test matches' warnings.")]
#pragma warning restore xUnit1004 // Test methods should not be skipped

#pragma warning disable S1186 // Methods should not be empty
#pragma warning disable IDE1006 // Naming Styles
        public static void __smoke_placeholder__() { }
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore S1186 // Methods should not be empty
    }
}
