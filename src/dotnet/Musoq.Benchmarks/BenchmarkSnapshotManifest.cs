using System.Text.Json;
using System.Text.RegularExpressions;

namespace Musoq.Benchmarks;

public sealed record BenchmarkSnapshotCase(CaseDefinition Definition, string File, string? OutputType);

public sealed record BenchmarkSnapshotManifest(string SuiteVersion, BenchmarkSnapshotCase[] Cases)
{
    public const string RelativeDirectory = "src/dotnet/generated-code-samples/benchmarks";
    public const string FileName = "manifest.json";

    public static BenchmarkSnapshotManifest Parse(string text)
    {
        var manifest =
            JsonSerializer.Deserialize<BenchmarkSnapshotManifest>(text, ArtifactJson.Options)
            ?? throw new InvalidDataException("The benchmark snapshot manifest is empty.");
        manifest.Validate();
        return manifest;
    }

    public static CaseDefinition PortableDefinition(CaseDefinition definition)
    {
        var arguments = new SortedDictionary<string, string>(definition.Arguments ?? [], StringComparer.Ordinal);
        arguments.Remove("maxDegreeOfParallelism");
        // SQL is already recorded in the snapshot's Parsed Query section.
        return definition with { Sql = "", JobSettings = "", Arguments = arguments };
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SuiteVersion) || Cases is null)
            throw new InvalidDataException("A benchmark snapshot manifest requires a suite version and case mappings.");
        var caseIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var files = new Dictionary<string, (string Scenario, string Api, bool Artifact)>(StringComparer.Ordinal);
        foreach (var item in Cases)
        {
            ValidateCase(item);
            if (!caseIds.Add(item.Definition.Id))
                throw new InvalidDataException($"Duplicate benchmark snapshot case '{item.Definition.Id}'.");
            var definition = item.Definition;
            var identity = (definition.ScenarioId, definition.ResultApi, definition.Phase == "artifact-load");
            if (!files.TryAdd(item.File, identity) && files[item.File] != identity)
                throw new InvalidDataException($"Unrelated benchmark cases share snapshot '{item.File}'.");
        }
        if (files.Keys.Distinct(StringComparer.OrdinalIgnoreCase).Count() != files.Count)
            throw new InvalidDataException(
                "Benchmark snapshot filenames must be unique on case-insensitive filesystems."
            );
    }

    private static void ValidateCase(BenchmarkSnapshotCase item)
    {
        if (
            item?.Definition is not { } definition
            || string.IsNullOrWhiteSpace(definition.Id)
            || string.IsNullOrWhiteSpace(definition.ScenarioId)
            || !IsSnapshotFile(item.File)
            || !item.File.StartsWith(definition.ScenarioId + "_Benchmark", StringComparison.Ordinal)
        )
            throw new InvalidDataException(
                "A benchmark snapshot mapping requires a canonical case ID and snapshot filename."
            );
    }

    private static bool IsSnapshotFile(string? file) =>
        file is not null && Regex.IsMatch(file, @"\A[A-Za-z0-9_-]+\.cs\z", RegexOptions.CultureInvariant);
}

public sealed record BenchmarkSnapshotSections(
    string Sql,
    string LogicalPlan,
    string PhysicalPlan,
    string ExecutionIr,
    string GeneratedCSharp
)
{
    public static BenchmarkSnapshotSections Parse(string text)
    {
        var remaining = text.ReplaceLineEndings("\n").TrimStart('\uFEFF');
        var sql = ReadBlock(ref remaining, "Parsed Query");
        var logical = ReadBlock(ref remaining, "Logical Plan");
        var physical = ReadBlock(ref remaining, "Physical Plan");
        var execution = ReadBlock(ref remaining, "Execution Plan");
        const string generatedMarker = "// === Generated C# ===\n\n";
        if (!remaining.StartsWith(generatedMarker, StringComparison.Ordinal))
            throw new InvalidDataException("Missing benchmark snapshot section 'Generated C#'.");
        var generated = remaining[generatedMarker.Length..].TrimEnd();
        if (generated.Length == 0)
            throw new InvalidDataException("Empty benchmark snapshot section 'Generated C#'.");
        return new(sql, logical, physical, execution, generated);
    }

    internal string[] ChangedSections(BenchmarkSnapshotSections candidate)
    {
        var sections = new List<string>();
        if (LogicalPlan != candidate.LogicalPlan)
            sections.Add("Logical Plan");
        if (PhysicalPlan != candidate.PhysicalPlan)
            sections.Add("Physical Plan");
        if (ExecutionIr != candidate.ExecutionIr)
            sections.Add("Execution IR");
        if (GeneratedCSharp != candidate.GeneratedCSharp)
            sections.Add("Generated C#");
        return sections.ToArray();
    }

    private static string ReadBlock(ref string remaining, string section)
    {
        var opening = $"// === {section} ===\n/*\n";
        const string closing = "\n*/\n\n";
        if (!remaining.StartsWith(opening, StringComparison.Ordinal))
            throw new InvalidDataException($"Missing benchmark snapshot section '{section}'.");
        var end = remaining.IndexOf(closing, opening.Length, StringComparison.Ordinal);
        if (end <= opening.Length)
            throw new InvalidDataException($"Empty or unclosed benchmark snapshot section '{section}'.");
        var text = remaining[opening.Length..end];
        remaining = remaining[(end + closing.Length)..];
        return text;
    }
}
