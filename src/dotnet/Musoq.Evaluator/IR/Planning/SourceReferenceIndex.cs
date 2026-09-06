using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Planning;

internal sealed record SourceReferenceIndex(
    IReadOnlyList<SourceReference> All,
    IReadOnlyDictionary<string, SourceReference[]> ByAlias)
{
    public static SourceReferenceIndex Create(LogicalNode node)
    {
        return Create(node, CancellationToken.None);
    }

    public static SourceReferenceIndex Create(
        LogicalNode node,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var references = new List<SourceReference>();
        AddSourceReferences(node, references, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();

        var byAlias = references
            .GroupBy(static source => source.Alias, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                static group => group.Key,
                static group => group.ToArray(),
                StringComparer.OrdinalIgnoreCase);

        cancellationToken.ThrowIfCancellationRequested();
        return new SourceReferenceIndex(references, byAlias);
    }

    public SourceReference[] Find(string alias)
    {
        return ByAlias.TryGetValue(alias, out var sourceReferences)
            ? sourceReferences
            : [];
    }

    private static void AddSourceReferences(
        LogicalNode node,
        List<SourceReference> references,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (node is SchemaScanNode scan)
            references.Add(new SourceReference(
                scan.SourceContextId,
                scan.Alias,
                CreateOutputColumnSet(scan, cancellationToken)));

        foreach (var child in node.Children)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddSourceReferences(child, references, cancellationToken);
        }
    }

    private static HashSet<string> CreateOutputColumnSet(
        SchemaScanNode scan,
        CancellationToken cancellationToken)
    {
        var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var column in scan.OutputSchema.Columns)
        {
            cancellationToken.ThrowIfCancellationRequested();
            columns.Add(column.Name);
        }

        cancellationToken.ThrowIfCancellationRequested();
        return columns;
    }
}
