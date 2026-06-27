using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class BuildMetadataAndInferTypesVisitor
{
    private readonly Stack<bool> _groupByAllQueryFlags = new();

    internal void MarkCurrentQueryGroupByAll()
    {
        if (_groupByAllQueryFlags.Count == 0)
            return;

        _groupByAllQueryFlags.Pop();
        _groupByAllQueryFlags.Push(true);
    }

    private void GroupByAllQueryBegins()
    {
        _groupByAllQueryFlags.Push(false);
    }

    private void GroupByAllQueryEnds()
    {
        if (_groupByAllQueryFlags.Count > 0)
            _groupByAllQueryFlags.Pop();
    }

    private static GroupByNode? ExpandGroupByAllIfNeeded(SelectNode select, GroupByNode? groupBy)
    {
        return groupBy?.IsAll == true ? ExpandGroupByAll(select, groupBy) : groupBy;
    }

    private static GroupByNode ExpandGroupByAll(SelectNode select, GroupByNode groupBy)
    {
        var fields = new List<FieldNode>();
        var seenExpressions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var field in select.Fields)
        {
            var expression = field.Expression;

            if (BuildMetadataAndInferTypesVisitorUtilities.ContainsAggregateFunction(expression))
                continue;

            if (!seenExpressions.Add(expression.ToString()))
                continue;

            fields.Add(new FieldNode(expression, fields.Count, string.Empty));
        }

        if (fields.Count == 0)
            fields.Add(new FieldNode(new IntegerNode("1", "s"), 0, string.Empty));

        return new GroupByNode(fields.ToArray(), groupBy.Having, false, groupBy.Span);
    }

}
