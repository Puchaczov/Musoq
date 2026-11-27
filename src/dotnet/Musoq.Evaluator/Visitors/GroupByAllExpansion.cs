using System.Collections.Generic;
using System.Linq;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static class GroupByAllExpansion
{
    internal static GroupByNode? ExpandIfNeeded(SelectNode select, GroupByNode? groupBy)
    {
        return groupBy?.IsAll == true ? ExpandGroupByAll(select, groupBy) : groupBy;
    }

    private static GroupByNode ExpandGroupByAll(SelectNode select, GroupByNode groupBy)
    {
        var fields = new List<FieldNode>();
        var seenExpressions = new Dictionary<string, List<Node>>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in select.Fields)
        {
            var expression = field.Expression;

            if (BuildMetadataAndInferTypesVisitorUtilities.ContainsAggregateFunction(expression))
                continue;

            var text = expression.ToString();
            if (!seenExpressions.TryGetValue(text, out var variants))
            {
                variants = [];
                seenExpressions.Add(text, variants);
            }
            else if (
                variants.Any(previous =>
                    GetStringLiterals(previous).SequenceEqual(GetStringLiterals(expression), StringComparer.Ordinal)
                )
            )
            {
                continue;
            }

            variants.Add(expression);
            fields.Add(new FieldNode(expression, fields.Count, string.Empty));
        }

        if (fields.Count == 0)
            fields.Add(new FieldNode(new IntegerNode("1", "s"), 0, string.Empty));

        return new GroupByNode(fields.ToArray(), groupBy.Having, false, groupBy.Span);
    }

    private static IEnumerable<string> GetStringLiterals(Node expression) =>
        expression is ConstantValueNode { ObjValue: string value }
            ? [value]
            : ParserNodeChildTraversal.EnumerateChildren(expression).SelectMany(GetStringLiterals);
}
