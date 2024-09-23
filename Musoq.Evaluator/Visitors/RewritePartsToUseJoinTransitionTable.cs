using Musoq.Evaluator.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public class RewritePartsToUseJoinTransitionTable : CloneQueryVisitor
{
    private readonly string _alias;

    public RewritePartsToUseJoinTransitionTable(string alias = "")
    {
        _alias = alias;
    }

    public SelectNode ChangedSelect { get; private set; }
        
    public GroupByNode ChangedGroupBy { get; private set; }

    public WhereNode ChangedWhere { get; private set; }
        
    public OrderByNode ChangedOrderBy { get; private set; }
        
    public Node RewrittenNode => Nodes.Pop();

    public override void Visit(AccessColumnNode node)
    {
        base.Visit(new AccessColumnNode(NamingHelper.ToColumnName(node.Alias, node.Name), _alias, node.ReturnType,
            node.Span));
    }

    public override void Visit(SelectNode node)
    {
        var fields = new FieldNode[node.Fields.Length];

        for (int i = 0, j = fields.Length - 1; i < fields.Length; i++, j--) fields[j] = (FieldNode) Nodes.Pop();

        ChangedSelect = new SelectNode(fields);
    }

    public override void Visit(GroupByNode node)
    {
        var fields = new FieldNode[node.Fields.Length];

        HavingNode having = null;
        if (node.Having != null)
            having = (HavingNode) Nodes.Pop();

        for (int i = 0, j = fields.Length - 1; i < fields.Length; i++, j--) fields[j] = (FieldNode) Nodes.Pop();
            
        ChangedGroupBy = new GroupByNode(fields, having);
    }

    public override void Visit(WhereNode node)
    {
        ChangedWhere = new WhereNode(Nodes.Pop());
    }
        
    public override void Visit(OrderByNode node)
    {
        var fields = new FieldOrderedNode[node.Fields.Length];

        for (int i = 0, j = fields.Length - 1; i < fields.Length; i++, j--) fields[j] = (FieldOrderedNode) Nodes.Pop();

        ChangedOrderBy = new OrderByNode(fields);
    }
}