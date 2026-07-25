namespace Musoq.Benchmarks;

public enum RecursiveCteBenchmarkScenario
{
    Chain,
    Tree,
    Diamond,
    Cycle,
    DuplicateHeavyKeyed,
    WideRows,
    InvariantSnapshot,
    IndexedEdges,
    CorrelatedApply,
    EmptyAnchor
}

internal sealed record RecursiveCteBenchmarkFixture(
    RecursiveCteBenchmarkScenario Scenario,
    RecursiveCteBenchmarkEdge[] Edges,
    string Query,
    bool UsesIdentity,
    bool IsWide,
    bool IsCorrelated,
    bool HasAnchor)
{
    public static RecursiveCteBenchmarkFixture Create(RecursiveCteBenchmarkScenario scenario, int scale)
    {
        if (scale < 4)
            throw new ArgumentOutOfRangeException(nameof(scale));

        var edges = scenario switch
        {
            RecursiveCteBenchmarkScenario.Tree => CreateTree(scale),
            RecursiveCteBenchmarkScenario.Diamond => CreateDiamonds(scale),
            RecursiveCteBenchmarkScenario.Cycle => CreateCycle(scale),
            RecursiveCteBenchmarkScenario.DuplicateHeavyKeyed => CreateDuplicateHeavyChain(scale),
            _ => CreateChain(scale)
        };

        return new RecursiveCteBenchmarkFixture(
            scenario,
            edges,
            RecursiveCteBenchmarkQueries.Create(scenario),
            scenario is not RecursiveCteBenchmarkScenario.Chain and
                not RecursiveCteBenchmarkScenario.Tree and
                not RecursiveCteBenchmarkScenario.EmptyAnchor,
            scenario == RecursiveCteBenchmarkScenario.WideRows,
            scenario == RecursiveCteBenchmarkScenario.CorrelatedApply,
            scenario != RecursiveCteBenchmarkScenario.EmptyAnchor);
    }

    private static RecursiveCteBenchmarkEdge[] CreateChain(int scale) =>
        Enumerable.Range(1, scale - 1)
            .Select(source => Edge(source, source + 1))
            .ToArray();

    private static RecursiveCteBenchmarkEdge[] CreateTree(int scale)
    {
        var edges = new List<RecursiveCteBenchmarkEdge>(scale - 1);
        for (var source = 1; source <= scale; source++)
        {
            var left = source * 2;
            if (left <= scale)
                edges.Add(Edge(source, left));
            if (left + 1 <= scale)
                edges.Add(Edge(source, left + 1));
        }

        return edges.ToArray();
    }

    private static RecursiveCteBenchmarkEdge[] CreateDiamonds(int scale)
    {
        var edges = new List<RecursiveCteBenchmarkEdge>(scale + scale / 3);
        var source = 1;
        var next = 2;
        while (next + 2 <= scale)
        {
            var left = next++;
            var right = next++;
            var merge = next++;
            edges.Add(Edge(source, left));
            edges.Add(Edge(source, right));
            edges.Add(Edge(left, merge));
            edges.Add(Edge(right, merge));
            source = merge;
        }

        return edges.ToArray();
    }

    private static RecursiveCteBenchmarkEdge[] CreateCycle(int scale)
    {
        var edges = CreateChain(scale).ToList();
        edges.Add(Edge(scale, 1));
        return edges.ToArray();
    }

    private static RecursiveCteBenchmarkEdge[] CreateDuplicateHeavyChain(int scale)
    {
        var edges = new List<RecursiveCteBenchmarkEdge>((scale - 1) * 8);
        for (var source = 1; source < scale; source++)
        for (var duplicate = 0; duplicate < 8; duplicate++)
            edges.Add(Edge(source, source + 1, duplicate));
        return edges.ToArray();
    }

    private static RecursiveCteBenchmarkEdge Edge(int source, int target, int duplicate = 0) =>
        new(source, target, 1, $"e{source}_{target}_{duplicate}");
}
