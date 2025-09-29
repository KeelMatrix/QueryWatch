using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;

namespace KeelMatrix.QueryWatch.Redaction.Benchmarks {
    [MemoryDiagnoser]
    [Config(typeof(Config))]
    public class RedactionBench {
        private const string _sample = "Contact me at user@example.com or +1-415-555-2671. Token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";
        private readonly EmailRedactor _email = new();
        private readonly PhoneRedactor _phone = new();
        private readonly JwtTokenRedactor _jwt = new();

        [Benchmark(Baseline = true)]
        public string Baseline_Email() => _email.Redact(_sample);

        [Benchmark]
        public string Hardened_Email() => _email.Redact(_sample); // uses NonBacktracking + timeouts via factory

        [Benchmark]
        public string Phone() => _phone.Redact(_sample);

        [Benchmark]
        public string Jwt() => _jwt.Redact(_sample);

        public class Config : ManualConfig {
            public Config() {
                AddColumn(BenchmarkDotNet.Columns.StatisticColumn.P95);
            }
        }
    }

    public static class Program {
        public static void Main(string[] args) => BenchmarkRunner.Run<RedactionBench>();
    }
}
