namespace Musoq.Benchmarks;

public sealed record CampaignOptions(string Mode = "Local", string Job = "Short", string Filter = "*",
    string? MachineProfile = null, string? Output = null, string? Resume = null,
    bool InProcess = false, int? Size = null, string Action = "Run", bool Json = false,
    string? RunDirectory = null, string? HistoryRoot = null)
{
    [System.Text.Json.Serialization.JsonIgnore]
    public string[] Supplied { get; init; } = [];
    public bool Official => Mode == "Official";
    public bool Inspecting => Action is "Status" or "Verify" or "Latest";

    public static CampaignOptions Parse(string[] arguments)
    {
        var options = new CampaignOptions { Supplied = arguments.Where(value => value.StartsWith("--", StringComparison.Ordinal)).ToArray() };
        for (var index = 0; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            if (argument is "--in-process" or "--json")
            {
                options = argument switch
                {
                    "--json" => options with { Json = true },
                    _ => options with { InProcess = true }
                };
                continue;
            }
            if (++index >= arguments.Length)
                throw new ArgumentException($"Missing value for {argument}.");
            options = SetValue(options, argument, arguments[index]);
        }
        if (options.Resume != null && options.Output != null)
            throw new ArgumentException("--resume and --output cannot be combined.");
        options = RestoreInvocation(options);
        Validate(options);
        return options;
    }

    private static CampaignOptions SetValue(CampaignOptions options, string argument, string value) => argument switch
    {
        "--mode" => options with { Mode = Canonical(value, "Local", "Official") },
        "--job" => options with { Job = Canonical(value, "Short", "Dry", "Default") },
        "--action" => options with { Action = Canonical(value, "Run", "List", "Status", "Verify", "Latest") },
        "--size" => options with { Size = BenchmarkSizes.Parse(value) },
        "--filter" => options with { Filter = value },
        "--machine-profile" => options with { MachineProfile = value },
        "--output" => options with { Output = value },
        "--resume" => options with { Resume = value },
        "--run-directory" => options with { RunDirectory = value },
        "--history-root" => options with { HistoryRoot = value },
        _ => throw new ArgumentException($"Unknown option {argument}.")
    };

    private static void Validate(CampaignOptions options)
    {
        if (options.Inspecting)
        {
            if (options.Action != "Latest" && options.RunDirectory is null)
                throw new ArgumentException("Status and Verify require --run-directory.");
            if (options.Resume is not null || options.Output is not null || options.Size is not null ||
                options.Filter != "*" || options.InProcess || options.MachineProfile is not null ||
                options.Supplied.Contains("--mode") || options.Supplied.Contains("--job"))
                throw new ArgumentException("Inspection actions do not accept measurement options.");
            if (options.Action == "Latest" && options.RunDirectory is not null ||
                options.Action != "Latest" && options.HistoryRoot is not null)
                throw new ArgumentException("Latest accepts --history-root; Status and Verify accept --run-directory.");
            return;
        }
        if (options.RunDirectory is not null || options.HistoryRoot is not null)
            throw new ArgumentException("--run-directory and --history-root are inspection options.");
        if (options.Official && (options.Filter != "*" || options.InProcess || options.Job != "Short" || options.Size != null))
            throw new ArgumentException("Official runs use the complete fixed out-of-process profile; filters, sizes and jobs are local only.");
        if (options.Official && options.Resume == null && string.IsNullOrWhiteSpace(options.MachineProfile))
            throw new ArgumentException("Official runs require --machine-profile.");
        if (options.InProcess && options.Job != "Dry")
            throw new ArgumentException("In-process execution is reserved for local Dry smoke checks.");
    }

    private static CampaignOptions RestoreInvocation(CampaignOptions options)
    {
        if (options.Resume is null)
            return options;
        if (options.Action != "Run")
            throw new ArgumentException("--resume is a Run option.");
        var saved = CampaignStore.Definition(options.Resume).Invocation
            ?? throw new InvalidDataException("Saved invocation is missing; start a new campaign.");
        var effective = saved with { Resume = options.Resume, Output = null, Json = options.Json, Supplied = options.Supplied };
        foreach (var argument in options.Supplied.Where(value => value is not ("--resume" or "--json")))
        {
            if (!SameOverride(argument, options, effective))
                throw new CampaignCompatibilityException($"Resume cannot override the saved invocation: {argument}.");
        }
        return effective;
    }

    private static bool SameOverride(string argument, CampaignOptions supplied, CampaignOptions saved) => argument switch
    {
        "--mode" => supplied.Mode == saved.Mode,
        "--job" => supplied.Job == saved.Job,
        "--filter" => supplied.Filter == saved.Filter,
        "--size" => supplied.Size == saved.Size,
        "--machine-profile" => supplied.MachineProfile == saved.MachineProfile,
        "--in-process" => supplied.InProcess == saved.InProcess,
        "--action" => supplied.Action == saved.Action,
        _ => false
    };

    private static string Canonical(string value, params string[] allowed) => allowed
        .FirstOrDefault(candidate => candidate.Equals(value, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Expected one of {string.Join(", ", allowed)}; received '{value}'.");
}

public sealed class CampaignCompatibilityException(string message) : InvalidOperationException(message);
