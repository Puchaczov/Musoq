using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static class IntermediateSourceProducers
{
    public static IReadOnlyDictionary<string, int> Resolve(PhysicalMultiStatementNode statements)
    {
        var referenced = new HashSet<IntermediateProducerId>();
        CollectReferences(statements, referenced);
        var producers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var producer in statements.IntermediateProducers.OrderBy(static producer => producer.StatementIndex))
        {
            if (!referenced.Contains(producer.Source.ProducerId))
                continue;
            if ((uint)producer.StatementIndex >= (uint)statements.Statements.Length)
                throw new InvalidOperationException("An intermediate producer refers to an unavailable statement.");
            producers.Add(producer.Source.StorageName, producer.StatementIndex);
        }
        return producers;
    }

    internal static ImmutableArray<IntermediateSourceProducer> Remap(
        PhysicalMultiStatementNode original, IReadOnlyList<int> retainedIndexes)
    {
        var positions = new int[original.Statements.Length];
        Array.Fill(positions, -1);
        for (var index = 0; index < retainedIndexes.Count; index++)
            positions[retainedIndexes[index]] = index;
        return original.IntermediateProducers.Where(producer => positions[producer.StatementIndex] >= 0)
            .Select(producer => producer with { StatementIndex = positions[producer.StatementIndex] }).ToImmutableArray();
    }

    private static void CollectReferences(PhysicalNode node, HashSet<IntermediateProducerId> referenced)
    {
        if (node is PhysicalCteRefNode { IntermediateProducerId: { } identity })
            referenced.Add(identity);
        foreach (var child in node.Children)
            CollectReferences(child, referenced);
    }
}
