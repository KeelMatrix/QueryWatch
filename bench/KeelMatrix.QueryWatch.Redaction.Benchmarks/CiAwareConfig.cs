using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;

namespace KeelMatrix.QueryWatch.Redaction.Benchmarks {
    internal sealed class CiAwareConfig : ManualConfig {
        public CiAwareConfig() {
            _ = AddLogger([.. DefaultConfig.Instance.GetLoggers()]);
            _ = AddColumnProvider([.. DefaultConfig.Instance.GetColumnProviders()]);
            _ = AddDiagnoser([.. DefaultConfig.Instance.GetDiagnosers()]);

            bool isCi = Environment.GetEnvironmentVariable("CI") is not null;
            if (!isCi) {
                _ = AddExporter(JsonExporter.FullCompressed);
            }
        }
    }
}
