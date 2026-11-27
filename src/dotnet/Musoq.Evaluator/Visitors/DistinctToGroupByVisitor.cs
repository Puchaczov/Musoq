using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class DistinctToGroupByVisitor : CloneQueryVisitor
{
    protected override string VisitorName => nameof(DistinctToGroupByVisitor);

    public override void Visit(QueryNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var orderBy = node.OrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var qualify = node.Qualify != null ? Nodes.Pop() as QualifyNode : null;
        var window = node.Window != null ? Nodes.Pop() as WindowNode : null;
        var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;

        var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
        var take = node.Take != null ? Nodes.Pop() as TakeNode : null;

        var select =
            Nodes.Pop() as SelectNode
            ?? throw new InvalidOperationException("Distinct rewrite requires a SELECT node.");
        var where = node.Where != null ? Nodes.Pop() as WhereNode : null;
        var from =
            Nodes.Pop() as FromNode ?? throw new InvalidOperationException("Distinct rewrite requires a FROM node.");

        // Method overloads, including provider aggregates, are resolved during binding.
        // Keep DISTINCT on their result instead of introducing a grouping key containing the call.
        if (
            select is { IsDistinct: true }
            && groupBy == null
            && !ParserNodeChildTraversal.ContainsProjectionCall(select)
            && (
                ParserNodeChildTraversal.Contains<IdentifierNode>(select)
                || ParserNodeChildTraversal.Contains<AccessColumnNode>(select)
                || ParserNodeChildTraversal.Contains<AllColumnsNode>(select)
            )
        )
        {
            groupBy = Array.Exists(select.Fields, static field => field.Expression is AllColumnsNode)
                ? new GroupByNode([], null, isAll: true)
                : new GroupByNode(CreateGroupByFieldsFromSelect(select), null);

            var newSelectFields = new FieldNode[select.Fields.Length];
            for (var i = 0; i < select.Fields.Length; i++)
                newSelectFields[i] = select.Fields[i];

            select = new SelectNode(newSelectFields, true);
        }

        Nodes.Push(
            (
                (QueryNode)new QueryNode(select, from, where, groupBy, orderBy, skip, take, window, qualify, node.Span)
            ).WithFullSpan(node.FullSpan)
        );
    }

    private static FieldNode[] CreateGroupByFieldsFromSelect(SelectNode select)
    {
        var fields = new FieldNode[select.Fields.Length];

        for (var i = 0; i < select.Fields.Length; i++)
        {
            var originalField = select.Fields[i];
            fields[i] = (FieldNode)
                new FieldNode(originalField.Expression, i, string.Empty, originalField.Span).WithFullSpan(
                    originalField.FullSpan
                );
        }

        return fields;
    }
}
