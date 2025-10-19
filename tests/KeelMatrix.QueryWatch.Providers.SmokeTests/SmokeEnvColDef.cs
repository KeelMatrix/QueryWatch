using Xunit;

namespace KeelMatrix.QueryWatch.Providers.SmokeTests {
    [CollectionDefinition("SmokeEnv", DisableParallelization = true)]
    public sealed class SmokeEnvColDef : ICollectionFixture<SmokeEnv.Setup>;
}
