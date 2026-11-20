using Musoq.Schema.StructuralInputs;

namespace Musoq.Benchmarks.Scenarios;

public static class ExecutionPlanScenarios
{
    public const string Prefix = "EP_";
    private const string Quantified =
        "select a.Id as Id from perf.rows() a where a.Amount > all (select b.Amount from perf.right() b where b.Bucket = a.Bucket)";
    private const string RangeJoin =
        "select a.Id as LeftId, b.Id as RightId from perf.rows() a full outer join perf.right() b on a.Amount < b.Amount";
    private const string Frame =
        "select r.Id as Id, Sum(r.Subkey) over (order by r.Subkey range between 2 preceding and 1 following) as Total, "
        + "Count(r.Subkey) over (order by r.Subkey range between 2 preceding and 1 following) as Present from perf.rows() r order by r.Id";
    private const string Rank =
        "select r.Id as Id, RowNumber() over (partition by r.Bucket order by r.Amount desc) as Ordinal from perf.rows() r qualify Ordinal <= 2";

    public static IReadOnlyList<Scenario> All { get; } =
        Array.AsReadOnly(
            new[]
            {
                Case("AllPass", Quantified, "All outer amounts exceed every inner amount in one group.", "core/7"),
                Case("AllFirstFails", Quantified, "The first amount in the group disproves each comparison.", "core/7"),
                Case(
                    "JoinSparse",
                    RangeJoin,
                    "Two disjoint ranges yield only unmatched rows, N on either side.",
                    "core/8"
                ),
                Case("JoinDense", RangeJoin, "Each of N observations matches eight fixed upper thresholds.", "core/8"),
                Case("FramePeers", Frame, "All observations share the same ordering value.", "core/11"),
                Case(
                    "FrameSpaced",
                    Frame,
                    "Ordering values are four apart and each frame contains one observation.",
                    "core/11"
                ),
                Case("RankOne", Rank, "Two highest amounts in one shuffled group.", "core/11"),
                Case("RankMany", Rank, "Two highest amounts per group of two shuffled observations.", "core/11"),
            }
        );

    private static Scenario Case(string profile, string sql, string description, string specification) =>
        new(
            Prefix + profile,
            "Execution plans",
            description,
            sql,
            specification,
            Ordered: sql == Frame,
            FixtureId: "execution-plans",
            DataProfileId: profile
        );

    internal static FixtureRequirements Requirements(Scenario scenario, int count) =>
        new(scenario.ValidatePrimitiveTypes, null, StructuralInputLimits.Default);

    internal static PreparedFixture Prepare(Scenario scenario, int count)
    {
        var profile = scenario.DataProfileId;
        var rows = Enumerable.Range(0, count).Select(id => Row(id, profile)).ToArray();
        var right = profile switch
        {
            "JoinDense" => Enumerable.Range(0, 8).Select(id => Row(id, "Right")).ToArray(),
            "AllPass" or "AllFirstFails" or "JoinSparse" => Enumerable
                .Range(0, count)
                .Select(id => Row(id, "Right"))
                .ToArray(),
            _ => Array.Empty<PerformanceRow>(),
        };
        if (profile is "RankOne" or "RankMany")
            StandardDataProfiles.Shuffle(rows);
        return new(
            scenario,
            count,
            Requirements(scenario, count),
            () => new PerformanceProvider(rows, right, "scalar-serial")
        );
    }

    private static PerformanceRow Row(int id, string profile)
    {
        var amount = profile switch
        {
            "Right" => 1,
            "AllPass" or "JoinSparse" => 2,
            "RankOne" or "RankMany" => id,
            _ => 0,
        };
        return new(
            id,
            "",
            0,
            profile == "FrameSpaced" ? checked(4 * id) : 1,
            amount,
            "",
            profile == "RankMany" ? id / 2 : 0,
            "",
            [],
            []
        );
    }
}
