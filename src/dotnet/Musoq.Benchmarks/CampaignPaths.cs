namespace Musoq.Benchmarks;

public static class CampaignPaths
{
    public const int MaximumRelativeLength = 180;
    public const int MaximumAbsoluteLength = 240;
    private const int AtomicWriteSuffixLength = 37;
    private const string ExampleId = "00000000000000000000000000000000";

    public static string RawDirectory(string campaign, string attemptId) => Path.Combine(campaign, "raw", attemptId);

    public static void Validate(string repository, string campaign, IReadOnlyList<CampaignCase> cases)
    {
        string[] fixedPaths = ["definition.json", "manifest.json", "measurements.json", "summary.md", "complete.json",
            $"attempts/{ExampleId}/start.json", $"attempts/{ExampleId}/result.json",
            $"sessions/{ExampleId}.json", $"raw/preflight/{ExampleId}.json",
            $"raw/{ExampleId}/measured-process-2147483647-{ExampleId}.json"];
        foreach (var relative in fixedPaths)
            ValidateFile(repository, Path.Combine(campaign, relative));
        foreach (var type in cases.Select(item => item.Benchmark.Descriptor.Type).Distinct())
            ValidateFile(repository, Path.Combine(campaign, "raw", ExampleId, type.FullName + "-report-full.json"));
    }

    public static void ValidateFile(string repository, string path)
    {
        var fullPath = Path.GetFullPath(path);
        if (fullPath.Length + AtomicWriteSuffixLength > MaximumAbsoluteLength)
            throw new ArgumentException($"Artifact path exceeds the {MaximumAbsoluteLength}-character absolute budget including temporary writes: {fullPath}. Choose a shorter output or checkout location.");
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(repository)) + Path.DirectorySeparatorChar;
        var comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (fullPath.StartsWith(root, comparison) && Path.GetRelativePath(root, fullPath).Length > MaximumRelativeLength)
            throw new ArgumentException($"Artifact path exceeds the {MaximumRelativeLength}-character repository-relative budget: {fullPath}. Choose a shorter output or checkout location.");
    }
}
