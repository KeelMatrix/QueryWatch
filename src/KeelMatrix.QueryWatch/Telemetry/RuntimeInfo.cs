// Copyright (c) KeelMatrix

using System.Runtime.InteropServices;

namespace KeelMatrix.QueryWatch.Telemetry {
    internal static class RuntimeInfo {
        public static string Runtime { get; } = DetectRuntime();
        public static string Os { get; } = DetectOs();
        public static bool IsCi { get; } = DetectCi();

        private static string DetectRuntime() {
            try {
                return RuntimeInformation.FrameworkDescription switch {
                    string s when s.Contains(".NET", StringComparison.OrdinalIgnoreCase)
                        => NormalizeRuntimeString(s),
                    _ => "dotnet"
                };
            }
            catch {
                return "unknown";
            }
        }

        private static string DetectOs() {
            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    return "windows";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    return "linux";
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    return "osx";

                return "unknown";
            }
            catch {
                return "unknown";
            }
        }

        private static bool DetectCi() {
            // common CI indicators
            return
                HasEnv("CI") ||
                HasEnv("GITHUB_ACTIONS") ||
                HasEnv("TF_BUILD") ||
                HasEnv("BUILD_BUILDID") ||
                HasEnv("JENKINS_URL");
        }

        private static bool HasEnv(string name)
            => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name));

        private static string NormalizeRuntimeString(string value) {
#pragma warning disable IDE0057 // Use range operator
            return value.Length <= TelemetryConfig.RuntimeMaxLength
                ? value
                : value.Substring(0, TelemetryConfig.RuntimeMaxLength);
#pragma warning restore IDE0057
        }
    }
}
