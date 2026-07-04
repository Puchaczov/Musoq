using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Logical;
using Musoq.Evaluator.IR.Logical.Nodes;
using Musoq.Evaluator.IR.Logical.Rewriting;
using Musoq.Evaluator.IR.Optimization;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed partial class LogicalConstantFoldingPass
{
    private static LogicalNode RewriteUnpivot(UnpivotNode node, LogicalConstantExpressionFolder folder)
    {
        var entries = RewriteUnpivotEntries(node.Entries, folder, out var entriesChanged);
        var keepFields = LogicalPlanRewriter.RewriteProjectedFields(node.KeepFields.ToArray(), folder.Visit, out var keepFieldsChanged);

        return !entriesChanged && !keepFieldsChanged
            ? node
            : new UnpivotNode(node.Alias, node.NameColumn, node.ValueColumn, entries, keepFields, node.Source, node.OutputSchema);
    }

    private static UnpivotEntry[] RewriteUnpivotEntries(
        IReadOnlyList<UnpivotEntry> entries,
        LogicalConstantExpressionFolder folder,
        out bool changed)
    {
        var rewritten = new UnpivotEntry[entries.Count];
        changed = false;

        for (var i = 0; i < entries.Count; i++)
        {
            var value = folder.Visit(entries[i].Value);
            rewritten[i] = ReferenceEquals(value, entries[i].Value)
                ? entries[i]
                : entries[i] with { Value = value };
            changed |= !ReferenceEquals(rewritten[i], entries[i]);
        }

        return rewritten;
    }
}

