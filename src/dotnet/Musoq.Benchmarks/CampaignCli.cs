namespace Musoq.Benchmarks;

public static class CampaignCli
{
    public static int Run(string[] arguments)
    {
        var output = Console.Out;
        var json = arguments.Contains("--json", StringComparer.Ordinal);
        try
        {
            if (arguments.Length == 0 || arguments.Contains("--help", StringComparer.Ordinal))
            {
                Console.WriteLine("--action Run|List|Status|Verify|Latest --mode Local|Official --filter <glob> --size <count> --job Short|Dry|Default --machine-profile <name> --output <directory> --resume <directory> --run-directory <directory> --history-root <directory> --json --in-process (local Dry only)");
                return 0;
            }
            return Dispatch(CampaignOptions.Parse(arguments), output);
        }
        catch (Exception exception)
        {
            var code = exception switch
            {
                CampaignCompatibilityException => 3,
                InvalidDataException or System.Text.Json.JsonException or IOException => 4,
                ArgumentException => 5,
                _ => 1
            };
            if (json)
                Write(output, new { Outcome = "error", ExitCode = code, Error = exception.Message });
            else
                Console.Error.WriteLine(exception);
            return code;
        }
        finally { Console.SetOut(output); }
    }

    private static int Dispatch(CampaignOptions options, TextWriter output)
    {
        if (options.Action == "List")
            return ListCases(options, output);
        if (options.Action == "Latest")
        {
            Write(output, CampaignInspection.Latest(options.HistoryRoot ?? Path.Combine(ProvenanceCollector.RepositoryRoot(), "benchmarks", "history")));
            return 0;
        }
        CampaignReceipt receipt;
        if (options.Inspecting)
            receipt = options.Action switch
            {
                "Status" => CampaignInspection.Status(options.RunDirectory!),
                _ => CampaignInspection.Verify(options.RunDirectory!)
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

    private static void Write<T>(TextWriter output, T value) => output.WriteLine(System.Text.Json.JsonSerializer.Serialize(value, ArtifactJson.Options));

    private static int ListCases(CampaignOptions options, TextWriter output)
    {
        var cases = CampaignCases.Create(options, Path.GetTempPath());
        if (cases.Length == 0)
            throw new ArgumentException("No benchmark cases match the filter.");
        if (options.Json)
            Write(output, cases.Select(item => item.Definition).ToArray());
        else
        {
            foreach (var item in cases)
                output.WriteLine($"{item.Definition.Id}: {item.Definition.InputCount} {item.Definition.InputUnit}; {item.Definition.Phase}; {item.Definition.ResultApi}");
        }
        return 0;
    }

}
