using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record CteReferenceIndex(
    IReadOnlyDictionary<string, CteReference[]> ByAlias)
{
    public static CteReferenceIndex Create(LogicalNode node)
    {
        var references = new List<CteReference>();
        AddCteReferences(node, references);

        var byAlias = references
            .GroupBy(static reference => reference.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return new CteReferenceIndex(byAlias);
    }

    public CteReference[] Find(string alias)
    {
        return ByAlias.TryGetValue(alias, out var references)
            ? references
            : [];
    }

    private static void AddCteReferences(LogicalNode node, List<CteReference> references)
    {
        if (node is CteRefNode cteRef)
            references.Add(new CteReference(cteRef.CteName, cteRef.Alias, CreateOutputColumnSet(cteRef)));

        foreach (var child in node.Children)
            AddCteReferences(child, references);
    }

    private static HashSet<string> CreateOutputColumnSet(CteRefNode cteRef)
    {
        return cteRef.OutputSchema.Columns
            .Select(static column => column.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
