using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record CteReferenceIndex(
    IReadOnlyDictionary<string, CteReference[]> ByAlias)
{
    public static CteReferenceIndex Create(LogicalNode node)
    {
        return Create(node, CancellationToken.None);
    }

    public static CteReferenceIndex Create(
        LogicalNode node,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var references = new List<CteReference>();
        AddCteReferences(node, references, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var byAlias = references
            .GroupBy(static reference => reference.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        cancellationToken.ThrowIfCancellationRequested();
        return new CteReferenceIndex(byAlias);
    }

    public CteReference[] Find(string alias)
    {
        return ByAlias.TryGetValue(alias, out var references)
            ? references
            : [];
    }

    private static void AddCteReferences(
        LogicalNode node,
        List<CteReference> references,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (node is CteRefNode cteRef)
            references.Add(new CteReference(
                cteRef.CteName,
                cteRef.Alias,
                CreateOutputColumnSet(cteRef, cancellationToken)));

        foreach (var child in node.Children)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddCteReferences(child, references, cancellationToken);
        }
    }

    private static HashSet<string> CreateOutputColumnSet(
        CteRefNode cteRef,
        CancellationToken cancellationToken)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in cteRef.OutputSchema.Columns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            columns.Add(column.Name);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return columns;
    }
}
