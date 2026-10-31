using Musoq.Benchmarks.Scenarios;

namespace Musoq.Benchmarks;

public static class CampaignRunner
{
    internal static CampaignReceipt RunCampaign(CampaignOptions options)
    {
        var root = ProvenanceCollector.RepositoryRoot();
        var work = Path.Combine(root, "BenchmarkDotNet.Artifacts", "work", Guid.NewGuid().ToString("N"));
        var cases = CampaignCases.Create(options, work);
        if (cases.Length == 0)
            throw new ArgumentException("No benchmark cases match the filter.");
        var provenance = ProvenanceCollector.Collect(root, options);
        ValidateOfficial(options, provenance);
        var runId = $"{DateTimeOffset.UtcNow:yyyyMMddTHHmmssfffZ}-{provenance.Source.Commit[..12]}";
        var manifest = new RunManifest
        {
            SuiteVersion = ScenarioCatalog.SuiteVersion, RunId = runId,
            Provenance = provenance, Cases = cases.Select(item => item.Definition).ToArray(),
            Invocation = options with { Resume = null, Output = null, Supplied = [] }
        };
        var output = options.Output ?? DefaultOutput(root, options, manifest);
        CampaignPaths.Validate(root, options.Resume ?? output, cases);
        CampaignStore store;
        if (options.Resume == null)
            store = new CampaignStore(output, manifest);
        else
            store = CampaignStore.Resume(options.Resume, manifest);
        Console.WriteLine($"{options.Mode}: {cases.Length} cases; {provenance.Source.Branch ?? "detached"}@{provenance.Source.Commit}; {provenance.Hardware.CpuModel}");
        Console.WriteLine($"Results: {store.DirectoryPath}; working reports: {work}");
        ExecuteCampaign(root, options, store, cases);
        if (store.Manifest.Status == "complete")
            return CampaignInspection.Verify(store.DirectoryPath);
        return CampaignInspection.Status(store.DirectoryPath);
    }

    private static void ExecuteCampaign(string root, CampaignOptions options, CampaignStore store, CampaignCase[] cases)
    {
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        var session = new CampaignSession(process.Id, process.StartTime.ToUniversalTime(), Environment.MachineName,
            DateTimeOffset.UtcNow);
        var sessionPath = Path.Combine(store.DirectoryPath, "sessions", Guid.NewGuid().ToString("N") + ".json");
        ArtifactJson.Write(sessionPath, session);
        foreach (var note in store.RecoveryNotes)
            Console.WriteLine(note);
        using var cancelled = new CancellationTokenSource();
        ConsoleCancelEventHandler handler = (_, eventArgs) => { eventArgs.Cancel = true; cancelled.Cancel(); };
        Console.CancelKeyPress += handler;
        try
        {
            Preflight(root, options, store);
            MeasureCases(root, store, cases, cancelled.Token);
        }
        catch (Exception exception)
        {
            store.Manifest.LastError = exception.Message;
            Console.Error.WriteLine(exception);
        }
        finally
        {
            Console.CancelKeyPress -= handler;
            ArtifactJson.Write(sessionPath, session with { Status = "stopped" });
            store.Finish();
        }
        Console.WriteLine($"Campaign {store.Manifest.Status}: {store.DirectoryPath}");
    }

    private static void MeasureCases(string root, CampaignStore store, CampaignCase[] cases, CancellationToken cancellation)
    {
        foreach (var item in cases)
        {
            if (cancellation.IsCancellationRequested)
                break;
            if (store.Completed(item.Definition.Id))
                continue;
            Console.WriteLine($"[{store.Attempts.Count + 1}] {item.Definition.Id}");
            CampaignIntegrity.ValidateSource(root, store.Manifest);
            var attempt = store.Begin(item.Definition.Id);
            var measurement = BenchmarkMeasurement.Measure(item, store, attempt);
            CampaignIntegrity.ValidateSource(root, store.Manifest);
            store.Complete(attempt, measurement);
        }
    }

    private static void ValidateOfficial(CampaignOptions options, RunProvenance provenance)
    {
        if (!options.Official)
            return;
        CampaignIntegrity.ValidateOfficialBuild(provenance);
        if (provenance.Source.Dirty)
            throw new InvalidOperationException("Official runs require a clean source checkout.");
        if (provenance.Hardware.CpuModel == "unknown" || provenance.Hardware.MemoryBytes <= 0 || provenance.Hardware.LogicalCores <= 0)
            throw new InvalidOperationException("Official hardware metadata is incomplete.");
        if (Environment.GetEnvironmentVariable("MUSOQ_EVALUATOR_PERF_TELEMETRY") == "1")
            throw new InvalidOperationException("Disable compilation telemetry before official timing.");
    }

    private static void Preflight(string root, CampaignOptions options, CampaignStore store)
    {
        if (!options.Official)
        {
            store.Manifest.Preflight = "not-run-local";
            store.Flush();
            return;
        }
        store.Manifest.Preflight = "running";
        store.Flush();
        var test = ProcessCommand.Run(root, "dotnet", "test", "src/dotnet/Musoq.Benchmarks.Tests", "-c", "Release", "--no-build",
            "--nologo", "--verbosity", "quiet", "--logger", "console;verbosity=minimal");
        RecordTests(store, test);
        store.Manifest.Preflight = "passed";
        store.Flush();
    }

    public static void RecordTests(CampaignStore store, CommandResult result)
    {
        var evidence = Path.Combine(store.DirectoryPath, "raw", "preflight", Guid.NewGuid().ToString("N") + ".json");
        ArtifactJson.Write(evidence, result);
        store.Manifest.Preflight = "failed";
        store.Flush();
        if (result.ExitCode != 0)
            throw new InvalidOperationException("Small benchmark resultset tests failed; read raw/preflight/.");
    }

    private static string DefaultOutput(string root, CampaignOptions options, RunManifest manifest)
    {
        if (options.Official)
            return Path.Combine(root, "benchmarks", "history", manifest.SuiteVersion, manifest.RunId);
        return Path.Combine(root, "BenchmarkDotNet.Artifacts", "local", manifest.RunId);
    }
}
