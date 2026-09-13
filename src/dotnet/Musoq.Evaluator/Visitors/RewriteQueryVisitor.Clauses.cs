using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Visitors.Helpers;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public sealed partial class RewriteQueryVisitor
{
    public void Visit(FieldNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new FieldNode(Nodes.Pop(), node.FieldOrder,
            QueryRewriteUtilities.RewriteFieldNameWithoutStringPrefixAndSuffix(node.FieldName),
            node.HasExplicitFieldName).CopySpansFrom(node));
    }

    public void Visit(FieldOrderedNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new FieldOrderedNode(Nodes.Pop(), node.FieldOrder,
            QueryRewriteUtilities.RewriteFieldNameWithoutStringPrefixAndSuffix(node.FieldName),
            node.HasExplicitFieldName, node.Order, node.NullOrdering).CopySpansFrom(node));
    }

    public void Visit(SelectNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = FieldProcessingHelper.CreateFields(node.Fields, Nodes);

        Nodes.Push(new SelectNode(fields.ToArray(), node.IsDistinct).CopySpansFrom(node));
    }

    public void Visit(GroupSelectNode node)
    {
    }

    public void Visit(ArgsListNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var args = new Node[node.Args.Length];

        for (var i = node.Args.Length - 1; i >= 0; --i)
            args[i] = Nodes.Pop();

        Nodes.Push(new ArgsListNode(args, node.ArgumentNames, default).CopySpansFrom(node));
    }

    public void Visit(WhereNode node)
    {
        var rewrittenNode = QueryRewriteUtilities.RewriteNullableBoolExpressions(Nodes.Pop());

        Nodes.Push(new WhereNode(rewrittenNode).CopySpansFrom(node));
    }

    public void Visit(GroupByNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var having = Nodes.Peek() as HavingNode;

        if (having != null)
            Nodes.Pop();

        var fields = new FieldNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldNode)Nodes.Pop();

        Nodes.Push(new GroupByNode(fields, having, node.IsAll, node.Span));
    }

    public void Visit(HavingNode node)
    {
        Nodes.Push(new HavingNode(Nodes.Pop()).CopySpansFrom(node));
    }

    public void Visit(SkipNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new SkipNode((IntegerNode)node.Expression).CopySpansFrom(node));
    }

    public void Visit(TakeNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new TakeNode((IntegerNode)node.Expression).CopySpansFrom(node));
    }

    public void Visit(OrderByNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var fields = new FieldOrderedNode[node.Fields.Length];

        for (var i = node.Fields.Length - 1; i >= 0; --i)
            fields[i] = (FieldOrderedNode)Nodes.Pop();

        Nodes.Push(new OrderByNode(fields).CopySpansFrom(node));
    }

    public void Visit(CaseNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var whenThenPairs = new List<(Node When, Node Then)>();

        for (var i = 0; i < node.WhenThenPairs.Length; ++i)
        {
            var then = Nodes.Pop();
            var when = Nodes.Pop();
            whenThenPairs.Add((when, then));
        }

        var elseNode = Nodes.Pop();

        Nodes.Push(new CaseNode(whenThenPairs.ToArray(), elseNode, node.ReturnType).CopySpansFrom(node));
    }

    public void Visit(WhenNode node)
    {
        var expression = Nodes.Pop();
        var rewrittenExpression = QueryRewriteUtilities.RewriteNullableBoolExpressions(expression);
        Nodes.Push(new WhenNode(rewrittenExpression).CopySpansFrom(node));
    }

    public void Visit(ThenNode node)
    {
        var expression = Nodes.Pop();
        Nodes.Push(new ThenNode(expression).CopySpansFrom(node));
    }

    public void Visit(ElseNode node)
    {
        var expression = Nodes.Pop();
        Nodes.Push(new ElseNode(expression).CopySpansFrom(node));
    }

    public void Visit(QualifyNode node)
    {
        Nodes.Push(new QualifyNode(Nodes.Pop()).CopySpansFrom(node));
    }
}
