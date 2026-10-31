using System.Diagnostics;
using System.Text.Json;

namespace Musoq.Benchmarks;

public sealed record CampaignReceipt(string Outcome, string RunDirectory, string Mode, string Status,
    string SchemaVersion, int Expected, int Completed, int Failed, int Missing, int Interrupted,
    bool Active, string? ActiveCase, DateTimeOffset LastProgressUtc, bool ResumeEligible,
    bool WebsiteEligible, string[] Reasons, string? ResumeCommand, string EngineVersion, string RunId,
    SourceStamp Source, DateTimeOffset? CompletedUtc, string[]? RecoveryNotes = null, bool Verified = false);

public sealed record LatestCampaigns(CampaignReceipt[] Selected, string[] Excluded);

public static class CampaignInspection
{
    public static CampaignReceipt Status(string directory) => ReadProgress(directory).Receipt;

    public static CampaignReceipt Verify(string directory)
    {
        var progress = ReadProgress(directory);
        var receipt = progress.Receipt;
        if (receipt.Status != "complete")
        {
            foreach (var attempt in progress.Attempts)
                BundleFiles.Validate(receipt.RunDirectory, attempt.RawReports ?? []);
            return receipt;
        }
        if (progress.Attempts.Count == 0 || progress.Receipt.RecoveryNotes!.Any(note => note.StartsWith("Invalid attempt", StringComparison.Ordinal)))
            throw new InvalidDataException("Completed campaign contains invalid attempt records.");
        var completedUtc = VerifyCompletion(receipt.RunDirectory, progress.Definition, receipt.Completed, progress.Attempts);
        return receipt with
        {
            Verified = true, CompletedUtc = completedUtc,
            WebsiteEligible = receipt.Mode == "Official" && receipt.Reasons.Length == 0
        };
    }

    private sealed record Progress(CampaignReceipt Receipt, RunManifest Definition, List<Measurement> Attempts);

    private static Progress ReadProgress(string directory)
    {
        directory = Path.GetFullPath(directory);
        var definition = CampaignStore.Definition(directory);
        ValidateCases(definition);
        var loaded = CampaignStore.ReadAttempts(directory, definition);
        var successes = loaded.Attempts.Where(item => item.Status == "complete").Select(item => item.CaseId).Distinct().Count();
        var latest = loaded.Attempts.GroupBy(item => item.CaseId).Select(group => group.Last()).ToArray();
        var started = ReadStarts(directory);
        var unfinished = started.Where(item => !loaded.Attempts.Any(result => result.AttemptId == item.Id)).ToArray();
        var active = Active(directory);
        string? activeCase = null;
        if (active)
            activeCase = unfinished.LastOrDefault()?.CaseId;
        var markerPath = Path.Combine(directory, "complete.json");
        var completedRun = File.Exists(markerPath);
        DateTimeOffset? finished = null;
        if (completedRun)
            finished = ArtifactJson.Read<CampaignCompletion>(markerPath).CompletedUtc;
        var reasons = ReadReasons(directory, definition);
        var status = completedRun ? "complete" : "incomplete";
        if (active)
            status = "running";
        var lastProgress = latest.Select(item => item.FinishedUtc).Concat(started.Select(item => item.StartedUtc))
            .Append(definition.StartedUtc).Max();
        var resumeCommand = "./scripts/benchmarks/Run-Benchmarks.ps1 -Resume '" + directory.Replace("'", "''", StringComparison.Ordinal) + "' -Json";
        var receipt = new CampaignReceipt(status, directory, definition.Provenance.Environment.Mode, status, definition.SchemaVersion,
            definition.Cases.Length, successes, latest.Count(item => item.Status == "failed"),
            definition.Cases.Count(item => !started.Any(start => start.CaseId == item.Id)),
            unfinished.Select(item => item.CaseId).Concat(latest.Where(item => item.Status == "interrupted").Select(item => item.CaseId))
                .Where(id => id != activeCase && !loaded.Attempts.Any(item => item.CaseId == id && item.Status == "complete")).Distinct().Count(),
            active, activeCase, lastProgress, !completedRun && !active,
            false, reasons, completedRun ? null : resumeCommand,
            definition.Provenance.Build.EngineVersion, definition.RunId, definition.Provenance.Source, finished, loaded.Notes.ToArray());
        return new(receipt, definition, loaded.Attempts);
    }

    private static string[] ReadReasons(string directory, RunManifest definition)
    {
        List<string> reasons = [];
        if (!File.Exists(Path.Combine(directory, "complete.json")))
            reasons.Add("Campaign has no completion marker.");
        reasons.AddRange(ReadErrors(directory));
        if (definition.Provenance.Environment.Mode != "Official")
            reasons.Add("Local campaigns, including Dry checks, are not website-eligible.");
        return reasons.ToArray();
    }

    private static string[] ReadErrors(string directory)
    {
        var path = Path.Combine(directory, "manifest.json");
        if (!File.Exists(path))
            return [];
        try
        {
            var error = ArtifactJson.Read<RunManifest>(path).LastError;
            return error is null ? [] : [error];
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            return [$"Mutable manifest view needs rebuilding: {exception.Message}"];
        }
    }

    public static LatestCampaigns Latest(string root)
    {
        if (!Directory.Exists(root))
            return new([], [$"History root does not exist: {Path.GetFullPath(root)}"]);
        var directories = Directory.GetFiles(root, "manifest.json", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(root, "definition.json", SearchOption.AllDirectories))
            .Select(path => Path.GetDirectoryName(path)!).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal);
        List<CampaignReceipt> eligible = [];
        List<string> excluded = [];
        foreach (var directory in directories)
        {
            try
            {
                var receipt = Verify(directory);
                if (receipt.WebsiteEligible)
                    eligible.Add(receipt);
                else
                    excluded.Add($"{directory}: {string.Join("; ", receipt.Reasons)}");
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or JsonException or UnauthorizedAccessException)
            {
                excluded.Add($"{directory}: {exception.Message}");
            }
        }
        var selected = eligible.GroupBy(item => item.EngineVersion, StringComparer.Ordinal)
            .Select(group => group.OrderByDescending(item => item.CompletedUtc).ThenBy(item => item.RunId, StringComparer.Ordinal).First())
            .OrderBy(item => item.EngineVersion, StringComparer.Ordinal).ToArray();
        return new(selected, excluded.ToArray());
    }

    private static void ValidateCases(RunManifest manifest)
    {
        if (manifest.Cases.Length == 0 || manifest.Cases.Select(item => item.Id).Distinct().Count() != manifest.Cases.Length)
            throw new InvalidDataException("Case manifest is empty or contains duplicate registrations.");
        if (manifest.Cases.Any(item => string.IsNullOrWhiteSpace(item.Sql) || item.InputCount < 0))
            throw new InvalidDataException("Case SQL or input count is invalid.");
    }

    private static DateTimeOffset VerifyCompletion(string directory, RunManifest definition, int completed, List<Measurement> attempts)
    {
        var marker = ArtifactJson.Read<CampaignCompletion>(Path.Combine(directory, "complete.json"));
        var manifest = ArtifactJson.Read<RunManifest>(Path.Combine(directory, "manifest.json"));
        if (marker.SchemaVersion != definition.SchemaVersion || marker.RunId != definition.RunId ||
            marker.Expected != definition.Cases.Length || marker.Completed != completed || completed != marker.Expected ||
            manifest.SchemaVersion != definition.SchemaVersion || manifest.RunId != definition.RunId ||
            manifest.SuiteVersion != definition.SuiteVersion || manifest.Status != "complete" || manifest.FinishedUtc != marker.CompletedUtc ||
            !CampaignComparison.SameCases(manifest.Cases, definition.Cases) ||
            !CampaignComparison.SameProvenance(manifest.Provenance, definition.Provenance))
            throw new InvalidDataException("Completion marker is inconsistent or successful cases are missing.");
        ValidateDerivedResults(directory, attempts);
        foreach (var attempt in attempts)
        {
            BundleFiles.Validate(directory, attempt.RawReports ?? []);
            if (attempt.Status == "complete" && (attempt.RawReports?.Length > 0 || definition.Provenance.Environment.Mode == "Official"))
                CampaignIntegrity.ValidateEvidence(directory, definition, attempt);
        }
        if (definition.Provenance.Environment.Mode == "Official")
            VerifyOfficial(directory, manifest);
        return marker.CompletedUtc;
    }

    private static void ValidateDerivedResults(string directory, List<Measurement> attempts)
    {
        var expected = attempts.GroupBy(item => item.CaseId).Select(group => group.Last())
            .OrderBy(item => item.CaseId, StringComparer.Ordinal).ToArray();
        var recorded = ArtifactJson.Read<Measurement[]>(Path.Combine(directory, "measurements.json"));
        if (!CampaignComparison.SameMeasurements(expected, recorded))
            throw new InvalidDataException("Recorded measurements differ from finalized attempts.");
        var summary = Path.Combine(directory, "summary.md");
        if (!File.Exists(summary) || new FileInfo(summary).Length == 0)
            throw new InvalidDataException("Completed campaign summary is missing.");
    }

    private static void VerifyOfficial(string directory, RunManifest manifest)
    {
        if (manifest.Preflight != "passed" || manifest.Provenance.Source.Dirty || manifest.Provenance.Environment.InProcess)
            throw new InvalidDataException("Official provenance or preflight is invalid.");
        var preflight = Path.Combine(directory, "raw", "preflight");
        if (!Directory.Exists(preflight))
            throw new InvalidDataException("Successful Official preflight evidence is missing.");
        var results = Directory.GetFiles(preflight, "*.json").Select(ArtifactJson.Read<CommandResult>).ToArray();
        if (!results.Any(result => result.ExitCode == 0 && result.Output is not null && result.Error is not null))
            throw new InvalidDataException("Successful Official preflight evidence is missing.");
    }

    private static AttemptStart[] ReadStarts(string directory)
    {
        var root = Path.Combine(directory, "attempts");
        if (!Directory.Exists(root))
            return [];
        List<AttemptStart> starts = [];
        foreach (var path in Directory.GetFiles(root, "start.json", SearchOption.AllDirectories))
        {
            try { starts.Add(ArtifactJson.Read<AttemptStart>(path)); }
            catch (Exception exception) when (exception is JsonException or IOException)
            {
                Console.Error.WriteLine($"Unreadable attempt start retained: {path}: {exception.Message}");
            }
        }
        return starts.OrderBy(item => item.StartedUtc).ToArray();
    }

    private static bool Active(string directory)
    {
        var sessions = Path.Combine(directory, "sessions");
        if (!Directory.Exists(sessions))
            return false;
        return Directory.GetFiles(sessions, "*.json").Any(SessionActive);
    }

    private static bool SessionActive(string path)
    {
        var session = ArtifactJson.Read<CampaignSession>(path);
        if (session.Status != "running" || session.Machine != Environment.MachineName)
            return false;
        try
        {
            using var process = Process.GetProcessById(session.ProcessId);
            return process.StartTime.ToUniversalTime() == session.ProcessStartedUtc && !process.HasExited;
        }
        catch (ArgumentException) { return false; }
    }
}
