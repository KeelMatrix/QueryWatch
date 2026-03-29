// Copyright (c) KeelMatrix

namespace KeelMatrix.QueryWatch.Cli.Telemetry {
    // Modified copy of KeelMatrix.Telemetry ProjectIdentity/GitDiscovery repo-root walking,
    // narrowed to the current working directory for qwatch repo-scoped telemetry commands.
    internal static class RepoRootResolver {
        private const int MaxUpwardSteps = 32;

        internal static bool TryFindRepositoryRootFromCurrentDirectory(out string repositoryRoot) {
            repositoryRoot = string.Empty;

            string? current = SafeGetFullPath(Environment.CurrentDirectory);
            if (string.IsNullOrEmpty(current))
                return false;

            string? fallbackRepositoryRoot = null;

            for (int i = 0; i <= MaxUpwardSteps && !string.IsNullOrEmpty(current); i++) {
                if (HasGitRepositoryRoot(current)) {
                    repositoryRoot = current;
                    return true;
                }

                if (fallbackRepositoryRoot is null && LooksLikeNonGitRepositoryRoot(current))
                    fallbackRepositoryRoot = current;

                current = SafeGetParentDirectory(current);
            }

            if (!string.IsNullOrEmpty(fallbackRepositoryRoot)) {
                repositoryRoot = fallbackRepositoryRoot;
                return true;
            }

            return false;
        }

        private static bool HasGitRepositoryRoot(string dir) {
            try {
                string dotGitPath = Path.Combine(dir, ".git");
                return Directory.Exists(dotGitPath) || File.Exists(dotGitPath);
            }
            catch {
                return false;
            }
        }

        private static bool LooksLikeNonGitRepositoryRoot(string dir) {
            try {
                return File.Exists(Path.Combine(dir, "global.json"))
                    || File.Exists(Path.Combine(dir, "Directory.Build.props"))
                    || File.Exists(Path.Combine(dir, "Directory.Build.targets"))
                    || File.Exists(Path.Combine(dir, "NuGet.config"));
            }
            catch {
                return false;
            }
        }

        private static string SafeGetFullPath(string path) {
            try { return Path.GetFullPath(path); } catch { return string.Empty; }
        }

        private static string? SafeGetParentDirectory(string path) {
            try {
                return Directory.GetParent(path)?.FullName;
            }
            catch {
                return null;
            }
        }
    }
}
