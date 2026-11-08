using System;
using System.Collections.Generic;

namespace Musoq.Evaluator.Tests.Schema.RecursiveCte;

public sealed record RecursiveGraphRoot(int RootId);

public sealed class RecursiveGraphEdge(
    int sourceId,
    int targetId,
    decimal weight,
    string? label)
{
    public int SourceId { get; set; } = sourceId;

    public int TargetId { get; set; } = targetId;

    public decimal Weight { get; set; } = weight;

    public string? Label { get; set; } = label;
}

public sealed class RecursiveGraphData
{
    public IReadOnlyList<RecursiveGraphRoot> Roots { get; init; } = [];

    public IReadOnlyList<RecursiveGraphEdge> Edges { get; init; } = [];

    public Func<IReadOnlyList<RecursiveGraphRoot>>? RootsFactory { get; init; }

    public Func<IReadOnlyList<RecursiveGraphEdge>>? EdgesFactory { get; init; }

    public Func<IEnumerable<IReadOnlyList<RecursiveGraphEdge>>>? EdgeChunksFactory { get; init; }

    public Action? EdgesEnumerationCompleted { get; init; }

    internal IReadOnlyList<RecursiveGraphRoot> GetRoots() => RootsFactory?.Invoke() ?? Roots;

    internal IReadOnlyList<RecursiveGraphEdge> GetEdges() => EdgesFactory?.Invoke() ?? Edges;

    internal IEnumerable<IReadOnlyList<RecursiveGraphEdge>> GetEdgeChunks() =>
        EdgeChunksFactory?.Invoke() ?? [GetEdges()];

    internal IReadOnlyList<RecursiveGraphEdge> GetNeighbors(int sourceId)
    {
        var edges = GetEdges();
        var result = new List<RecursiveGraphEdge>();

        foreach (var edge in edges)
            if (edge.SourceId == sourceId)
                result.Add(edge);

        return result;
    }
}
