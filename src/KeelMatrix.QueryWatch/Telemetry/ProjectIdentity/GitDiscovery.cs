// Copyright (c) KeelMatrix

using System.IO.Compression;
using System.Reflection;
using System.Text;

namespace KeelMatrix.QueryWatch.Telemetry.ProjectIdentity {
    internal static class GitDiscovery {
        public static IEnumerable<string> GetStartingPoints() {
            var seen = new HashSet<string>(Path.DirectorySeparatorChar == '\\' ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            foreach (var p in new[] {
                SafeGetCurrentDirectory(),
                SafeGetBaseDirectory(),
                SafeGetEntryAssemblyDirectory()
            }) {
                if (string.IsNullOrWhiteSpace(p))
                    continue;

                var full = SafeGetFullPath(p);
                if (string.IsNullOrWhiteSpace(full))
                    continue;

                if (seen.Add(full))
                    yield return full;
            }
        }

        public static bool TryFindGitDirectory(string startingPoint, out string gitDir) {
            gitDir = string.Empty;

            string? current = startingPoint;
            for (int i = 0; i <= TelemetryConfig.ProjectIdentity.MaxUpwardSteps && !string.IsNullOrEmpty(current); i++) {
                try {
                    var dotGitPath = Path.Combine(current, ".git");

                    if (Directory.Exists(dotGitPath)) {
                        gitDir = dotGitPath;
                        return true;
                    }

                    if (File.Exists(dotGitPath) &&
                        TryResolveGitDirFromFile(dotGitPath, out var resolvedGitDir)) {
                        gitDir = resolvedGitDir;
                        return true;
                    }
                }
                catch {
                    // swallow and keep walking up
                }

                current = SafeGetParentDirectory(current!);
            }

            return false;
        }

        public static bool TryReadOriginRemoteUrl(string gitDir, out string originUrl) {
            originUrl = string.Empty;

            var configPath = Path.Combine(gitDir, "config");
            if (!TryReadTextFileCapped(configPath, TelemetryConfig.ProjectIdentity.MaxConfigBytes, out var configText))
                return false;

            bool inOriginSection = false;

            using var reader = new StringReader(configText);
            string? line;
            while ((line = reader.ReadLine()) != null) {
                var trimmed = line.Trim();

#pragma warning disable CA1865 // Use char overload
                if (trimmed.Length == 0 || trimmed.StartsWith(";", StringComparison.Ordinal) || trimmed.StartsWith("#", StringComparison.Ordinal))
                    continue;

                if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal)) {
                    var section = trimmed.Substring(1, trimmed.Length - 2).Trim();
#pragma warning restore CA1865

                    // Exact target: remote "origin" (case-insensitive).
                    inOriginSection = section.Equals("remote \"origin\"", StringComparison.OrdinalIgnoreCase);
                    continue;
                }

                if (!inOriginSection)
                    continue;

                // Look for: url = ...
                int eq = trimmed.IndexOf('=');
                if (eq <= 0)
                    continue;

                var key = trimmed.Substring(0, eq).Trim();
                if (!key.Equals("url", StringComparison.OrdinalIgnoreCase))
                    continue;

                var value = trimmed.Substring(eq + 1).Trim();
                if (value.Length == 0)
                    continue;

                // Remove optional surrounding quotes.
                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"') {
                    value = value.Substring(1, value.Length - 2).Trim();
                }

                if (value.Length == 0)
                    continue;

                originUrl = value;
                return true;
            }

            return false;
        }

        public static bool TryComputeRootCommitHashBestEffort(string gitDir, out string rootCommitHashLowerAscii) {
            rootCommitHashLowerAscii = string.Empty;

            // Resolve current commit hash (HEAD -> ref file or packed-refs).
            if (!TryReadHeadCommitHash(gitDir, out var headHashLower))
                return false;

            // Traverse commit parents to find root commit hash.
            // Best-effort: only supports loose objects. If objects are packed or parsing fails, return false.
            string current = headHashLower;
            for (int i = 0; i < TelemetryConfig.ProjectIdentity.MaxCommitParentTraversal; i++) {
                if (!TryReadLooseCommitObject(gitDir, current, out var commitText))
                    return false;

                if (!TryGetFirstParentHash(commitText, out var parentHashLower)) {
                    // No parents => root commit.
                    rootCommitHashLowerAscii = current;
                    return true;
                }

                current = parentHashLower;
            }

            return false;
        }

        private static bool TryReadHeadCommitHash(string gitDir, out string commitHashLower) {
            commitHashLower = string.Empty;

            var headPath = Path.Combine(gitDir, "HEAD");
            if (!TryReadTextFileCapped(headPath, 16 * 1024, out var headText))
                return false;

            var head = headText.Trim();
            if (head.Length == 0)
                return false;

            // HEAD: "ref: refs/heads/main"
            const string refPrefix = "ref:";
            if (head.StartsWith(refPrefix, StringComparison.OrdinalIgnoreCase)) {
                var refName = head.Substring(refPrefix.Length).Trim();
                if (refName.Length == 0)
                    return false;

                // refs file
                var refPath = Path.Combine(gitDir, refName.Replace('/', Path.DirectorySeparatorChar));
                if (TryReadTextFileCapped(refPath, 16 * 1024, out var refText)) {
                    var hash = refText.Trim();
                    if (TryNormalize40Hex(hash, out commitHashLower))
                        return true;
                }

                // packed-refs
                var packedRefsPath = Path.Combine(gitDir, "packed-refs");
                if (!TryReadTextFileCapped(packedRefsPath, TelemetryConfig.ProjectIdentity.MaxPackedRefsBytes, out var packedRefsText))
                    return false;

                using var reader = new StringReader(packedRefsText);
                string? line;
                while ((line = reader.ReadLine()) != null) {
                    if (line.Length == 0)
                        continue;

                    if (line[0] == '#' || line[0] == '^')
                        continue;

                    // "<hash> <ref>"
                    int space = line.IndexOf(' ');
                    if (space <= 0 || space == line.Length - 1)
                        continue;

                    var hashPart = line.Substring(0, space).Trim();
                    var refPart = line.Substring(space + 1).Trim();

                    if (!refPart.Equals(refName, StringComparison.Ordinal))
                        continue;

                    return TryNormalize40Hex(hashPart, out commitHashLower);
                }

                return false;
            }

            // Detached head: "<hash>"
            return TryNormalize40Hex(head, out commitHashLower);
        }

        private static bool TryReadLooseCommitObject(string gitDir, string commitHashLower, out string commitText) {
            commitText = string.Empty;

            if (!TryNormalize40Hex(commitHashLower, out var hash))
                return false;

            var objPath = Path.Combine(gitDir, "objects", hash.Substring(0, 2), hash.Substring(2));
            if (!File.Exists(objPath)) {
                // Likely packed objects; not supported in best-effort.
                return false;
            }

            try {
                using var fs = new FileStream(objPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var ds = new DeflateStream(fs, CompressionMode.Decompress);

                if (!TryReadAllBytesCapped(ds, TelemetryConfig.ProjectIdentity.MaxObjectBytesDecompressed, out var decompressed))
                    return false;

                // Format: "commit <size>\0<content>"
                int nul = Array.IndexOf(decompressed, (byte)0);
                if (nul <= 0 || nul >= decompressed.Length - 1)
                    return false;

                // Validate type prefix starts with "commit ".
                // (Avoid parsing trees/blobs incorrectly.)
                var header = Encoding.ASCII.GetString(decompressed, 0, nul);
                if (!header.StartsWith("commit ", StringComparison.Ordinal))
                    return false;

                commitText = Encoding.UTF8.GetString(decompressed, nul + 1, decompressed.Length - (nul + 1));
                return commitText.Length > 0;
            }
            catch {
                return false;
            }
        }

        private static bool TryGetFirstParentHash(string commitText, out string parentHashLower) {
            parentHashLower = string.Empty;

            // Headers are lines until a blank line.
            using var reader = new StringReader(commitText);
            string? line;
            while ((line = reader.ReadLine()) != null) {
                if (line.Length == 0)
                    break;

                if (!line.StartsWith("parent ", StringComparison.Ordinal))
                    continue;

                var hash = line.Substring("parent ".Length).Trim();
                return TryNormalize40Hex(hash, out parentHashLower);
            }

            return false;
        }

        private static bool TryResolveGitDirFromFile(string dotGitFilePath, out string gitDir) {
            gitDir = string.Empty;

            if (!TryReadTextFileCapped(dotGitFilePath, 16 * 1024, out var text))
                return false;

            // Expected: "gitdir: <path>"
            var trimmed = text.Trim();
            const string prefix = "gitdir:";
            if (!trimmed.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return false;

            var pathPart = trimmed.Substring(prefix.Length).Trim();
            if (pathPart.Length == 0)
                return false;

            // Relative paths are relative to the working directory containing the .git file.
            try {
                var baseDir = Path.GetDirectoryName(dotGitFilePath) ?? string.Empty;
                string resolved = Path.IsPathRooted(pathPart)
                    ? pathPart
                    : Path.GetFullPath(Path.Combine(baseDir, pathPart));

                if (Directory.Exists(resolved)) {
                    gitDir = resolved;
                    return true;
                }

                return false;
            }
            catch {
                return false;
            }
        }

        private static bool TryReadTextFileCapped(string path, int maxBytes, out string text) {
            text = string.Empty;

            try {
                var fi = new FileInfo(path);
                if (!fi.Exists)
                    return false;

                if (fi.Length <= 0 || fi.Length > maxBytes)
                    return false;

                text = File.ReadAllText(path);
                return text.Length > 0;
            }
            catch {
                return false;
            }
        }

        private static bool TryReadAllBytesCapped(Stream stream, int maxBytes, out byte[] bytes) {
            bytes = [];

            try {
                using var ms = new MemoryStream();
                var buffer = new byte[16 * 1024];

                int total = 0;
                while (true) {
                    int read = stream.Read(buffer, 0, buffer.Length);
                    if (read <= 0)
                        break;

                    total += read;
                    if (total > maxBytes)
                        return false;

                    ms.Write(buffer, 0, read);
                }

                bytes = ms.ToArray();
                return bytes.Length > 0;
            }
            catch {
                return false;
            }
        }

        private static bool TryNormalize40Hex(string value, out string lower) {
            lower = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
                return false;

            value = value.Trim();
            if (value.Length != 40)
                return false;

            for (int i = 0; i < 40; i++) {
                if (HexValue(value[i]) < 0)
                    return false;
            }

            lower = value.ToLowerInvariant();
            return true;
        }

        public static int HexValue(char c) {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }

        private static string SafeGetCurrentDirectory() {
            try { return Environment.CurrentDirectory; } catch { return string.Empty; }
        }

        private static string SafeGetBaseDirectory() {
            try { return AppContext.BaseDirectory; } catch { return string.Empty; }
        }

        private static string SafeGetEntryAssemblyDirectory() {
            try {
                var loc = Assembly.GetEntryAssembly()?.Location;
                if (string.IsNullOrWhiteSpace(loc))
                    return string.Empty;

                var dir = Path.GetDirectoryName(loc);
                return dir ?? string.Empty;
            }
            catch {
                return string.Empty;
            }
        }

        private static string SafeGetFullPath(string path) {
            try { return Path.GetFullPath(path); } catch { return string.Empty; }
        }

        private static string? SafeGetParentDirectory(string path) {
            try {
                var parent = Directory.GetParent(path);
                return parent?.FullName;
            }
            catch {
                return null;
            }
        }
    }
}
