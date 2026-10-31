using System.Text.Json.Nodes;

namespace Musoq.Benchmarks;

public static class CampaignIntegrity
{
    public static void ValidateOfficialBuild(RunProvenance provenance)
    {
        if (!provenance.Build.EngineInformationalVersion.Contains(provenance.Source.Commit, StringComparison.Ordinal) ||
            !provenance.Build.SuiteInformationalVersion.Contains(provenance.Source.Commit, StringComparison.Ordinal))
            throw new ArgumentException("Official launcher binaries do not identify the selected commit. Rebuild before collection.");
    }

    public static void ValidateSource(string root, RunManifest manifest)
    {
        if (!CampaignComparison.SameSource(ProvenanceCollector.Source(root), manifest.Provenance.Source))
            throw new InvalidOperationException("Source changed during measurement; restore the recorded checkout before resuming.");
    }

    public static void ValidateProcesses(string directory, RunManifest manifest, CaseDefinition definition)
    {
        ValidateProcessFiles(Directory.GetFiles(directory, "measured-process-*.json"), manifest, definition);
    }

    public static void RequireReports(Measurement measurement)
    {
        var reports = measurement.RawReports ?? [];
        if (reports.Count(path => path.EndsWith("-report-full.json", StringComparison.Ordinal)) != 1 ||
            !reports.Any(path => Path.GetFileName(path).StartsWith("measured-process-", StringComparison.Ordinal)))
            throw new InvalidDataException($"Native report or measured-process provenance is missing: {measurement.CaseId}");
    }

    public static void ValidateEvidence(string directory, RunManifest manifest, Measurement measurement)
    {
        RequireReports(measurement);
        ValidateNativeReport(directory, measurement);
        var paths = measurement.RawReports!.Where(path => Path.GetFileName(path).StartsWith("measured-process-", StringComparison.Ordinal))
            .Select(path => BundleFiles.Resolve(directory, path)).ToArray();
        ValidateProcessFiles(paths, manifest, manifest.Cases.Single(item => item.Id == measurement.CaseId));
    }

    private static void ValidateNativeReport(string directory, Measurement measurement)
    {
        var native = measurement.RawReports!.Single(path => path.EndsWith("-report-full.json", StringComparison.Ordinal));
        var report = JsonNode.Parse(File.ReadAllText(BundleFiles.Resolve(directory, native)));
        if (report is not JsonObject root || root["Benchmarks"] is not JsonArray { Count: 1 } benchmarks ||
            benchmarks[0] is not JsonObject benchmark || benchmark["Statistics"] is not JsonObject)
            throw new InvalidDataException("Successful native report has no benchmark statistics.");
    }

    private static void ValidateProcessFiles(string[] paths, RunManifest manifest, CaseDefinition definition)
    {
        if (paths.Length == 0)
            throw new InvalidDataException("Measured-process provenance is missing.");
        foreach (var path in paths)
        {
            var stamp = ArtifactJson.Read<MeasuredProcessStamp>(path);
            ValidateProcess(stamp, manifest.Provenance);
            if (stamp.Source is null || !CampaignComparison.SameSource(stamp.Source, manifest.Provenance.Source))
                throw new InvalidDataException("Measured process source differs from the campaign.");
            ValidateQuery(stamp, definition);
        }
    }

    public static void ValidateQuery(MeasuredProcessStamp process, CaseDefinition definition)
    {
        if (process.ScenarioId != definition.ScenarioId || process.Sql != definition.Sql || process.InputCount != definition.InputCount)
            throw new InvalidDataException("Measured SQL or input count differs from the campaign case.");
    }

    public static void ValidateProcess(MeasuredProcessStamp process, RunProvenance expected)
    {
        if (process.Runtime != expected.Environment.Runtime || process.EffectiveProcessors != expected.Hardware.EffectiveProcessors ||
            process.ServerGc != expected.Environment.ServerGc)
            throw new InvalidDataException("Measured process runtime, processors or GC differ from the campaign.");
        if (process.EngineInformationalVersion != expected.Build.EngineInformationalVersion ||
            process.SuiteInformationalVersion != expected.Build.SuiteInformationalVersion)
            throw new InvalidDataException("Measured process informational revisions differ from the launcher.");
    }
}
