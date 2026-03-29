// Copyright (c) KeelMatrix

using KeelMatrix.Telemetry;

namespace KeelMatrix.QueryWatch.Cli {
    internal static class QueryWatchCliTelemetry {
        private const string ActivationSentinelPathEnvVar = "QWATCH_CLI_TRACK_ACTIVATION_SENTINEL";
        private static readonly Client Client = new("qwatchCLI", typeof(Program));

        internal static void TrackActivation() {
            TryWriteActivationSentinel();
            Client.TrackActivation();
        }

        private static void TryWriteActivationSentinel() {
            try {
                string? path = Environment.GetEnvironmentVariable(ActivationSentinelPathEnvVar);
                if (string.IsNullOrWhiteSpace(path))
                    return;

                File.AppendAllText(path, "activated" + Environment.NewLine);
            }
            catch {
                // Test-only best effort; never block telemetry or CLI execution.
            }
        }
    }
}
