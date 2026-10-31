using Musoq.Evaluator.IR.Expressions;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical.Nodes;

namespace Musoq.Evaluator.IR.Logical.Rewriting;

internal static partial class LogicalPlanRewriter
{
    private static LogicalNode RewriteUnpivot(UnpivotNode node, Func<IrExpression, IrExpression> rewriteExpression)
    {
        var entries = RewriteUnpivotEntries(node.Entries, rewriteExpression, out var entriesChanged);
        var keepFields = LogicalPlanRewriter.RewriteProjectedFields(node.KeepFields.ToArray(), rewriteExpression, out var keepFieldsChanged);

        return !entriesChanged && !keepFieldsChanged
            ? node
            : new UnpivotNode(node.Alias, node.NameColumn, node.ValueColumn, entries, keepFields, node.Source, node.OutputSchema);
    }

    private static UnpivotEntry[] RewriteUnpivotEntries(
        IReadOnlyList<UnpivotEntry> entries,
        Func<IrExpression, IrExpression> rewriteExpression,
        out bool changed)
    {
        var rewritten = new UnpivotEntry[entries.Count];
        changed = false;

        for (var i = 0; i < entries.Count; i++)
        {
            var value = rewriteExpression(entries[i].Value);
            rewritten[i] = ReferenceEquals(value, entries[i].Value)
                ? entries[i]
                : entries[i] with { Value = value };
            changed |= !ReferenceEquals(rewritten[i], entries[i]);
        }

        return rewritten;
    }
}

