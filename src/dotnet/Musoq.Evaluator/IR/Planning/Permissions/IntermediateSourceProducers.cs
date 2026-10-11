using System.Collections.Generic;
using Musoq.Evaluator.IR.Physical;
using Musoq.Evaluator.IR.Physical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal static class IntermediateSourceProducers
{
    public static IReadOnlyDictionary<string, int> Resolve(PhysicalMultiStatementNode statements,
        IEnumerable<string> existingNames)
    {
        var known = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        var producers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var statement in statements.Statements)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectNames(statement, names);
            foreach (var name in names)
            {
                if (known.Add(name))
                    producers.Add(name, producers.Count);
            }
        }
        return producers;
    }

    private static void CollectNames(PhysicalNode node, HashSet<string> names)
    {
        if (node is PhysicalCteRefNode reference)
            names.Add(reference.CteName);
        foreach (var child in node.Children)
            CollectNames(child, names);
    }
}
