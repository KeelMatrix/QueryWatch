// Copyright (c) KeelMatrix

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;

namespace KeelMatrix.QueryWatch.Telemetry.ProjectIdentity {
    internal static class ProjectFileIdentityFingerprint {
        public static bool TryComputeIdentityFingerprintFromProjectFiles(out byte[] fingerprintBytes) {
            fingerprintBytes = [];

            foreach (var start in GitDiscovery.GetStartingPoints()) {
                if (!TrySelectPrimaryIdentityFile(start, out var identityRoot, out var primaryPath, out var primaryRole))
                    continue;

                if (TryComputeFingerprintForRoot(identityRoot, primaryPath, primaryRole, out fingerprintBytes))
                    return true;
            }

            fingerprintBytes = [];
            return false;
        }

        private static bool TrySelectPrimaryIdentityFile(
            string startingPoint,
            out string identityRoot,
            out string primaryPath,
            out string primaryRole) {

            identityRoot = string.Empty;
            primaryPath = string.Empty;
            primaryRole = string.Empty;

            if (string.IsNullOrWhiteSpace(startingPoint))
                return false;

            var slnCandidates = new List<Candidate>();
            var projCandidates = new List<Candidate>();

            string? current = SafeGetFullPath(startingPoint);

            string highestVisited = string.Empty;
            string markerRoot = string.Empty;

            for (int step = 0;
                 step <= TelemetryConfig.ProjectIdentity.MaxUpwardSteps && !string.IsNullOrEmpty(current);
                 step++) {

                highestVisited = current!;

                if (string.IsNullOrEmpty(markerRoot) && LooksLikeRepoRootMarker(current!))
                    markerRoot = current!;

                TryCollectCandidatesInDirectory(current!, step, slnCandidates, projCandidates);
                current = SafeGetParentDirectory(current!);
            }

            if (slnCandidates.Count > 0) {
                var chosen = slnCandidates
                    .OrderBy(c => c.Step)
                    .ThenBy(c => c.FullPath, StringComparer.Ordinal)
                    .First();

                primaryPath = chosen.FullPath;
                primaryRole = "sln";

                var primaryDir = SafeGetFullPath(Path.GetDirectoryName(primaryPath) ?? string.Empty);
                if (!string.IsNullOrEmpty(markerRoot)) {
                    identityRoot = markerRoot;
                }
                else if (!string.IsNullOrEmpty(highestVisited)) {
                    identityRoot = highestVisited;
                }
                else {
                    identityRoot = primaryDir;
                }

                return !string.IsNullOrEmpty(identityRoot) && !string.IsNullOrEmpty(primaryPath);
            }

            if (projCandidates.Count > 0) {
                var chosen = projCandidates
                    .OrderBy(c => c.Step)
                    .ThenBy(c => c.FullPath, StringComparer.Ordinal)
                    .First();

                primaryPath = chosen.FullPath;
                primaryRole = "proj";

                var primaryDir = SafeGetFullPath(Path.GetDirectoryName(primaryPath) ?? string.Empty);
                if (!string.IsNullOrEmpty(markerRoot)) {
                    identityRoot = markerRoot;
                }
                else if (!string.IsNullOrEmpty(highestVisited)) {
                    identityRoot = highestVisited;
                }
                else {
                    identityRoot = primaryDir;
                }

                return !string.IsNullOrEmpty(identityRoot) && !string.IsNullOrEmpty(primaryPath);
            }

            return false;
        }

        private static bool LooksLikeRepoRootMarker(string dir) {
            try {
                if (Directory.Exists(Path.Combine(dir, ".git")))
                    return true;

                // Common repo-level markers in .NET repos/monorepos.
                if (File.Exists(Path.Combine(dir, "global.json")))
                    return true;
                if (File.Exists(Path.Combine(dir, "Directory.Build.props")))
                    return true;
                if (File.Exists(Path.Combine(dir, "Directory.Build.targets")))
                    return true;
                if (File.Exists(Path.Combine(dir, "NuGet.config")))
                    return true;

                return false;
            }
            catch {
                return false;
            }
        }

        private static void TryCollectCandidatesInDirectory(
            string dir,
            int step,
            List<Candidate> slnCandidates,
            List<Candidate> projCandidates) {
            try {
#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
                foreach (var sln in SafeEnumerateFiles(dir, "*.sln")) {
                    if (!string.IsNullOrEmpty(sln))
                        slnCandidates.Add(new Candidate(step, SafeGetFullPath(sln)));
                }
#pragma warning restore S3267

                foreach (var p in SafeEnumerateFiles(dir, "*.csproj"))
                    projCandidates.Add(new Candidate(step, SafeGetFullPath(p)));

                foreach (var p in SafeEnumerateFiles(dir, "*.fsproj"))
                    projCandidates.Add(new Candidate(step, SafeGetFullPath(p)));

                foreach (var p in SafeEnumerateFiles(dir, "*.vbproj"))
                    projCandidates.Add(new Candidate(step, SafeGetFullPath(p)));
            }
            catch {
                // swallow
            }
        }

        private static bool TryComputeFingerprintForRoot(
            string identityRoot,
            string primaryPath,
            string primaryRole,
            out byte[] fingerprintBytes) {
            fingerprintBytes = [];

            identityRoot = SafeGetFullPath(identityRoot);
            primaryPath = SafeGetFullPath(primaryPath);

            if (string.IsNullOrEmpty(identityRoot) || string.IsNullOrEmpty(primaryPath))
                return false;

            // On Windows paths are case-insensitive; on Unix they are case-sensitive.
            var pathComparer = Path.DirectorySeparatorChar == '\\'
                ? StringComparer.OrdinalIgnoreCase
                : StringComparer.Ordinal;

            bool PathsEqual(string a, string b) => pathComparer.Equals(a, b);

            var items = new List<FileItem>(TelemetryConfig.ProjectIdentity.MaxTotalFiles) {
		        // Primary is required.
		        new(primaryRole, primaryPath)
            };

            // If primary is sln, include up to 3 project files under identityRoot.
            if (string.Equals(primaryRole, "sln", StringComparison.Ordinal)) {
                var projects = FindProjectFilesUnderIdentityRoot(identityRoot);
#pragma warning disable S3267 // Loops should be simplified with "LINQ" expressions
                foreach (var p in projects.Take(TelemetryConfig.ProjectIdentity.MaxProjectFiles)) {
                    var pFull = SafeGetFullPath(p);

                    // Avoid duplicating primary. Comparison must respect OS path semantics.
                    if (!string.IsNullOrEmpty(pFull) && !PathsEqual(pFull, primaryPath)) {
                        items.Add(new FileItem("proj", p));
                        if (items.Count >= 1 + TelemetryConfig.ProjectIdentity.MaxProjectFiles)
                            break;
                    }
                }
#pragma warning restore S3267
            }

            // Include build config files (first found walking upward from primary dir up to identityRoot).
            TryAddConfigFiles(identityRoot, Path.GetDirectoryName(primaryPath) ?? identityRoot, items);

            // Enforce hard cap (should already be within bounds).
            if (items.Count > TelemetryConfig.ProjectIdentity.MaxTotalFiles)
                items = [.. items.Take(TelemetryConfig.ProjectIdentity.MaxTotalFiles)];

            // Compute per-file digests (primary must succeed).
            var digests = new List<FileDigest>(items.Count);

            foreach (var item in items) {
                if (!TryComputeFileDigest(identityRoot, item, out var digest)) {
                    // Primary unusable => identity unavailable for this starting point.
                    if (PathsEqual(item.Path, primaryPath))
                        return false;

                    continue; // optional file; skip
                }

                digests.Add(digest);
            }

            // Must have primary digest.
            if (digests.Count == 0)
                return false;

            // Sort by (role, relativePath) ordinal.
            digests.Sort((a, b) => {
                int r = StringComparer.Ordinal.Compare(a.Role, b.Role);
                if (r != 0) return r;
                return StringComparer.Ordinal.Compare(a.RelativePath, b.RelativePath);
            });

            // IdentityFingerprintBytes = SHA256( "content.v1" || "\0" || concat(fileDigestBytes...) )
            var prefix = Encoding.UTF8.GetBytes("content.v1");
            var concatenated = ConcatDigests(prefix, digests);
            fingerprintBytes = Sha256(concatenated);
            return true;
        }

        private static List<string> FindProjectFilesUnderIdentityRoot(string identityRoot) {
            // Non-recursive first.
            var nonRecursive = new List<string>();
            foreach (var ext in new[] { "*.csproj", "*.fsproj", "*.vbproj" }) {
                nonRecursive.AddRange(SafeEnumerateFiles(identityRoot, ext)
                    .Select(SafeGetFullPath)
                    .Where(p => !string.IsNullOrEmpty(p)));
            }

            if (nonRecursive.Count > 0) {
                return [.. nonRecursive
                    .Select(p => new { Full = p, Rel = GetRelativePath(identityRoot, p) })
                    .OrderBy(x => x.Rel, StringComparer.Ordinal)
                    .Select(x => x.Full)];
            }

            // Recursive (deterministic order; bounded).
            var recursive = new List<string>();
            CollectProjectFilesRecursiveDeterministic(identityRoot, recursive);

            return [.. recursive
                .Select(p => new { Full = p, Rel = GetRelativePath(identityRoot, p) })
                .OrderBy(x => x.Rel, StringComparer.Ordinal)
                .Select(x => x.Full)];
        }

        private static void CollectProjectFilesRecursiveDeterministic(string root, List<string> results) {
            var queue = new Queue<string>();
            queue.Enqueue(root);

            int dirsVisited = 0;

            while (queue.Count > 0) {
                if (dirsVisited++ > TelemetryConfig.ProjectIdentity.MaxRecursiveDirs)
                    return;

                var dir = queue.Dequeue();

                // Project files (sorted).
                var files = new List<string>();
                foreach (var ext in new[] { "*.csproj", "*.fsproj", "*.vbproj" }) {
                    files.AddRange(SafeEnumerateFiles(dir, ext));
                }

                files.Sort(StringComparer.Ordinal);

                foreach (var f in files) {
                    var full = SafeGetFullPath(f);
                    if (string.IsNullOrEmpty(full))
                        continue;

                    results.Add(full);
                    if (results.Count >= TelemetryConfig.ProjectIdentity.MaxRecursiveFiles)
                        return;
                }

                // Subdirectories (sorted).
                var subdirs = SafeEnumerateDirectories(dir).ToList();
                subdirs.Sort(StringComparer.Ordinal);

                foreach (var sd in subdirs) {
                    if (results.Count >= TelemetryConfig.ProjectIdentity.MaxRecursiveFiles)
                        return;

                    var full = SafeGetFullPath(sd);
                    if (!string.IsNullOrEmpty(full))
                        queue.Enqueue(full);
                }
            }
        }

        private static void TryAddConfigFiles(string identityRoot, string startDir, List<FileItem> items) {
            // Deterministic order of file names and first-hit behavior while walking upward.
            var wantProps = true;
            var wantTargets = true;
            var wantGlobalJson = true;

            string? current = SafeGetFullPath(startDir);
            string root = SafeGetFullPath(identityRoot);

            for (int step = 0; step <= TelemetryConfig.ProjectIdentity.MaxUpwardSteps && !string.IsNullOrEmpty(current); step++) {
                if (wantProps)
                    TryAddIfExists(items, "dirprops", Path.Combine(current!, "Directory.Build.props"), ref wantProps);

                if (wantTargets)
                    TryAddIfExists(items, "dirtargets", Path.Combine(current!, "Directory.Build.targets"), ref wantTargets);

                if (wantGlobalJson)
                    TryAddIfExists(items, "globaljson", Path.Combine(current!, "global.json"), ref wantGlobalJson);

                // Stop once we've reached identityRoot (inclusive).
                if (PathsEqual(current!, root))
                    break;

                current = SafeGetParentDirectory(current!);
                if (string.IsNullOrEmpty(current))
                    break;
            }

            static void TryAddIfExists(List<FileItem> itemsLocal, string role, string path, ref bool stillWanted) {
                if (!stillWanted)
                    return;

                try {
                    if (File.Exists(path)) {
                        itemsLocal.Add(new FileItem(role, path));
                        stillWanted = false;
                    }
                }
                catch {
                    // swallow
                }
            }
        }

        private static bool TryComputeFileDigest(string identityRoot, FileItem item, out FileDigest digest) {
            digest = default;

            if (!TryReadFileBytesCapped(item.Path, TelemetryConfig.ProjectIdentity.MaxFileBytes, out var rawBytes))
                return false;

            string rel = GetRelativePath(identityRoot, item.Path);
            if (string.IsNullOrEmpty(rel))
                rel = Path.GetFileName(item.Path);

            var canonical = Canonicalize(item, rawBytes);

#pragma warning disable S125 // Sections of code should not be commented out
            // fileDigest = SHA256( "file.v1" || "\0" || role || "\0" || relativePath || "\0" || canonicalContentBytes )
#pragma warning restore S125

            var prefix = Encoding.UTF8.GetBytes("file.v1");
            var roleBytes = Encoding.UTF8.GetBytes(item.Role);
            var relBytes = Encoding.UTF8.GetBytes(rel);

            int total = prefix.Length + 1 + roleBytes.Length + 1 + relBytes.Length + 1 + canonical.Length;
            var buffer = new byte[total];

            int o = 0;
            Buffer.BlockCopy(prefix, 0, buffer, o, prefix.Length);
            o += prefix.Length;
            buffer[o++] = 0;

            Buffer.BlockCopy(roleBytes, 0, buffer, o, roleBytes.Length);
            o += roleBytes.Length;
            buffer[o++] = 0;

            Buffer.BlockCopy(relBytes, 0, buffer, o, relBytes.Length);
            o += relBytes.Length;
            buffer[o++] = 0;

            Buffer.BlockCopy(canonical, 0, buffer, o, canonical.Length);

            var hash = Sha256(buffer);
            digest = new FileDigest(item.Role, rel, hash);
            return true;
        }

        private static byte[] Canonicalize(FileItem item, byte[] rawBytes) {
            var path = item.Path;
            var ext = SafeGetExtensionLower(path);

            if (string.Equals(ext, ".sln", StringComparison.Ordinal)) {
                return CanonicalizeSln(rawBytes);
            }

            if (string.Equals(ext, ".csproj", StringComparison.Ordinal) ||
                string.Equals(ext, ".fsproj", StringComparison.Ordinal) ||
                string.Equals(ext, ".vbproj", StringComparison.Ordinal) ||
                string.Equals(ext, ".props", StringComparison.Ordinal) ||
                string.Equals(ext, ".targets", StringComparison.Ordinal)) {
                return CanonicalizeMsbuild(rawBytes);
            }

            if (string.Equals(Path.GetFileName(path), "global.json", StringComparison.OrdinalIgnoreCase)) {
                return CanonicalizeGlobalJson(rawBytes);
            }

            // Fallback: general normalization bytes.
            return GeneralNormalizeToUtf8Bytes(rawBytes);
        }

        private static byte[] CanonicalizeSln(byte[] rawBytes) {
            var normalizedText = GeneralNormalizeToString(rawBytes);

            try {
                var projects = new List<SlnProject>();
                var solCfgLines = new List<string>();
                var projCfgLines = new List<string>();

                bool inSolCfg = false;
                bool inProjCfg = false;

                using (var reader = new StringReader(normalizedText)) {
                    string? line;
                    while ((line = reader.ReadLine()) != null) {
                        var trimmed = line.Trim();
                        if (trimmed.Length == 0)
                            continue;

#pragma warning disable CA1865 // Use char overload
                        if (trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.StartsWith(";", StringComparison.Ordinal))
                            continue;
#pragma warning restore CA1865

                        // Project(...) entries
                        if (trimmed.StartsWith("Project(", StringComparison.Ordinal)) {
                            if (TryParseSlnProjectLine(trimmed, out var proj))
                                projects.Add(proj);
                            continue;
                        }

                        // Global sections capture (minimal)
                        if (trimmed.StartsWith("GlobalSection(", StringComparison.Ordinal)) {
                            inSolCfg = trimmed.IndexOf("SolutionConfigurationPlatforms", StringComparison.OrdinalIgnoreCase) >= 0;
                            inProjCfg = trimmed.IndexOf("ProjectConfigurationPlatforms", StringComparison.OrdinalIgnoreCase) >= 0;
                            continue;
                        }

                        if (trimmed.Equals("EndGlobalSection", StringComparison.OrdinalIgnoreCase)) {
                            inSolCfg = false;
                            inProjCfg = false;
                            continue;
                        }

                        if (inSolCfg) {
                            solCfgLines.Add(NormalizeWhitespace(trimmed));
                            continue;
                        }

                        if (inProjCfg) {
                            projCfgLines.Add(NormalizeWhitespace(trimmed));
                            // continue
                        }
                    }
                }

                projects = [.. projects.OrderBy(p => p.ProjectGuid, StringComparer.Ordinal)];

                solCfgLines = [.. solCfgLines
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(s => s, StringComparer.Ordinal)];

                projCfgLines = [.. projCfgLines
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(s => s, StringComparer.Ordinal)];

                var sb = new StringBuilder();
                sb.Append("sln.v1\n");

                foreach (var p in projects) {
                    sb.Append("project ");
                    sb.Append(p.ProjectGuid);
                    sb.Append(' ');
                    sb.Append(p.TypeGuid);
                    sb.Append(' ');
                    sb.Append(p.Name);
                    sb.Append(" @path\n");
                }

                if (solCfgLines.Count > 0) {
                    sb.Append("SolutionConfigurationPlatforms\n");
                    foreach (var l in solCfgLines)
                        sb.Append(l).Append('\n');
                }

                if (projCfgLines.Count > 0) {
                    sb.Append("ProjectConfigurationPlatforms\n");
                    foreach (var l in projCfgLines)
                        sb.Append(l).Append('\n');
                }

                EnsureSingleTrailingNewline(sb);
                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            catch {
                // Parsing failed => fall back to general normalization bytes.
                return Encoding.UTF8.GetBytes(normalizedText);
            }
        }

        private static bool TryParseSlnProjectLine(string line, out SlnProject project) {
            project = default;

            // Expect quoted tokens: [typeGuid, name, path, projectGuid]
            if (!TryExtractQuotedTokens(line, out var tokens) || tokens.Count < 4)
                return false;

            var typeGuid = NormalizeGuidToken(tokens[0]);
            var name = NormalizeWhitespace(tokens[1]);
            var projectGuid = NormalizeGuidToken(tokens[tokens.Count - 1]);

            if (typeGuid.Length == 0 || projectGuid.Length == 0 || name.Length == 0)
                return false;

            project = new SlnProject(typeGuid, projectGuid, name);
            return true;
        }

        private static bool TryExtractQuotedTokens(string line, out List<string> tokens) {
            tokens = [];

            int i = 0;
            while (i < line.Length) {
                int start = line.IndexOf('"', i);
                if (start < 0 || start >= line.Length - 1)
                    break;

                int end = line.IndexOf('"', start + 1);
                if (end < 0)
                    break;

                tokens.Add(line.Substring(start + 1, end - start - 1));
                i = end + 1;
            }

            return tokens.Count > 0;
        }

        private static string NormalizeGuidToken(string token) {
            token = (token ?? string.Empty).Trim();
            if (token.Length == 0)
                return string.Empty;

            // Accept "{...}" or plain guid.
            var t = token;
#pragma warning disable CA1865 // Use char overload
            if (t.StartsWith("{", StringComparison.Ordinal) && t.EndsWith("}", StringComparison.Ordinal) && t.Length > 2)
                t = t.Substring(1, t.Length - 2);
#pragma warning restore CA1865

            if (Guid.TryParse(t, out var g))
                return g.ToString("B").ToLowerInvariant(); // "{xxxxxxxx-....}"

            return token.ToLowerInvariant();
        }

        private static byte[] CanonicalizeMsbuild(byte[] rawBytes) {
            var text = GeneralNormalizeToString(rawBytes);

            try {
                var settings = new XmlReaderSettings {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null
                };

                XDocument doc;
                using (var sr = new StringReader(text))
                using (var xr = XmlReader.Create(sr, settings)) {
                    doc = XDocument.Load(xr, LoadOptions.None);
                }

                var root = doc.Root;
                if (root == null)
                    return Encoding.UTF8.GetBytes(text);

                var lines = new List<string>();

                var sdk = root.Attribute("Sdk")?.Value;
                if (!string.IsNullOrWhiteSpace(sdk))
                    lines.Add("Sdk=" + NormalizeWhitespace(sdk!.Trim()));

                var tfms = new List<string>();
                foreach (var e in root.Descendants()) {
                    var name = e.Name.LocalName;

                    if (name.Equals("TargetFramework", StringComparison.OrdinalIgnoreCase) ||
                        name.Equals("TargetFrameworks", StringComparison.OrdinalIgnoreCase)) {
                        var v = (e.Value ?? string.Empty).Trim();
                        if (v.Length == 0)
                            continue;

                        foreach (var part in v.Split(TelemetryConfig.ProjectIdentity.Separator, StringSplitOptions.RemoveEmptyEntries)) {
                            var t = part.Trim();
                            if (t.Length == 0)
                                continue;
                            tfms.Add(t.ToLowerInvariant());
                        }
                    }
                }

                tfms = [.. tfms
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(s => s, StringComparer.Ordinal)];

                if (tfms.Count > 0)
                    lines.Add("TargetFrameworks=" + string.Join(";", tfms));

                var langVersion = FindFirstElementValue(root, "LangVersion");
                if (!string.IsNullOrWhiteSpace(langVersion))
                    lines.Add("LangVersion=" + NormalizeWhitespace(langVersion.Trim()));

                var nullable = FindFirstElementValue(root, "Nullable");
                if (!string.IsNullOrWhiteSpace(nullable))
                    lines.Add("Nullable=" + NormalizeWhitespace(nullable.Trim()));

                var refs = new List<string>();
                foreach (var e in root.Descendants()) {
                    if (!e.Name.LocalName.Equals("ProjectReference", StringComparison.OrdinalIgnoreCase))
                        continue;

                    var inc = e.Attribute("Include")?.Value;
                    if (string.IsNullOrWhiteSpace(inc))
                        continue;

                    refs.Add("ProjectReference=" + NormalizeMsbuildPath(inc!.Trim()));
                }

                refs.Sort(StringComparer.Ordinal);
                lines.AddRange(refs);

                // Signature record: header + sorted lines.
                lines = [.. lines
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(s => s, StringComparer.Ordinal)];

                var sb = new StringBuilder();
                sb.Append("msbuild.v1\n");
                foreach (var l in lines)
                    sb.Append(l).Append('\n');

                EnsureSingleTrailingNewline(sb);
                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            catch {
                // Parse failed => general normalization bytes.
                return Encoding.UTF8.GetBytes(text);
            }
        }

        private static string FindFirstElementValue(XElement root, string localName) {
            if (root == null)
                return string.Empty;

            var first = root.Descendants()
                .FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));

            return first?.Value ?? string.Empty;
        }

        private static string NormalizeMsbuildPath(string value) {
            value = value.Replace('\\', '/');

            while (value.StartsWith("./", StringComparison.Ordinal))
                value = value.Substring(2);

            // Remove "/./" segments.
            while (true) {
                var idx = value.IndexOf("/./", StringComparison.Ordinal);
                if (idx < 0)
                    break;
#pragma warning disable CA1845 // Use span-based 'string.Concat'
                value = value.Substring(0, idx + 1) + value.Substring(idx + 3);
#pragma warning restore CA1845
            }

            return value;
        }

        private static byte[] CanonicalizeGlobalJson(byte[] rawBytes) {
            var text = GeneralNormalizeToString(rawBytes);

            try {
                var options = new JsonDocumentOptions {
                    AllowTrailingCommas = true,
                    CommentHandling = JsonCommentHandling.Skip
                };

                using var doc = JsonDocument.Parse(text, options);
                var root = doc.RootElement;

                var lines = new List<string>();

                if (TryGetProperty(root, "sdk", out var sdk) && sdk.ValueKind == JsonValueKind.Object) {
                    if (TryGetProperty(sdk, "version", out var v))
                        lines.Add("sdk.version=" + JsonValueToStableString(v));

                    if (TryGetProperty(sdk, "rollForward", out var rf))
                        lines.Add("sdk.rollForward=" + JsonValueToStableString(rf));

                    if (TryGetProperty(sdk, "allowPrerelease", out var ap))
                        lines.Add("sdk.allowPrerelease=" + JsonValueToStableString(ap));
                }

                if (TryGetProperty(root, "msbuild-sdks", out var msbuildSdks) && msbuildSdks.ValueKind == JsonValueKind.Object) {
                    var props = new List<(string Key, string Value)>();
                    foreach (var p in msbuildSdks.EnumerateObject()) {
                        props.Add((p.Name, JsonValueToStableString(p.Value)));
                    }

                    props.Sort((a, b) => StringComparer.Ordinal.Compare(a.Key, b.Key));

                    foreach (var (Key, Value) in props) {
                        lines.Add("msbuild-sdks." + Key + "=" + Value);
                    }
                }

                lines = [.. lines
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(s => s, StringComparer.Ordinal)];

                var sb = new StringBuilder();
                sb.Append("globaljson.v1\n");
                foreach (var l in lines)
                    sb.Append(l).Append('\n');

                EnsureSingleTrailingNewline(sb);
                return Encoding.UTF8.GetBytes(sb.ToString());
            }
            catch {
                return Encoding.UTF8.GetBytes(text);
            }
        }

        private static bool TryGetProperty(JsonElement obj, string name, out JsonElement value) {
            value = default;

            if (obj.ValueKind != JsonValueKind.Object)
                return false;

            // Case-sensitive per JSON; keep strict.
            if (!obj.TryGetProperty(name, out value))
                return false;

            return true;
        }

        private static string JsonValueToStableString(JsonElement e) {
            try {
                return e.ValueKind switch {
                    JsonValueKind.String => e.GetString() ?? string.Empty,
                    JsonValueKind.Number => e.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => "null",
                    _ => e.GetRawText()
                };
            }
            catch {
                return e.GetRawText();
            }
        }

        private static string GeneralNormalizeToString(byte[] rawBytes) {
            var text = DecodeUtf8BestEffort(rawBytes);
            text = NormalizeLineEndings(text);
            return NormalizeWhitespaceAndBlankLines(text);
        }

        private static byte[] GeneralNormalizeToUtf8Bytes(byte[] rawBytes) {
            return Encoding.UTF8.GetBytes(GeneralNormalizeToString(rawBytes));
        }

        private static string DecodeUtf8BestEffort(byte[] bytes) {
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            // First try strict UTF-8; if invalid, fall back to replacement.
            try {
                var strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
                var s = strict.GetString(bytes);
                return StripBom(s);
            }
            catch {
                var s = Encoding.UTF8.GetString(bytes); // replacement
                return StripBom(s);
            }
        }

        private static string StripBom(string s) {
            if (!string.IsNullOrEmpty(s) && s[0] == '\uFEFF')
                return s.Substring(1);
            return s;
        }

        private static string NormalizeLineEndings(string text) {
            // Convert CRLF and CR to LF.
            return text.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static string NormalizeWhitespaceAndBlankLines(string text) {
            var lines = text.Split('\n');

            var normalized = new List<string>(lines.Length);
            bool lastWasBlank = true; // also trims leading blanks

            foreach (var raw in lines) {
                var line = (raw ?? string.Empty).TrimEnd(' ', '\t');

                bool blank = line.Length == 0;
                if (blank) {
                    if (lastWasBlank)
                        continue; // collapse multiple blank lines
                    normalized.Add(string.Empty);
                    lastWasBlank = true;
                    continue;
                }

                normalized.Add(line);
                lastWasBlank = false;
            }

            // Trim trailing blank lines
            while (normalized.Count > 0 && normalized[normalized.Count - 1].Length == 0)
                normalized.RemoveAt(normalized.Count - 1);

            // Ensure exactly one trailing '\n'
            var result = string.Join("\n", normalized);
            if (result.Length > 0)
                result += "\n";
            else
                result = "\n";

            return result;
        }

        private static string NormalizeWhitespace(string value) {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var sb = new StringBuilder(value.Length);
            bool inWs = false;

            foreach (var c in value.Trim()) {
                if (char.IsWhiteSpace(c)) {
                    if (!inWs) {
                        sb.Append(' ');
                        inWs = true;
                    }
                }
                else {
                    sb.Append(c);
                    inWs = false;
                }
            }

            return sb.ToString();
        }

        private static void EnsureSingleTrailingNewline(StringBuilder sb) {
            if (sb.Length == 0) {
                sb.Append('\n');
                return;
            }

            // Remove trailing whitespace-only lines beyond a single newline.
            while (sb.Length > 0 && (sb[sb.Length - 1] == '\r' || sb[sb.Length - 1] == '\n'))
                sb.Length--;

            sb.Append('\n');
        }

        private static byte[] ConcatDigests(byte[] prefix, List<FileDigest> digests) {
            // "content.v1" || "\0" || concat(32-byte digests...)
            int total = prefix.Length + 1 + (digests.Count * 32);
            var buffer = new byte[total];

            int o = 0;
            Buffer.BlockCopy(prefix, 0, buffer, o, prefix.Length);
            o += prefix.Length;
            buffer[o++] = 0;

            foreach (var d in digests.Select(x => x.DigestBytes)) {
                Buffer.BlockCopy(d, 0, buffer, o, d.Length);
                o += d.Length;
            }

            return buffer;
        }

        private static byte[] Sha256(byte[] input) {
            using var sha = SHA256.Create();
            return sha.ComputeHash(input);
        }

        private static string GetRelativePath(string rootDir, string fullPath) {
            try {
                rootDir = SafeGetFullPath(rootDir);
                fullPath = SafeGetFullPath(fullPath);

                if (string.IsNullOrEmpty(rootDir) || string.IsNullOrEmpty(fullPath))
                    return string.Empty;

                if (!rootDir.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal) &&
                    !rootDir.EndsWith(Path.AltDirectorySeparatorChar.ToString(), StringComparison.Ordinal)) {
                    rootDir += Path.DirectorySeparatorChar;
                }

                var rootUri = new Uri(rootDir, UriKind.Absolute);
                var fileUri = new Uri(fullPath, UriKind.Absolute);

                var rel = Uri.UnescapeDataString(rootUri.MakeRelativeUri(fileUri).ToString());
                rel = rel.Replace('\\', '/');

                while (rel.StartsWith("./", StringComparison.Ordinal))
                    rel = rel.Substring(2);

                return rel;
            }
            catch {
                // Fallback: file name only (still deterministic within identity root choice).
                try { return Path.GetFileName(fullPath) ?? string.Empty; }
                catch { return string.Empty; }
            }
        }

        private static IEnumerable<string> SafeEnumerateFiles(string dir, string pattern) {
            try {
                return Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly);
            }
            catch {
                return [];
            }
        }

        private static IEnumerable<string> SafeEnumerateDirectories(string dir) {
            try {
                return Directory.EnumerateDirectories(dir, "*", SearchOption.TopDirectoryOnly);
            }
            catch {
                return [];
            }
        }

        private static bool TryReadFileBytesCapped(string path, int maxBytes, out byte[] bytes) {
            bytes = [];

            try {
                var fi = new FileInfo(path);
                if (!fi.Exists || fi.Length <= 0 || fi.Length > maxBytes)
                    return false;

                bytes = File.ReadAllBytes(path);
                return bytes.Length > 0 && bytes.Length <= maxBytes;
            }
            catch {
                bytes = [];
                return false;
            }
        }

        private static string SafeGetExtensionLower(string path) {
            try { return (Path.GetExtension(path) ?? string.Empty).ToLowerInvariant(); } catch { return string.Empty; }
        }

        private static string SafeGetFullPath(string path) {
            try { return Path.GetFullPath(path); } catch { return string.Empty; }
        }

        private static string? SafeGetParentDirectory(string path) {
            try { return Directory.GetParent(path)?.FullName; } catch { return null; }
        }

        private static bool PathsEqual(string a, string b) {
            if (Path.DirectorySeparatorChar == '\\') {
                return string.Equals(a.TrimEnd('\\', '/'), b.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
            }

            return string.Equals(a.TrimEnd('\\', '/'), b.TrimEnd('\\', '/'), StringComparison.Ordinal);
        }

        private readonly struct Candidate {
            public int Step { get; }
            public string FullPath { get; }

            public Candidate(int step, string fullPath) {
                Step = step;
                FullPath = fullPath;
            }
        }

        private readonly struct FileItem {
            public string Role { get; }
            public string Path { get; }

            public FileItem(string role, string path) {
                Role = role;
                Path = path;
            }
        }

        private readonly struct FileDigest {
            public string Role { get; }
            public string RelativePath { get; }
            public byte[] DigestBytes { get; }

            public FileDigest(string role, string relativePath, byte[] digestBytes) {
                Role = role;
                RelativePath = relativePath;
                DigestBytes = digestBytes;
            }
        }

        private readonly struct SlnProject {
            public string TypeGuid { get; }
            public string ProjectGuid { get; }
            public string Name { get; }

            public SlnProject(string typeGuid, string projectGuid, string name) {
                TypeGuid = typeGuid;
                ProjectGuid = projectGuid;
                Name = name;
            }
        }
    }
}
