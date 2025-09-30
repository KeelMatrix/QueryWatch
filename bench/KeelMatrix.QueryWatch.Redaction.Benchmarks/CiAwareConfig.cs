using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters.Json;

namespace KeelMatrix.QueryWatch.Redaction.Benchmarks {
    internal sealed class CiAwareConfig : ManualConfig {
        public CiAwareConfig() {
            AddLogger([.. DefaultConfig.Instance.GetLoggers()]);
            AddColumnProvider([.. DefaultConfig.Instance.GetColumnProviders()]);
            AddDiagnoser([.. DefaultConfig.Instance.GetDiagnosers()]);

            bool isCi = Environment.GetEnvironmentVariable("CI") is not null;
            if (!isCi) {
                AddExporter(JsonExporter.FullCompressed);
            }
        }
    }
}
