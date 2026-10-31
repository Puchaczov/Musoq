namespace Musoq.Benchmarks.Scenarios;

public static partial class ScenarioCatalog
{
    public const string SuiteVersion = "engine-v3";
    public static string PackageVersion => ScenarioBuildInfo.PackageVersion;
    public static string InformationalVersion => ScenarioBuildInfo.InformationalVersion;
    public static IReadOnlyList<string> CompilationIds { get; } = Array.AsReadOnly(new[]
    {
        "B01_Scan", "B01_Wide", "B14_Group", "B10_CorrelatedFallback",
        "B17_MultipleWindows", "B22_Recursive", "B28_BinarySubstream", "B31_TextCaptures"
    });
    public static IReadOnlyList<string> ArtifactIds { get; } = Array.AsReadOnly(new[] { "B14_Group", "B27_BinaryHeader" });

    public static IReadOnlyList<Scenario> All { get; } = Create().AsReadOnly();

    public static Scenario Get(string id) => All.Single(scenario => scenario.Id == id);

    private static List<Scenario> Create()
    {
        List<Scenario> scenarios =
        [
            new("B01_Scan", "Expressions", "Read a narrow projection from typed chunks.",
                "select r.Id as Id, r.Name as Name from bench.rows() r order by r.Id", "core/5,6"),
            new("B01_Wide", "Expressions", "Project 64 decimal expressions and an integer key.",
                "select r.Id as Id, " + string.Join(", ", Enumerable.Range(0, 64).Select(index => $"r.Amount + {index}d as C{index}")) +
                " from bench.rows() r order by r.Id", "core/5")
        ];
        scenarios.AddRange(Relational());
        scenarios.AddRange(Analytics());
        scenarios.AddRange(Interpretation());
        scenarios.AddRange(Composed());
        AddProfiles(scenarios);
        return scenarios;
    }
}
