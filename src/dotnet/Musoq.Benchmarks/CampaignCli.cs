namespace Musoq.Benchmarks;

public static class CampaignCli
{
    public static int Run(string[] arguments)
    {
        var output = Console.Out;
        var json = arguments.Contains("--json", StringComparer.Ordinal);
        try
        {
            if (arguments is ["--compare-readme", var scriptPath])
            {
                ReadmeComparison.Write(scriptPath, output);
                return 0;
            }
            if (arguments.Length == 0 || arguments.Contains("--help", StringComparer.Ordinal))
            {
                Console.WriteLine(
                    "--action Run|List|Status|Verify|Latest --mode Local|Official --filter <glob> --size <count> --job Short|Dry|Default --machine-profile <name> --output <directory> --resume <directory> --run-directory <directory> --history-root <directory> --json --include-retired (Local measurement or List) --in-process (local Dry only); --compare-readme <script.sql>"
                );
                Console.WriteLine("--action SnapshotDiff --baseline-ref <ref> [--candidate-patch <path>] --json");
                Console.WriteLine("--action Compare --comparison-input <path> --json");
                return 0;
            }
            if (
                arguments is ["--action", var action, .. var supplied]
                && action.ToLowerInvariant() is "snapshotdiff" or "compare"
            )
                return InspectRegression(action, supplied.Where(value => value != "--json").ToArray(), output);
            return Dispatch(CampaignOptions.Parse(arguments), output);
        }
        catch (Exception exception)
        {
            var code = exception switch
            {
                CampaignCompatibilityException => 3,
                InvalidDataException or System.Text.Json.JsonException or IOException => 4,
                ArgumentException => 5,
                _ => 1,
            };
            if (json)
                Write(
                    output,
                    new
                    {
                        Outcome = "error",
                        ExitCode = code,
                        Error = exception.Message,
                    }
                );
            else
                Console.Error.WriteLine(exception);
            return code;
        }
        finally
        {
            Console.SetOut(output);
        }
    }

    private static int InspectRegression(string action, string[] arguments, TextWriter output)
    {
        switch (action.ToLowerInvariant(), arguments)
        {
            case ("snapshotdiff", ["--baseline-ref", var baseline]):
                Write(output, BenchmarkSnapshotChanges.Inspect(ProvenanceCollector.RepositoryRoot(), baseline, null));
                return 0;
            case ("snapshotdiff", ["--baseline-ref", var baseline, "--candidate-patch", var patch]):
                Write(output, BenchmarkSnapshotChanges.Inspect(ProvenanceCollector.RepositoryRoot(), baseline, patch));
                return 0;
            case ("compare", ["--comparison-input", var input]):
                Write(output, BenchmarkRegressionComparison.Read(input));
                return 0;
            default:
                throw new ArgumentException(
                    "SnapshotDiff requires --baseline-ref and optionally --candidate-patch. Compare requires --comparison-input."
                );
        }
    }

    private static int Dispatch(CampaignOptions options, TextWriter output)
    {
        if (options.Action == "List")
            return ListCases(options, output);
        if (options.Action == "Latest")
        {
            Write(
                output,
                CampaignInspection.Latest(
                    options.HistoryRoot ?? Path.Combine(ProvenanceCollector.RepositoryRoot(), "benchmarks", "history")
                )
            );
            return 0;
        }
        CampaignReceipt receipt;
        if (options.Inspecting)
            receipt = options.Action switch
            {
                "Status" => CampaignInspection.Status(options.RunDirectory!),
                _ => CampaignInspection.Verify(options.RunDirectory!),
            };
        else
        {
            if (options.Json)
                Console.SetOut(Console.Error);
            receipt = CampaignRunner.RunCampaign(options);
        }
        Write(output, receipt);
        return receipt.Status == "complete" ? 0 : 2;
    }

    private static void Write<T>(TextWriter output, T value) =>
        output.WriteLine(System.Text.Json.JsonSerializer.Serialize(value, ArtifactJson.Options));

    private static int ListCases(CampaignOptions options, TextWriter output)
    {
        var cases = CampaignCases.Create(options, Path.GetTempPath());
        if (cases.Length == 0)
            throw new ArgumentException("No benchmark cases match the filter.");
        if (options.IncludeRetired)
        {
            var canonical = CampaignCases.CreateComplete(options with { Size = null }, Path.GetTempPath());
            var catalog = CaseLifecycleCatalog.Load(canonical.Select(item => item.Definition.Id));
            var listings = cases
                .Select(item =>
                {
                    var retired = catalog.Retirement(item.Definition.Id);
                    return new CaseListing(
                        item.Definition,
                        retired is null ? "active" : "retired",
                        retired?.Reason,
                        retired?.ReplacementCaseId
                    );
                })
                .ToArray();
            if (options.Json)
                Write(output, listings);
            else
                foreach (var listing in listings)
                    output.WriteLine($"{listing.Definition.Id}: {listing.Status}; {listing.Reason}");
            return 0;
        }
        if (options.Json)
            Write(output, cases.Select(item => item.Definition).ToArray());
        else
        {
            foreach (var item in cases)
                output.WriteLine(
                    $"{item.Definition.Id}: {item.Definition.InputCount} {item.Definition.InputUnit}; {item.Definition.Phase}; {item.Definition.ResultApi}"
                );
        }
        return 0;
    }
}
