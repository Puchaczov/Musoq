using System.Security.Cryptography;
using Musoq.Benchmarks.Scenarios;

namespace Musoq.Benchmarks;

public sealed record BenchmarkSnapshotChange(
    string CaseId,
    string ScenarioId,
    string Status,
    string[] ChangedSections,
    string? Filter,
    string? BaselineFile,
    string? CandidateFile,
    string? Reason
);

public sealed record BenchmarkSnapshotDiff(
    string SuiteVersion,
    string BaselineCommit,
    SourceStamp Candidate,
    bool BaselineHasSnapshots,
    string[] WorkloadSourceChanges,
    BenchmarkSnapshotChange[] Cases,
    string? CandidatePatchSha256 = null
);

internal static class BenchmarkSnapshotChanges
{
    private static readonly string[] WorkloadSources =
    [
        "src/dotnet/Musoq.Benchmarks.Scenarios",
        "src/dotnet/Musoq.Benchmarks/BenchmarkRegistrations.cs",
        "src/dotnet/Musoq.Benchmarks/BenchmarkSizes.cs",
        "src/dotnet/Musoq.Benchmarks/CampaignCases.cs",
        "src/dotnet/Musoq.Benchmarks/ExecutionSettings.cs",
        "src/dotnet/Musoq.Benchmarks/LifecycleBenchmarks.cs",
        "src/dotnet/Musoq.Benchmarks/QueryExecutionBenchmark.cs",
        "src/dotnet/Musoq.Benchmarks/TypedOutputBenchmarks.cs",
    ];

    internal static BenchmarkSnapshotDiff Inspect(string root, string baselineRef, string? candidatePatch)
    {
        var baseline = ProcessCommand.Require(
            root,
            "git",
            "rev-parse",
            "--verify",
            "--end-of-options",
            baselineRef + "^{commit}"
        );
        var candidate = ProvenanceCollector.Source(root);
        var manifestPath = BenchmarkSnapshotManifest.RelativeDirectory + "/" + BenchmarkSnapshotManifest.FileName;
        var candidateManifest = BenchmarkSnapshotManifest.Parse(File.ReadAllText(Path.Combine(root, manifestPath)));
        if (candidateManifest.SuiteVersion != ScenarioCatalog.SuiteVersion)
            throw new InvalidDataException("Benchmark snapshot suite is stale. Regenerate the snapshot collection.");

        var baselineObject = baseline + ":" + manifestPath;
        var baselineHasSnapshots = ProcessCommand.Run(root, "git", "cat-file", "-e", baselineObject).ExitCode == 0;
        var changes = baselineHasSnapshots
            ? Compare(
                BenchmarkSnapshotManifest.Parse(ProcessCommand.Require(root, "git", "show", baselineObject)),
                candidateManifest,
                file =>
                    ProcessCommand.Require(
                        root,
                        "git",
                        "show",
                        $"{baseline}:{BenchmarkSnapshotManifest.RelativeDirectory}/{file}"
                    ),
                file => File.ReadAllText(Path.Combine(root, BenchmarkSnapshotManifest.RelativeDirectory, file))
            )
            : [];
        var patchHash = candidatePatch is null ? null : VerifyCandidatePatch(root, candidate, candidatePatch);
        if (!CampaignComparison.SameSource(candidate, ProvenanceCollector.Source(root)))
            throw new InvalidOperationException("Source changed during snapshot inspection. Regenerate and retry.");

        return new(
            candidateManifest.SuiteVersion,
            baseline,
            candidate,
            baselineHasSnapshots,
            ChangedPaths(root, baseline, WorkloadSources),
            changes,
            patchHash
        );
    }

    internal static BenchmarkSnapshotChange[] Compare(
        BenchmarkSnapshotManifest baseline,
        BenchmarkSnapshotManifest candidate,
        Func<string, string> readBaseline,
        Func<string, string> readCandidate
    )
    {
        var before = baseline.Cases.ToDictionary(item => item.Definition.Id, StringComparer.Ordinal);
        var after = candidate.Cases.ToDictionary(item => item.Definition.Id, StringComparer.Ordinal);
        var baselineSections = ReadSections(baseline, readBaseline);
        var candidateSections = ReadSections(candidate, readCandidate);
        var changes = new List<BenchmarkSnapshotChange>();
        foreach (var id in before.Keys.Union(after.Keys).Order(StringComparer.Ordinal))
        {
            var left = before.GetValueOrDefault(id);
            var right = after.GetValueOrDefault(id);
            if (left is null)
            {
                changes.Add(
                    new(id, right!.Definition.ScenarioId, "added", [], id, null, right.File, "No baseline case.")
                );
                continue;
            }
            if (right is null)
            {
                changes.Add(
                    new(
                        id,
                        left.Definition.ScenarioId,
                        "retired",
                        [],
                        null,
                        left.File,
                        null,
                        "The case is absent from the active inventory."
                    )
                );
                continue;
            }
            var baselineContent = baselineSections[left.File];
            var candidateContent = candidateSections[right.File];
            var sections = baselineContent.ChangedSections(candidateContent);
            var reason = Difference(left, right, baselineContent, candidateContent);
            if (baseline.SuiteVersion != candidate.SuiteVersion)
                reason = $"Benchmark suite changed from '{baseline.SuiteVersion}' to '{candidate.SuiteVersion}'.";
            if (reason is not null || sections.Length != 0)
                changes.Add(
                    new(
                        id,
                        right.Definition.ScenarioId,
                        reason is null ? "changed" : "incompatible",
                        sections,
                        id,
                        left.File,
                        right.File,
                        reason
                    )
                );
        }
        return changes.ToArray();
    }

    private static Dictionary<string, BenchmarkSnapshotSections> ReadSections(
        BenchmarkSnapshotManifest manifest,
        Func<string, string> read
    ) =>
        manifest
            .Cases.Select(item => item.File)
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(file => file, file => BenchmarkSnapshotSections.Parse(read(file)), StringComparer.Ordinal);

    private static string? Difference(
        BenchmarkSnapshotCase baseline,
        BenchmarkSnapshotCase candidate,
        BenchmarkSnapshotSections baselineContent,
        BenchmarkSnapshotSections candidateContent
    )
    {
        if (baselineContent.Sql != candidateContent.Sql)
            return "Canonical SQL changed.";
        if (baseline.OutputType != candidate.OutputType)
            return "The typed result contract changed.";
        return CampaignComparison.WorkloadDifference(baseline.Definition, candidate.Definition);
    }

    private static string VerifyCandidatePatch(string root, SourceStamp candidate, string path)
    {
        if (!candidate.Dirty)
            throw new ArgumentException("A candidate source patch applies only to a dirty working tree.");
        var patchPath = Path.GetFullPath(path);
        var stats = ProcessCommand.Require(root, "git", "apply", "--numstat", "-z", "--", patchPath);
        var patchFiles = stats
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split('\t', 3))
            .Where(fields => fields.Length == 3)
            .Select(fields => fields[2])
            .ToArray();
        if (!patchFiles.Order(StringComparer.Ordinal).SequenceEqual(ChangedPaths(root, candidate.Commit, ["."])))
            throw new InvalidDataException(
                "The candidate patch must include every changed and untracked file. Capture it with --no-renames."
            );
        ProcessCommand.Require(root, "git", "apply", "--check", "--reverse", "--binary", "--", patchPath);
        VerifyPatchBlobs(root, candidate.Commit, patchPath, patchFiles);
        return PatchHash(patchPath);
    }

    private static void VerifyPatchBlobs(string root, string commit, string patchPath, string[] files)
    {
        var blobs = ReadPatchBlobs(patchPath);
        if (blobs.Count != files.Length)
            throw new InvalidDataException("The candidate patch has an inconsistent file inventory.");
        foreach (var (file, blob) in files.Zip(blobs))
        {
            var expected = blob ?? ProcessCommand.Require(root, "git", "rev-parse", $"{commit}:{file}");
            if (expected.Length != commit.Length)
                throw new InvalidDataException("Capture the candidate patch using --binary --full-index.");
            if (expected.All(character => character == '0'))
                continue;
            var actual = ProcessCommand.Require(root, "git", "hash-object", $"--path={file}", "--", file);
            if (actual != expected)
                throw new InvalidDataException($"The candidate patch contains stale content for '{file}'.");
        }
    }

    private static List<string?> ReadPatchBlobs(string patchPath)
    {
        var blobs = new List<string?>();
        foreach (var line in File.ReadLines(patchPath))
        {
            if (line.StartsWith("diff --git ", StringComparison.Ordinal))
                blobs.Add(null);
            if (blobs.Count == 0 || !line.StartsWith("index ", StringComparison.Ordinal))
                continue;
            var ids = line.Split(' ')[1].Split("..", StringSplitOptions.None);
            if (ids.Length != 2)
                throw new InvalidDataException("Invalid blob IDs in the candidate patch.");
            blobs[^1] = ids[1];
        }
        return blobs;
    }

    internal static string PatchHash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static string[] ChangedPaths(string root, string baseline, string[] paths)
    {
        var tracked = ProcessCommand.Require(
            root,
            "git",
            ["diff", "--name-only", "-z", "--no-renames", baseline, "--", .. paths]
        );
        var untracked = ProcessCommand.Require(
            root,
            "git",
            ["ls-files", "--others", "--exclude-standard", "-z", "--", .. paths]
        );
        return tracked
            .Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Union(untracked.Split('\0', StringSplitOptions.RemoveEmptyEntries), StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
    }
}
