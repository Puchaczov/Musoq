using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class DistinctToGroupByVisitor : CloneQueryVisitor
{
    protected override string VisitorName => nameof(DistinctToGroupByVisitor);

    public override void Visit(QueryNode node)
    {
        var orderBy = node.OrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var groupBy = node.GroupBy != null ? Nodes.Pop() as GroupByNode : null;

        var skip = node.Skip != null ? Nodes.Pop() as SkipNode : null;
        var take = node.Take != null ? Nodes.Pop() as TakeNode : null;

        var select = Nodes.Pop() as SelectNode;
        var where = node.Where != null ? Nodes.Pop() as WhereNode : null;
        var from = Nodes.Pop() as FromNode;

        if (select is { IsDistinct: true } && groupBy == null)
        {
            var groupByFields = CreateGroupByFieldsFromSelect(select);
            groupBy = new GroupByNode(groupByFields, null);

            var newSelectFields = new FieldNode[select.Fields.Length];
            for (var i = 0; i < select.Fields.Length; i++) newSelectFields[i] = select.Fields[i];

            select = new SelectNode(newSelectFields, true);
        }

        Nodes.Push(new QueryNode(select, from, where, groupBy, orderBy, skip, take));
    }

    private static FieldNode[] CreateGroupByFieldsFromSelect(SelectNode select)
    {
        var fields = new FieldNode[select.Fields.Length];

        for (var i = 0; i < select.Fields.Length; i++)
        {
            var originalField = select.Fields[i];
            fields[i] = new FieldNode(originalField.Expression, i, string.Empty);
        }

        return fields;
    }
}
