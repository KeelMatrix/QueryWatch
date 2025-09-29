using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace KeelMatrix.QueryWatch.Redaction.Benchmarks {
    public static class Program {
        public static void Main(string[] args) =>
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }

    [MemoryDiagnoser]
    public class RedactionBench {
        // Build inputs in code (attributes must be constants, so we use ParamsSource).
        [ParamsSource(nameof(SampleCases))]
        public string Sample { get; set; } = string.Empty;

        private readonly EmailRedactor _email = new();
        private readonly PhoneRedactor _phone = new();
        private readonly JwtTokenRedactor _jwt = new();

        // Baseline keeps historical comparability against existing runs.
        [Benchmark(Baseline = true)]
        public string Baseline_Email() => _email.Redact(Sample);

        [Benchmark]
        public string Hardened_Email() => _email.Redact(Sample);

        [Benchmark]
        public string Phone() => _phone.Redact(Sample);

        [Benchmark]
        public string Jwt() => _jwt.Redact(Sample);

        public static IEnumerable<string> SampleCases() {
            yield return "Contact me at user@example.com or +1-415-555-2671. Token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.";
            yield return "noise-no-pii-" + new string('x', 32); // ~32B filler
            yield return "email test a@b.co " + new string('y', 1024) + " +44 20 7946 0958"; // larger line with phone
            yield return "jwt " + new string('z', 4096) + " eyJhbGciOi"; // big line w/ jwt-like token
        }
    }
}
