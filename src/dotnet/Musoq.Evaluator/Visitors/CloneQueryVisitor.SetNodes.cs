using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(UnionNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        Nodes.Push(((UnionNode)new UnionNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
            orderBy, skip, take)
            {
                KeySpans = node.KeySpans
            })
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan));
    }

    public override void Visit(UnionAllNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();

        Nodes.Push(((UnionAllNode)new UnionAllNode(node.ResultTableName, node.Keys, left, right, node.IsNested,
            node.IsTheLastOne, orderBy, skip, take)
            {
                KeySpans = node.KeySpans
            })
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan));
    }

    public override void Visit(ExceptNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(((ExceptNode)new ExceptNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
            orderBy, skip, take)
            {
                KeySpans = node.KeySpans
            })
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan));
    }

    public override void Visit(IntersectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var take = node.ResultTake != null ? Nodes.Pop() as TakeNode : null;
        var skip = node.ResultSkip != null ? Nodes.Pop() as SkipNode : null;
        var orderBy = node.ResultOrderBy != null ? Nodes.Pop() as OrderByNode : null;
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(
            ((IntersectNode)new IntersectNode(node.ResultTableName, node.Keys, left, right, node.IsNested, node.IsTheLastOne,
                orderBy, skip, take)
            {
                KeySpans = node.KeySpans
            })
            .WithSpan(node.Span)
            .WithFullSpan(node.FullSpan));
    }
}
