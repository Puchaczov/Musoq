using System;

namespace Musoq.Evaluator.Tests.Schema.RecursiveCte;

public static class RecursiveGraphFixtures
{
    public static RecursiveGraphSchemaProvider CreateChainProvider()
    {
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            Edges =
            [
                Edge(1, 2, "one-two"),
                Edge(2, 3, "two-three")
            ]
        });
    }

    public static RecursiveGraphSchemaProvider CreateCycleProvider()
    {
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            Edges =
            [
                Edge(1, 2, "one-two"),
                Edge(2, 3, "two-three"),
                Edge(3, 1, "three-one")
            ]
        });
    }

    public static RecursiveGraphSchemaProvider CreateDiamondProvider()
    {
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            Edges =
            [
                Edge(1, 2, "one-two"),
                Edge(1, 3, "one-three"),
                Edge(2, 4, "two-four"),
                Edge(3, 4, "three-four")
            ]
        });
    }

    public static RecursiveGraphSchemaProvider CreateEquivalentPayloadDiamondProvider()
    {
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            Edges =
            [
                Edge(1, 2, "one-two"),
                Edge(1, 3, "one-three"),
                Edge(2, 4, "to-four"),
                Edge(3, 4, "to-four")
            ]
        });
    }

    public static RecursiveGraphSchemaProvider CreateEmptyRootsProvider()
    {
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Edges = [Edge(1, 2, "unused")]
        });
    }

    public static RecursiveGraphSchemaProvider CreateTreeProvider()
    {
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            Edges =
            [
                Edge(1, 2, "one-two"),
                Edge(1, 3, "one-three"),
                Edge(2, 4, "two-four"),
                Edge(2, 5, "two-five")
            ]
        });
    }

    public static RecursiveGraphSchemaProvider CreateDuplicateEdgesProvider()
    {
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            Edges =
            [
                Edge(1, 2, "one-two"),
                Edge(1, 2, "one-two-duplicate"),
                Edge(2, 3, "two-three")
            ]
        });
    }

    public static RecursiveGraphSchemaProvider CreateDisconnectedProvider()
    {
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            Edges = [Edge(1, 2, "one-two"), Edge(9, 10, "disconnected")]
        });
    }

    public static RecursiveGraphSchemaProvider CreateMultipleRootsProvider()
    {
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1), new RecursiveGraphRoot(10)],
            Edges = [Edge(1, 2, "one-two"), Edge(10, 11, "ten-eleven")]
        });
    }

    public static RecursiveGraphSchemaProvider CreateSelfLoopProvider()
    {
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            Edges = [Edge(1, 1, "self")]
        });
    }

    public static RecursiveGraphSchemaProvider CreateEmptyEdgesProvider()
    {
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)]
        });
    }

    public static RecursiveGraphSchemaProvider CreateFailingEdgesProvider(bool emptyRoots = false)
    {
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = emptyRoots ? [] : [new RecursiveGraphRoot(1)],
            EdgesFactory = static () => throw new InvalidOperationException(
                "A dead or empty-frontier recursive edge source was opened.")
        });
    }

    public static RecursiveGraphSchemaProvider CreateMutableSnapshotProvider()
    {
        var mutableEdge = Edge(2, 3, "two-three");
        return new RecursiveGraphSchemaProvider(new RecursiveGraphData
        {
            Roots = [new RecursiveGraphRoot(1)],
            Edges =
            [
                Edge(1, 2, "one-two"),
                mutableEdge
            ],
            EdgesEnumerationCompleted = () => mutableEdge.TargetId = 99
        });
    }

    private static RecursiveGraphEdge Edge(int sourceId, int targetId, string label)
    {
        return new RecursiveGraphEdge(sourceId, targetId, 1m, label);
    }
}
