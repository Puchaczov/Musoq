using Musoq.Parser.Nodes;
using Musoq.Evaluator.Visitors.Helpers.Subqueries;

namespace Musoq.Evaluator.Visitors;

internal static class ColumnAccessNodeCopy
{
    internal static AccessColumnNode Create(AccessColumnNode node) =>
        node is SubqueryParameterNode ? node : new(node.Name, node.Alias, node.ReturnType, node.Span, node.IntendedTypeName)
        {
            SourceColumnIndex = node.SourceColumnIndex
        };
}
