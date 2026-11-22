using System.Text.Json;

namespace Musoq.Benchmarks;

public sealed record BenchmarkRunPair(string Baseline, string Candidate);

public sealed record BenchmarkComparisonInput(
    string SnapshotDiff,
    BenchmarkRunPair[] Runs,
    string[]? AdditionalCaseIds = null,
    string? CandidatePatch = null
);

internal sealed record BenchmarkComparisonReport(
    string BaselineCommit,
    SourceStamp Candidate,
    string? CandidatePatch,
    string? CandidatePatchSha256,
    string[] WorkloadSourceChanges,
    ComparedRunPair[] Runs,
    ComparedBenchmarkCase[] Cases
);

internal sealed record ComparedRunPair(int Pair, ComparedRun Baseline, ComparedRun Candidate, string? Incompatibility);

internal sealed record ComparedRun(string Directory, string? RunId, RunProvenance? Provenance, string? Error);

internal sealed record ComparedBenchmarkCase(
    string CaseId,
    int? Pair,
    string Status,
    string? Reason,
    TimingComparison? Timing = null,
    AllocationComparison? Allocations = null,
    string? AllocationUnavailableReason = null
);

internal sealed record TimingObservation(
    double MeanNanoseconds,
    double? ConfidenceLowerNanoseconds,
    double? ConfidenceUpperNanoseconds,
    int SampleCount
);

internal sealed record TimingComparison(
    TimingObservation Baseline,
    TimingObservation Candidate,
    double DeltaNanoseconds,
    double ChangePercent,
    string ConfidenceIntervalRelation
);

internal sealed record AllocationComparison(
    long BaselineBytes,
    long CandidateBytes,
    long DeltaBytes,
    double? ChangePercent
);

internal static class BenchmarkRegressionComparison
{
    internal static BenchmarkComparisonReport Read(string inputPath)
    {
        var path = Path.GetFullPath(inputPath);
        var directory = Path.GetDirectoryName(path)!;
        var input = ArtifactJson.Read<BenchmarkComparisonInput>(path);
        if (string.IsNullOrWhiteSpace(input.SnapshotDiff) || input.Runs is null)
            throw new ArgumentException("A comparison input requires snapshotDiff and runs.");
        var diff = ArtifactJson.Read<BenchmarkSnapshotDiff>(Path.GetFullPath(input.SnapshotDiff, directory));
        ValidateDiff(diff);
        var patchPath = input.CandidatePatch is null ? null : Path.GetFullPath(input.CandidatePatch, directory);
        var patchReason = CheckCandidatePatch(diff, patchPath);
        var runs = input
            .Runs.Select((pair, index) => ReadPair(directory, pair, index + 1, diff, patchReason))
            .ToArray();
        var changes = diff.Cases.ToDictionary(item => item.CaseId, StringComparer.Ordinal);
        var comparisonCases = changes
            .Keys.Union(input.AdditionalCaseIds ?? [], StringComparer.Ordinal)
            .Order(StringComparer.Ordinal);
        var cases = comparisonCases.SelectMany(id => CompareCase(id, changes.GetValueOrDefault(id), runs)).ToArray();
        return new(
            diff.BaselineCommit,
            diff.Candidate,
            patchPath,
            diff.CandidatePatchSha256,
            diff.WorkloadSourceChanges,
            runs.Select(pair => pair.Report).ToArray(),
            cases
        );
    }

    private static void ValidateDiff(BenchmarkSnapshotDiff diff)
    {
        if (
            string.IsNullOrWhiteSpace(diff.BaselineCommit)
            || diff.Candidate is null
            || string.IsNullOrWhiteSpace(diff.Candidate.Commit)
            || string.IsNullOrWhiteSpace(diff.SuiteVersion)
            || diff.Cases is null
            || diff.WorkloadSourceChanges is null
        )
            throw new InvalidDataException("The comparison requires a complete snapshot diff receipt.");
    }

    private static string? CheckCandidatePatch(BenchmarkSnapshotDiff diff, string? patchPath)
    {
        if (!diff.Candidate.Dirty)
            return patchPath is null
                ? null
                : "The snapshot receipt describes a clean candidate and does not require a patch.";
        if (patchPath is null || !File.Exists(patchPath) || diff.CandidatePatchSha256 is null)
            return "A dirty candidate requires the retained source patch verified when generating the snapshot diff.";
        return BenchmarkSnapshotChanges.PatchHash(patchPath) == diff.CandidatePatchSha256
            ? null
            : "The retained candidate patch differs from the patch verified by the snapshot diff.";
    }

    private static RunPairEvidence ReadPair(
        string directory,
        BenchmarkRunPair pair,
        int index,
        BenchmarkSnapshotDiff diff,
        string? patchReason
    )
    {
        if (string.IsNullOrWhiteSpace(pair?.Baseline) || string.IsNullOrWhiteSpace(pair.Candidate))
            throw new ArgumentException("Each run pair requires baseline and candidate directories.");
        var baseline = ReadRun(Path.GetFullPath(pair.Baseline, directory));
        var candidate = ReadRun(Path.GetFullPath(pair.Candidate, directory));
        var reason = PairIncompatibility(baseline, candidate, diff, patchReason);
        return new(new(index, baseline.Reference, candidate.Reference, reason), baseline, candidate);
    }

    private static string? PairIncompatibility(
        RunEvidence baseline,
        RunEvidence candidate,
        BenchmarkSnapshotDiff diff,
        string? patchReason
    )
    {
        if (baseline.Reference.Error is { } baselineError)
            return "Baseline run: " + baselineError;
        if (candidate.Reference.Error is { } candidateError)
            return "Candidate run: " + candidateError;
        if (patchReason is not null)
            return patchReason;

        var before = baseline.Reference.Provenance!;
        var after = candidate.Reference.Provenance!;
        if (before.Source.Commit != diff.BaselineCommit || before.Source.Dirty || !BuildIdentifiesSource(before))
            return "Baseline run must identify the selected clean Git revision in both source and build provenance.";
        if (
            after.Source.Commit != diff.Candidate.Commit
            || after.Source.Dirty != diff.Candidate.Dirty
            || !BuildIdentifiesSource(after)
        )
            return "Candidate source or build provenance differs from the snapshot diff.";
        if (baseline.SuiteVersion != diff.SuiteVersion || candidate.SuiteVersion != diff.SuiteVersion)
            return "The measurement suite differs from the snapshot suite.";
        if (
            before.Environment.InProcess
            || after.Environment.InProcess
            || before.Environment.Job == "Dry"
            || after.Environment.Job == "Dry"
        )
            return "Dry or in-process runs cannot verify performance.";
        return CampaignComparison.SameMeasurementEnvironment(before, after)
            ? null
            : "The runs differ in hardware, runtime, SDK, GC, BenchmarkDotNet, or job settings.";
    }

    private static bool BuildIdentifiesSource(RunProvenance provenance) =>
        provenance.Build.EngineInformationalVersion.EndsWith("+" + provenance.Source.Commit, StringComparison.Ordinal)
        && provenance.Build.SuiteInformationalVersion.EndsWith(
            "+" + provenance.Source.Commit,
            StringComparison.Ordinal
        );

    private static RunEvidence ReadRun(string directory)
    {
        try
        {
            var receipt = CampaignInspection.Verify(directory);
            if (!receipt.Verified)
                return UnavailableRun(directory, $"The campaign is {receipt.Status}.", receipt.RunId);
            var definition = CampaignStore.Definition(directory);
            var cases = definition.Cases.ToDictionary(item => item.Id, StringComparer.Ordinal);
            var measurements = ArtifactJson
                .Read<Measurement[]>(Path.Combine(directory, "measurements.json"))
                .ToDictionary(item => item.CaseId, StringComparer.Ordinal);
            return new(
                new(directory, receipt.RunId, definition.Provenance, null),
                definition.SuiteVersion,
                cases,
                measurements
            );
        }
        catch (Exception error)
            when (error is IOException or InvalidDataException or JsonException or UnauthorizedAccessException)
        {
            return UnavailableRun(directory, error.Message);
        }
    }

    private static RunEvidence UnavailableRun(string directory, string error, string? runId = null) =>
        new(new(directory, runId, null, error), null, new(StringComparer.Ordinal), new(StringComparer.Ordinal));

    private static IEnumerable<ComparedBenchmarkCase> CompareCase(
        string caseId,
        BenchmarkSnapshotChange? change,
        RunPairEvidence[] runs
    )
    {
        if (change?.Reason is { } reason)
            return [Unavailable(caseId, null, reason)];
        var relevant = runs.Where(pair =>
                pair.Baseline.Cases.ContainsKey(caseId) || pair.Candidate.Cases.ContainsKey(caseId)
            )
            .ToArray();
        if (relevant.Length == 0)
            return [Unavailable(caseId, null, "No supplied run pair contains this case.")];
        return relevant.Select(pair => ComparePair(caseId, pair));
    }

    private static ComparedBenchmarkCase ComparePair(string caseId, RunPairEvidence pair)
    {
        var index = pair.Report.Pair;
        if (pair.Report.Incompatibility is { } reason)
            return Unavailable(caseId, index, reason);
        var baseline = pair.Baseline.Cases.GetValueOrDefault(caseId);
        var candidate = pair.Candidate.Cases.GetValueOrDefault(caseId);
        if (baseline is null || candidate is null)
            return Unavailable(
                caseId,
                index,
                baseline is null ? "Baseline case is missing." : "Candidate case is missing."
            );
        if (CampaignComparison.WorkloadDifference(baseline, candidate) is { } difference)
            return Unavailable(caseId, index, difference);
        var before = pair.Baseline.Measurements[caseId];
        var after = pair.Candidate.Measurements[caseId];
        return CompareMeasurements(before, after, candidate.Phase, index);
    }

    internal static ComparedBenchmarkCase CompareMeasurements(
        Measurement baseline,
        Measurement candidate,
        string phase,
        int pair
    )
    {
        var caseId = candidate.CaseId;
        if (!HasTimingEvidence(baseline) || !HasTimingEvidence(candidate))
            return Unavailable(
                caseId,
                pair,
                "A successful measurement with native timing and measured-process evidence is missing."
            );

        var before = Timing(baseline);
        var after = Timing(candidate);
        var delta = after.MeanNanoseconds - before.MeanNanoseconds;
        var timing = new TimingComparison(
            before,
            after,
            delta,
            100 * (delta / before.MeanNanoseconds),
            ConfidenceIntervalRelation(before, after)
        );
        var allocation = Allocations(baseline, candidate);
        var allocationReason =
            allocation is not null ? null
            : phase == "compilation-cold" ? "First compilation does not collect allocations."
            : "Allocation measurements are unavailable in at least one run.";
        return new(caseId, pair, "comparable", null, timing, allocation, allocationReason);
    }

    private static bool HasTimingEvidence(Measurement measurement) =>
        measurement is { Status: "complete", SampleCount: > 0, MeanNanoseconds: > 0 }
        && (measurement.RawReports ?? []).Any(path => path.EndsWith("-report-full.json", StringComparison.Ordinal))
        && (measurement.RawReports ?? []).Any(path =>
            Path.GetFileName(path).StartsWith("measured-process-", StringComparison.Ordinal)
        );

    private static TimingObservation Timing(Measurement measurement) =>
        new(
            measurement.MeanNanoseconds!.Value,
            measurement.ConfidenceLowerNanoseconds,
            measurement.ConfidenceUpperNanoseconds,
            measurement.SampleCount
        );

    private static string ConfidenceIntervalRelation(TimingObservation baseline, TimingObservation candidate)
    {
        if (
            baseline.ConfidenceLowerNanoseconds is not { } beforeLower
            || baseline.ConfidenceUpperNanoseconds is not { } beforeUpper
            || candidate.ConfidenceLowerNanoseconds is not { } afterLower
            || candidate.ConfidenceUpperNanoseconds is not { } afterUpper
        )
            return "unavailable";
        if (afterLower > beforeUpper)
            return "candidate-higher";
        return afterUpper < beforeLower ? "candidate-lower" : "overlap";
    }

    private static AllocationComparison? Allocations(Measurement baseline, Measurement candidate)
    {
        if (
            baseline.AllocatedBytesPerOperation is not long before
            || candidate.AllocatedBytesPerOperation is not long after
        )
            return null;
        var delta = after - before;
        return new(before, after, delta, before == 0 ? null : 100 * ((double)delta / before));
    }

    private static ComparedBenchmarkCase Unavailable(string id, int? pair, string reason) =>
        new(id, pair, "unavailable", reason);

    private sealed record RunEvidence(
        ComparedRun Reference,
        string? SuiteVersion,
        Dictionary<string, CaseDefinition> Cases,
        Dictionary<string, Measurement> Measurements
    );

    private sealed record RunPairEvidence(ComparedRunPair Report, RunEvidence Baseline, RunEvidence Candidate);
}
