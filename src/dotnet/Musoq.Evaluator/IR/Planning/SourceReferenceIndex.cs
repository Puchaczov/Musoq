using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourceReferenceIndex(
    IReadOnlyList<SourceReference> All,
    IReadOnlyDictionary<string, SourceReference[]> ByAlias)
{
    public static SourceReferenceIndex Create(LogicalNode node)
    {
        var references = new List<SourceReference>();
        AddSourceReferences(node, references);

        var byAlias = references
            .GroupBy(static source => source.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        return new SourceReferenceIndex(references, byAlias);
    }

    public SourceReference[] Find(string alias)
    {
        return ByAlias.TryGetValue(alias, out var sourceReferences)
            ? sourceReferences
            : [];
    }

    private static void AddSourceReferences(LogicalNode node, List<SourceReference> references)
    {
        if (node is SchemaScanNode scan)
            references.Add(new SourceReference(scan.SourceContextId, scan.Alias, CreateOutputColumnSet(scan)));

        foreach (var child in node.Children)
            AddSourceReferences(child, references);
    }

    private static HashSet<string> CreateOutputColumnSet(SchemaScanNode scan)
    {
        return scan.OutputSchema.Columns
            .Select(static column => column.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
