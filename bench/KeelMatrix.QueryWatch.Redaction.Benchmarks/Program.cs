using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace KeelMatrix.QueryWatch.Redaction.Benchmarks {
    public static class Program {
        public static void Main(string[] args) =>
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new CiAwareConfig());
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
            // Email-only samples
            yield return "simple user@example.com";
            yield return "very.long.email.address_" + new string('x', 128) + "@domain.co.uk";

            // Phone-only
            yield return "+1-415-555-2671 is my number";
            yield return "call me at +44 20 7946 0958";

            // JWT-only
            yield return "jwt eyJhbGciOi" + new string('z', 2048);

            // Pure noise
            yield return "noise-no-pii-" + new string('x', 1024);
        }

    }
}
