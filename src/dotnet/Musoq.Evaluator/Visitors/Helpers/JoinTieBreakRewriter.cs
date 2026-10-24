using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors.Helpers;

internal static class JoinTieBreakRewriter
{
    internal static FieldOrderedNode? Rewrite(
        FieldOrderedNode? tieBreak,
        IReadOnlyDictionary<string, string> usedTables)
    {
        if (tieBreak == null)
            return null;

        var expressionUpdater = new RewriteToUpdatedColumnAccess(usedTables);
        var expressionUpdaterTraverser = new RewriteToUpdatedColumnAccessTraverser(expressionUpdater);
        new WhereNode(tieBreak.Expression).Accept(expressionUpdaterTraverser);

        return new FieldOrderedNode(
            expressionUpdater.Where.Expression,
            tieBreak.FieldOrder,
            tieBreak.FieldName,
            tieBreak.HasExplicitFieldName,
            tieBreak.Order,
            tieBreak.NullOrdering);
    }
}
