namespace Musoq.Benchmarks;

internal static class RecursiveCteBenchmarkQueries
{
    public static string Create(RecursiveCteBenchmarkScenario scenario)
    {
        if (scenario == RecursiveCteBenchmarkScenario.WideRows)
            return WideRows;

        var anchor = scenario == RecursiveCteBenchmarkScenario.EmptyAnchor
            ? "select 1, 0 from values {{ Seed: 1 }} seed where seed.Seed < 0"
            : "select 1, 0 from values {{ Seed: 1 }} seed";
        var separator = scenario is RecursiveCteBenchmarkScenario.Chain or
            RecursiveCteBenchmarkScenario.Tree or
            RecursiveCteBenchmarkScenario.EmptyAnchor
                ? "union all"
                : "union (Id)";
        var memberSource = scenario switch
        {
            RecursiveCteBenchmarkScenario.InvariantSnapshot =>
                "from walk w cross join #graph.edges() e where e.SourceId = w.Id",
            RecursiveCteBenchmarkScenario.CorrelatedApply =>
                "from walk w cross apply #graph.neighbors(w.Id) e",
            _ => "from walk w inner join #graph.edges() e on e.SourceId = w.Id"
        };

        return $"with recursive walk (Id, Depth) as ({anchor} {separator} " +
               $"select e.TargetId, w.Depth + 1 {memberSource}) " +
               "select Id, Depth from walk";
    }

    private const string WideRows =
        "with recursive walk (Id, Depth, P1, P2, P3, P4, P5, P6) as (" +
        "select 1, 0, 10, 20, 30, 40, 50, 60 from values {{ Seed: 1 }} seed " +
        "union (Id) " +
        "select e.TargetId, w.Depth + 1, w.P1 + e.Weight, w.P2 + e.Weight, " +
        "w.P3 + e.Weight, w.P4 + e.Weight, w.P5 + e.Weight, w.P6 + e.Weight " +
        "from walk w inner join #graph.edges() e on e.SourceId = w.Id) " +
        "select Id, Depth, P1, P2, P3, P4, P5, P6 from walk";
}
