// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Telemetry {
    /// <summary>
    /// Central configuration for telemetry constants and limits.
    /// </summary>
    internal static class TelemetryConfig {
        public static readonly Uri Url =
            new("https://keelmatrix-nuget-telemetry.dz-bb6.workers.dev", UriKind.Absolute);

        public static string ToolVersion { get; }
            = typeof(TelemetryConfig).Assembly.GetName().Version?.ToString() ?? "unknown";

        public const int SchemaVersion = 1;
        public const int MaxPayloadBytes = 512;
        public const int RuntimeMaxLength = 32;
        public const int ToolVersionMaxLength = 16;
        public const int ProjectHashMaxLength = 64;
        public const int OsMaxLength = 16;
        public const int MaxDeadLetterItems = 400;
        public const int MaxPendingItems = 128;
        public const int MaxSendAttempts = 12;
        public const int ExpectedSaltBytes = 32;
        public const int MaxSaltFileBytes = 4 * 1024; // 4KB hard cap
        public const int MaxMarkerFiles = 1024;
        public static readonly TimeSpan ProcessingStaleThreshold = TimeSpan.FromMinutes(5);

        private const string ToolNameUpper = "QueryWatch";
        public static string ToolName { get; } = ToolNameUpper.ToLowerInvariant();

        private static readonly string RootDirectory = ResolveRootDirectory();
        private static int processDisabled; // 0/1

        internal static class ProjectIdentity {
            public const int MaxUpwardSteps = 32;
            public const int MaxConfigBytes = 512 * 1024;
            public const int MaxPackedRefsBytes = 512 * 1024;
            public const int MaxObjectBytesDecompressed = 512 * 1024;
            public const int MaxCommitParentTraversal = 256;
            public const int MaxFileBytes = 512 * 1024;
            public const int MaxTotalFiles = 7;
            public const int MaxProjectFiles = 3;
            public const int MaxRecursiveDirs = 128;
            public const int MaxRecursiveFiles = 1024;
            public static readonly char[] Separator = [';'];
        }

        public static string GetRootDirectory() => RootDirectory;

        public static void DisableTelemetryForCurrentProcess() {
            Interlocked.Exchange(ref processDisabled, 1);
        }

        private static string ResolveRootDirectory() {
            try {
                // 1) Preferred: LocalApplicationData (per-user, non-roaming).
                var local = SafeGetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (IsUsableAbsolutePath(local))
                    return Path.Combine(local, "KeelMatrix", ToolNameUpper);

                // 2) Fallback: ApplicationData (roaming). Still per-user and usually writable.
                var roaming = SafeGetFolderPath(Environment.SpecialFolder.ApplicationData);
                if (IsUsableAbsolutePath(roaming))
                    return Path.Combine(roaming, "KeelMatrix", ToolNameUpper);

                // 3) Fallback: UserProfile (cross-platform). Use ".local/share" on Unix-like.
                var userProfile = SafeGetFolderPath(Environment.SpecialFolder.UserProfile);
                if (IsUsableAbsolutePath(userProfile)) {
                    if (Path.DirectorySeparatorChar != '\\') {
                        // ~/.local/share/KeelMatrix/QueryWatch
                        return Path.Combine(userProfile, ".local", "share", "KeelMatrix", ToolNameUpper);
                    }

                    // Windows: keep it simple under user profile if nothing else is available.
                    return Path.Combine(userProfile, "AppData", "Local", "KeelMatrix", ToolNameUpper);
                }

                // 4) Last resort: temp (always absolute).
                var temp = Path.GetTempPath();
                if (IsUsableAbsolutePath(temp))
                    return Path.Combine(temp, "KeelMatrix", ToolNameUpper);
            }
            catch {
                // swallow and fall through to absolute temp fallback
            }

            // Absolute last line of defense: hard fallback to temp.
            return Path.Combine(Path.GetTempPath(), "KeelMatrix", ToolNameUpper);

            static string SafeGetFolderPath(Environment.SpecialFolder folder) {
                try { return Environment.GetFolderPath(folder) ?? string.Empty; }
                catch { return string.Empty; }
            }

            static bool IsUsableAbsolutePath(string path) {
                try {
                    if (string.IsNullOrWhiteSpace(path))
                        return false;

                    path = path.Trim();

                    // Must be rooted to avoid writing under CWD.
                    if (!Path.IsPathRooted(path))
                        return false;

                    // Normalize; will throw on malformed paths.
                    _ = Path.GetFullPath(path);
                    return true;
                }
                catch {
                    return false;
                }
            }
        }

        /// <summary>
        /// Determines whether telemetry is globally disabled via environment variables.
        /// Compatibility: honors common ecosystem opt-out variables in addition to the library-specific one.
        /// </summary>
        public static bool IsTelemetryDisabled() {
            // Process-local hard disable
            if (Volatile.Read(ref processDisabled) == 1)
                return true;

            // KeelMatrix opt-out
            if (IsOptOutSet("KEELMATRIX_NO_TELEMETRY"))
                return true;

            // Ecosystem-standard opt-outs
            if (IsOptOutSet("DOTNET_CLI_TELEMETRY_OPTOUT"))
                return true;

            if (IsOptOutSet("DO_NOT_TRACK"))
                return true;

            return false;
        }

        private static bool IsOptOutSet(string variableName) {
            try {
                var value = Environment.GetEnvironmentVariable(variableName);
                if (string.IsNullOrWhiteSpace(value))
                    return false;

                value = value.Trim();

                // Common truthy values used by tooling and CI environments
                return value == "1"
                    || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("y", StringComparison.OrdinalIgnoreCase)
                    || value.Equals("on", StringComparison.OrdinalIgnoreCase);
            }
            catch {
                return false;
            }
        }
    }
}
