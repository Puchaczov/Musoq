using Musoq.Evaluator.Utils;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    public void Visit(CreateTransformationTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = FieldProcessingHelper.CreateFields(node.Fields, Nodes);

        Nodes.Push(new CreateTransformationTableNode(node.Name, node.Keys, fields, node.ForGrouping));
    }

    public void Visit(RenameTableNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new RenameTableNode(node.TableSourceName, node.TableDestinationName));
    }

    public void Visit(TranslatedSetTreeNode node)
    {
    }

    public void Visit(IntoNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new IntoNode(node.Name));
    }

    public void Visit(QueryScope node)
    {
    }

    public void Visit(ShouldBePresentInTheTable node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ShouldBePresentInTheTable(node.Table, node.ExpectedResult, node.Keys));
    }

    public void Visit(TranslatedSetOperatorNode node)
    {
    }

    public void Visit(InternalQueryNode node)
    {
        throw new NotSupportedException();
    }

    public void Visit(RootNode node)
    {
        ValidatePhaseRoot(node);
        var poppedNode = Nodes.Pop();
        RootScript = new RootNode(poppedNode);
    }

    public void Visit(SingleSetNode node)
    {
        var query = (InternalQueryNode)Nodes.Pop();

        var nodes = new Node[]
            { new CreateTransformationTableNode(query.From.Alias, [], query.Select.Fields, false), query };

        Nodes.Push(new MultiStatementNode(nodes, null));
    }

    public void Visit(RefreshNode node)
    {
    }

    public void Visit(UnionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
            orderBy, skip, take)
        {
            KeySpans = node.KeySpans
        });
    }

    public void Visit(UnionAllNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested,
            node.IsTheLastOne, orderBy, skip, take)
        {
            KeySpans = node.KeySpans
        });
    }

    public void Visit(ExceptNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
            orderBy, skip, take)
        {
            KeySpans = node.KeySpans
        });
    }

    public void Visit(IntersectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(
            new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                orderBy, skip, take)
            {
                KeySpans = node.KeySpans
            });
    }

    public void Visit(PutTrueNode node)
    {
        Nodes.Push(new PutTrueNode());
    }

    public void Visit(MultiStatementNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var items = new Node[node.Nodes.Length];

        for (var i = node.Nodes.Length - 1; i >= 0; --i)
            items[i] = Nodes.Pop();

        Nodes.Push(new MultiStatementNode(items, node.ReturnType));
    }

    public void Visit(CteExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var sets = new CteInnerExpressionNode[node.InnerExpression.Length];

        var set = Nodes.Pop();

        for (var i = node.InnerExpression.Length - 1; i >= 0; --i)
            sets[i] = (CteInnerExpressionNode)Nodes.Pop();

        Nodes.Push(new CteExpressionNode(sets, set, node.IsRecursive));
    }

    public void Visit(CteInnerExpressionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new CteInnerExpressionNode(
            Nodes.Pop(),
            node.Name,
            node.Columns,
            node.IsRecursiveDefinition));
    }

    public void SetScope(Scope scope)
    {
        _scopeValue = scope;
    }

}
