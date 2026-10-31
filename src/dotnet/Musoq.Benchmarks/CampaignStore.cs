using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Musoq.Benchmarks;

public sealed class CampaignStore
{
    private readonly List<Measurement> _attempts;
    public string DirectoryPath { get; }
    public RunManifest Manifest { get; }
    public IReadOnlyList<Measurement> Attempts => _attempts.AsReadOnly();
    public IReadOnlyList<string> RecoveryNotes { get; private set; } = [];

    public CampaignStore(string directory, RunManifest manifest)
    {
        DirectoryPath = Path.GetFullPath(directory);
        Manifest = manifest;
        _attempts = [];
        if (Directory.Exists(DirectoryPath) && Directory.EnumerateFileSystemEntries(DirectoryPath).Any())
            throw new InvalidOperationException("A new run requires a new empty directory.");
        ArtifactJson.Write(Path.Combine(DirectoryPath, "definition.json"), manifest);
        Flush();
    }

    private CampaignStore(string directory, RunManifest manifest, List<Measurement> attempts)
    {
        DirectoryPath = directory;
        Manifest = manifest;
        _attempts = attempts;
    }

    public static RunManifest Definition(string directory)
    {
        var path = Path.Combine(directory, "definition.json");
        if (!File.Exists(path))
            throw new InvalidDataException("Unsupported or damaged campaign: schema-v3 definition.json is required.");
        var definition = ArtifactJson.Read<RunManifest>(path);
        if (definition.SchemaVersion != "3")
            throw new InvalidDataException("Unsupported campaign schema; only schema v3 is supported.");
        return definition;
    }

    public static CampaignStore Resume(string directory, RunManifest expected)
    {
        var fullPath = Path.GetFullPath(directory);
        if (File.Exists(Path.Combine(fullPath, "complete.json")))
            throw new CampaignCompatibilityException("Completed campaigns are immutable.");
        var manifest = Definition(fullPath);
        ValidateResume(manifest, expected);
        var loaded = ReadVerifiedAttempts(fullPath, manifest);
        return new CampaignStore(fullPath, manifest, loaded.Attempts) { RecoveryNotes = loaded.Notes };
    }

    public static void ValidateResume(RunManifest saved, RunManifest expected)
    {
        if (saved.Status == "complete")
            throw new CampaignCompatibilityException("Completed campaigns are immutable.");
        if (saved.SchemaVersion != expected.SchemaVersion ||
            saved.SuiteVersion != expected.SuiteVersion)
            throw new CampaignCompatibilityException("Resume requires identical schema and suite versions.");
        if (!CampaignComparison.SameProvenance(saved.Provenance, expected.Provenance))
            throw new CampaignCompatibilityException("Resume provenance differs: source or machine/environment settings changed.");
        if (!CampaignComparison.SameCases(saved.Cases, expected.Cases))
            throw new CampaignCompatibilityException("Resume requires identical cases, parameters and measurement settings.");
    }

    public bool Completed(string id) => _attempts.Any(attempt => attempt.CaseId == id && attempt.Status == "complete");

    public AttemptStart Begin(string caseId)
    {
        EnsureMutable();
        if (!Manifest.Cases.Any(item => item.Id == caseId))
            throw new InvalidDataException("Measurement is not part of this campaign.");
        if (Completed(caseId))
            throw new InvalidOperationException("Successful cases are retained when resuming.");
        var attempt = new AttemptStart(Guid.NewGuid().ToString("N"), caseId, DateTimeOffset.UtcNow);
        ArtifactJson.Write(AttemptPath(attempt.Id, "start.json"), attempt);
        return attempt;
    }

    public string RawDirectory(AttemptStart attempt) => CampaignPaths.RawDirectory(DirectoryPath, attempt.Id);
    private string AttemptPath(string id, string name) => Path.Combine(DirectoryPath, "attempts", id, name);

    public void Save(Measurement measurement)
    {
        ValidateMeasurement(Manifest, measurement);
        var attempt = Begin(measurement.CaseId);
        Complete(attempt, measurement);
    }

    public void Complete(AttemptStart attempt, Measurement measurement)
    {
        EnsureMutable();
        if (measurement.CaseId != attempt.CaseId || Completed(attempt.CaseId))
            throw new InvalidOperationException("Attempt does not match an unfinished case.");
        var start = ArtifactJson.Read<AttemptStart>(AttemptPath(attempt.Id, "start.json"));
        if (start != attempt || File.Exists(AttemptPath(attempt.Id, "result.json")))
            throw new InvalidOperationException("Attempt is missing or already finalized.");
        ValidateMeasurement(Manifest, measurement);
        if (measurement.Status == "complete" && Manifest.Provenance.Environment.Mode == "Official")
            CampaignIntegrity.RequireReports(measurement);
        BundleFiles.Validate(DirectoryPath, measurement.RawReports ?? []);
        var saved = measurement with { AttemptId = attempt.Id };
        ArtifactJson.Write(AttemptPath(attempt.Id, "result.json"), saved);
        _attempts.Add(saved);
        Flush();
    }

    public void Finish()
    {
        EnsureMutable();
        var preflightPassed = Manifest.Provenance.Environment.Mode != "Official" || Manifest.Preflight == "passed";
        Manifest.Status = preflightPassed && Manifest.Cases.All(item => Completed(item.Id)) ? "complete" : "incomplete";
        Manifest.FinishedUtc = DateTimeOffset.UtcNow;
        Flush();
        if (Manifest.Status != "complete")
            return;
        ArtifactJson.Write(Path.Combine(DirectoryPath, "complete.json"),
            new CampaignCompletion(Manifest.SchemaVersion, Manifest.RunId, Manifest.FinishedUtc.Value,
                Manifest.Cases.Length, _attempts.Where(item => item.Status == "complete").Select(item => item.CaseId).Distinct().Count()));
    }

    public void Flush()
    {
        EnsureMutable();
        ArtifactJson.Write(Path.Combine(DirectoryPath, "measurements.json"), Latest());
        WriteSummary();
        ArtifactJson.Write(Path.Combine(DirectoryPath, "manifest.json"), Manifest);
    }

    public static (List<Measurement> Attempts, List<string> Notes) ReadAttempts(string directory, RunManifest manifest)
    {
        List<Measurement> attempts = [];
        List<string> notes = [];
        var root = Path.Combine(directory, "attempts");
        if (!Directory.Exists(root))
            return (attempts, notes);
        foreach (var path in Directory.GetDirectories(root).Order(StringComparer.Ordinal))
        {
            try
            {
                var start = ArtifactJson.Read<AttemptStart>(Path.Combine(path, "start.json"));
                var resultPath = Path.Combine(path, "result.json");
                if (!File.Exists(resultPath))
                {
                    notes.Add($"Interrupted attempt {start.Id}: {start.CaseId}; evidence retained.");
                    continue;
                }
                var result = ArtifactJson.Read<Measurement>(resultPath);
                ValidateMeasurement(manifest, result);
                if (start.Id != Path.GetFileName(path) || result.AttemptId != start.Id ||
                    result.CaseId != start.CaseId)
                    throw new InvalidDataException("Attempt identity or evidence index differs.");
                attempts.Add(result);
            }
            catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
            {
                notes.Add($"Invalid attempt {Path.GetFileName(path)} retained: {exception.Message}");
            }
        }
        return (attempts.OrderBy(item => item.FinishedUtc).ThenBy(item => item.AttemptId, StringComparer.Ordinal).ToList(), notes);
    }

    public static (List<Measurement> Attempts, List<string> Notes) ReadVerifiedAttempts(string directory, RunManifest manifest)
    {
        var loaded = ReadAttempts(directory, manifest);
        List<Measurement> valid = [];
        foreach (var attempt in loaded.Attempts)
        {
            try
            {
                BundleFiles.Validate(directory, attempt.RawReports ?? []);
                if (attempt.Status == "complete" && (attempt.RawReports?.Length > 0 || manifest.Provenance.Environment.Mode == "Official"))
                    CampaignIntegrity.ValidateEvidence(directory, manifest, attempt);
                valid.Add(attempt);
            }
            catch (Exception exception) when (exception is IOException or JsonException or InvalidDataException or UnauthorizedAccessException)
            {
                loaded.Notes.Add($"Invalid attempt {attempt.AttemptId} retained: {exception.Message}");
            }
        }
        return (valid, loaded.Notes);
    }

    public static void ValidateMeasurement(RunManifest manifest, Measurement measurement)
    {
        if (!manifest.Cases.Any(item => item.Id == measurement.CaseId))
            throw new InvalidDataException("Measurement is not part of this campaign.");
        if (measurement.Status is not ("complete" or "failed" or "interrupted"))
            throw new InvalidDataException("Unknown measurement status.");
        if (measurement.FinishedUtc < measurement.StartedUtc)
            throw new InvalidDataException("Measurement timestamps are reversed.");
        if (measurement.Status == "complete" && (measurement.SampleCount <= 0 ||
            measurement.MeanNanoseconds is not > 0 || !double.IsFinite(measurement.MeanNanoseconds.Value)))
            throw new InvalidDataException("A successful measurement needs positive finite timings and samples.");
        double?[] statistics = [measurement.MedianNanoseconds, measurement.StandardDeviationNanoseconds,
            measurement.ConfidenceLowerNanoseconds, measurement.ConfidenceUpperNanoseconds];
        if (statistics.Any(value => value.HasValue && !double.IsFinite(value.Value)))
            throw new InvalidDataException("Measurement statistics must be finite where available.");
    }

    private Measurement[] Latest() => _attempts.GroupBy(item => item.CaseId).Select(group => group.Last())
        .OrderBy(item => item.CaseId, StringComparer.Ordinal).ToArray();

    private void WriteSummary()
    {
        var stamp = Manifest.Provenance;
        var text = new StringBuilder("# Musoq benchmark campaign\n\n");
        text.AppendLine($"Version: {stamp.Build.EngineVersion}  \nBranch: {stamp.Source.Branch ?? "(detached)"}  \nCommit: {stamp.Source.Commit}  \nDirty: {stamp.Source.Dirty}  \nMode: {stamp.Environment.Mode}  \nStatus: {Manifest.Status}");
        text.AppendLine($"\nMachine: {stamp.Hardware.Profile}; {stamp.Hardware.CpuModel}; {stamp.Hardware.PhysicalCores?.ToString(CultureInfo.InvariantCulture) ?? "unknown"} physical/{stamp.Hardware.LogicalCores} logical cores; {stamp.Hardware.MemoryBytes} RAM bytes.");
        text.AppendLine($"\nEnvironment: {stamp.Environment.Os}; {stamp.Environment.Runtime}; SDK {stamp.Environment.Sdk}. Suite: {Manifest.SuiteVersion}.");
        text.AppendLine("\nLower elapsed time is better. Compare the same case, input parameters, suite and environment. Dry results are execution checks.\n\n| Case | Status | Mean ms | Allocated B/op |\n|---|---|---:|---:|");
        foreach (var item in Latest())
            text.AppendLine($"| {item.CaseId} | {item.Status} | {(item.MeanNanoseconds / 1_000_000)?.ToString("F4", CultureInfo.InvariantCulture) ?? "unavailable"} | {item.AllocatedBytesPerOperation?.ToString(CultureInfo.InvariantCulture) ?? "unavailable"} |");
        var path = Path.Combine(DirectoryPath, "summary.md");
        ArtifactJson.WriteText(path, text.ToString());
    }

    private void EnsureMutable()
    {
        if (File.Exists(Path.Combine(DirectoryPath, "complete.json")))
            throw new CampaignCompatibilityException("Completed campaigns are immutable.");
    }
}
