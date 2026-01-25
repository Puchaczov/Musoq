using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Plugins;

namespace Musoq.Evaluator.Visitors;

public abstract class RewriteFieldWithGroupMethodCallBase<TFieldNode, TInputFieldNode>(TInputFieldNode[] fields)
    : CloneQueryVisitor
    where TFieldNode : FieldNode
    where TInputFieldNode : FieldNode
{
    public TFieldNode Expression { get; protected set; }

    protected abstract string ExtractOriginalExpression(TInputFieldNode node);

    public override void Visit(AccessColumnNode node)
    {
        Nodes.Push(new AccessColumnNode(NamingHelper.ToColumnName(node.Alias, node.Name), string.Empty,
            node.ReturnType, TextSpan.Empty, node.IntendedTypeName));
    }

    public override void Visit(DotNode node)
    {
        if (!(node.Root is DotNode) && node.Root is AccessColumnNode column)
        {
            Nodes.Pop();
            Nodes.Pop();

            var name = $"{NamingHelper.ToColumnName(column.Alias, column.Name)}.{node.Expression.ToString()}";
            Nodes.Push(new AccessColumnNode(name, string.Empty, node.ReturnType, TextSpan.Empty));
            return;
        }

        base.Visit(node);
    }

    public override void Visit(AccessMethodNode node)
    {
        if (node.IsAggregateMethod())
        {
            Nodes.Pop();

            var wordNode = node.Arguments.Args[0] as WordNode;
            var accessGroup = new AccessColumnNode("none", string.Empty, typeof(Group), TextSpan.Empty);
            var args = new List<Node> { accessGroup, wordNode };
            args.AddRange(node.Arguments.Args.Skip(1));
            var extractFromGroup = new AccessMethodNode(
                new FunctionToken(node.Method.Name, TextSpan.Empty),
                new ArgsListNode(args.ToArray()), node.ExtraAggregateArguments, node.CanSkipInjectSource, node.Method,
                node.Alias);
            Nodes.Push(extractFromGroup);
        }
        else if (fields.Select(ExtractOriginalExpression).Contains(node.ToString()))
        {
            Nodes.Push(new AccessColumnNode(node.ToString(), string.Empty, node.ReturnType, TextSpan.Empty));
        }
        else
        {
            base.Visit(node);
        }
    }

    public override void Visit(AccessCallChainNode node)
    {
        Nodes.Push(new AccessColumnNode(node.ToString(), string.Empty, node.ReturnType, TextSpan.Empty));
    }

    public override void Visit(CaseNode node)
    {
        if (fields.Select(f => f.Expression.ToString()).Contains(node.ToString()))
            Nodes.Push(new AccessColumnNode(node.ToString(), string.Empty, node.ReturnType, TextSpan.Empty));
        else
            base.Visit(node);
    }
}
