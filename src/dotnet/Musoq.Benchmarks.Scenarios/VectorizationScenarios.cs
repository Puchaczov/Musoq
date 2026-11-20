using System.Globalization;
using Musoq.Schema.StructuralInputs;

namespace Musoq.Benchmarks.Scenarios;

/// <summary>Opt-in query workloads for measuring internal execution strategies.</summary>
public static class VectorizationScenarios
{
    public const string Prefix = "V_";
    public static IReadOnlyList<Scenario> All { get; } = Create().AsReadOnly();

    private static List<Scenario> Create()
    {
        var cases = new List<Scenario>();
        foreach (var size in new[] { 0, 1, 4, 32, 128 })
        foreach (var hits in new[] { 1, 50 })
            cases.Add(
                new Scenario(
                    $"V_In{size}_{hits}",
                    "Membership",
                    "Filter by a captured integer allowlist and sort matching rows.",
                    "param(ids: int[]) select r.StringKey as Name, r.Id as Id from perf.rows() r where r.Partition in $ids order by r.StringKey",
                    "core/7",
                    FixtureId: "vectorization",
                    DataProfileId: hits == 1 ? "rare" : "frequent"
                )
                {
                    MembershipIds = Array.AsReadOnly(Enumerable.Range(0, size).ToArray()),
                }
            );
        foreach (var width in new[] { 8, 64, 1024 })
            cases.Add(
                new(
                    $"V_Tag{width}",
                    "Interpretation",
                    "Parse a nested bracket field and consume its contents.",
                    "text Entry { Tag: between '[' ']' nested }; select r.Id as Id, p.Tag as Tag from perf.rows() r cross apply Parse<Entry>(r.Text) p",
                    "binary-text/5",
                    Ordered: false,
                    FixtureId: "vectorization",
                    DataProfileId: $"tag{width}"
                )
            );
        cases.Add(ScenarioCatalog.Get("B16_RowsFrame") with { Id = "V_Window" });
        const string filter = "r.Partition >= 2 and r.Partition < 16";
        const string projection = "r.Id as Id, r.Partition * 2 + 1 as Score";
        const string counts =
            "Count(*) filter (where r.Partition >= 2) as Eligible, Count(*) filter (where r.Partition < 16) as Recent";
        const string groups =
            "r.Bucket as Bucket, " + counts + ", Min(r.Partition) as Minimum, Max(r.Partition) as Maximum";
        const string stored =
            "with joined as (select a.Id as Id, b.Partition as Partition, a.Bucket as Bucket from perf.rows() a inner join perf.right() b on a.Id = b.Id) ";
        foreach (var (input, prefix, source) in new[] { ("Stream", "", "perf.rows()"), ("Stored", stored, "joined") })
        {
            cases.Add(Analytics(input + "Project", prefix + $"select {projection} from {source} r where {filter}"));
            cases.Add(Analytics(input + "Counts", prefix + $"select {counts} from {source} r"));
            cases.Add(Analytics(input + "Groups", prefix + $"select {groups} from {source} r group by r.Bucket"));
            cases.Add(
                Analytics(
                    input + "Extrema",
                    prefix + $"select Min(r.Partition) as Minimum, Max(r.Partition) as Maximum from {source} r"
                )
            );
            var withNullableKey = prefix.Replace("a.Bucket as Bucket", "b.Subkey as Subkey", StringComparison.Ordinal);
            cases.Add(
                Analytics(
                    input + "NullableExtrema",
                    withNullableKey + $"select Min(r.Subkey) as Minimum, Max(r.Subkey) as Maximum from {source} r"
                )
            );
        }
        cases.AddRange(NumericBatchWorkloads.Cases());
        return cases;
    }

    private static Scenario Analytics(string name, string sql) =>
        new(
            Prefix + name,
            "Analytics",
            "Measure primitive filtering, projection or aggregation.",
            sql,
            "core/7,10",
            Ordered: false,
            FixtureId: "vectorization"
        );

    internal static FixtureRequirements Requirements(Scenario scenario, int count) =>
        new(scenario.ValidatePrimitiveTypes, null, StructuralInputLimits.Default);

    internal static string Distribution(Scenario scenario) =>
        "Id=0..N-1, right rows shuffled with seed 1729, Bucket=Id%4, Partition=Id%64; "
        + (
            scenario.MembershipIds is { Count: var size }
                ? $"membership profile {scenario.DataProfileId}, {size} keys, deterministic round-robin hits and -1 misses."
                : $"profile {scenario.DataProfileId}, nested tag every fourth row."
        );

    internal static PreparedFixture Prepare(Scenario scenario, int count)
    {
        var rows = new PerformanceRow[count];
        var width = scenario.DataProfileId switch
        {
            "tag8" => 8,
            "tag64" => 64,
            "tag1024" => 1024,
            _ => 0,
        };
        var content = new string('x', width);
        var plain = "[" + content + "]";
        var nested = "[" + content + "[y]]";
        var hits = scenario.DataProfileId == "frequent" ? 50 : 1;
        for (var index = 0; index < rows.Length; index++)
        {
            var partition = index % 64;
            if (scenario.MembershipIds is { } keys)
                partition =
                    keys.Count > 0 && index % 100 < hits
                        ? (int)(((long)(index / 100) * hits + index % 100) % keys.Count)
                        : -1;
            rows[index] = new(
                index,
                index.ToString("D8", CultureInfo.InvariantCulture),
                partition,
                index % 5 == 0 ? null : index,
                index + 1m,
                string.Empty,
                index % 4,
                index % 4 == 0 ? nested : plain,
                [],
                []
            );
        }
        var right = rows.ToArray();
        StandardDataProfiles.Shuffle(right);
        return new(
            scenario,
            count,
            Requirements(scenario, count),
            () => new PerformanceProvider(rows, right, "scalar-serial")
        );
    }
}
