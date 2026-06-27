using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Musoq.Evaluator.Helpers;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Parser.Tokens;
using Musoq.Plugins.Attributes;

namespace Musoq.Evaluator.Visitors;

public abstract class RewriteFieldWithGroupMethodCallBase<TFieldNode, TInputFieldNode>(TInputFieldNode[] fields)
    : CloneQueryVisitor
    where TFieldNode : FieldNode
    where TInputFieldNode : FieldNode
{
    private TFieldNode? _expression;

    public TFieldNode Expression
    {
        get => _expression ?? throw new InvalidOperationException("The rewritten field expression is available only after visiting a compatible field node.");
        protected set => _expression = value ?? throw new ArgumentNullException(nameof(value));
    }

    protected abstract string ExtractOriginalExpression(TInputFieldNode node);

    public override void Visit(AccessColumnNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new AccessColumnNode(NamingHelper.ToColumnName(node.Alias, node.Name), string.Empty,
            node.ReturnType, TextSpan.Empty, node.IntendedTypeName));
    }

    public override void Visit(DotNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (!(node.Root is DotNode) && node.Root is AccessColumnNode column)
        {
            Nodes.Pop();
            Nodes.Pop();

            var name = $"{NamingHelper.ToColumnName(column.Alias, column.Name)}.{node.Expression.ToString()}";
            Nodes.Push(new AccessColumnNode(name, string.Empty, node.ReturnType ?? typeof(object), TextSpan.Empty));
            return;
        }

        base.Visit(node);
    }

    public override void Visit(AccessMethodNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.IsAggregateMethod())
        {
            Nodes.Pop();

            var wordNode = node.Arguments.Args.Length > 0
                ? node.Arguments.Args[0] as WordNode
                : null;
            if (IsAggregateDeclarationMethod(node))
            {
                Nodes.Push(new AccessColumnNode(
                    wordNode?.Value ?? node.ToString(),
                    string.Empty,
                    node.ReturnType,
                    TextSpan.Empty));
                return;
            }

            var accessGroup = new AccessColumnNode("none", string.Empty, typeof(object), TextSpan.Empty);
            var args = new List<Node> { accessGroup, wordNode ?? new WordNode(node.ToString()) };
            args.AddRange(node.Arguments.Args.Skip(1));
            var extractFromGroup = new AccessMethodNode(
                new FunctionToken(node.Method?.Name ?? node.Name, TextSpan.Empty),
                new ArgsListNode(args.ToArray()), node.ExtraAggregateArguments, node.CanSkipInjectSource, node.Method,
                node.Alias, default, node.IsDistinct);
            Nodes.Push(extractFromGroup);
        }
        else if (fields.Select(ExtractOriginalExpression).Contains(node.ToString()))
        {
            Nodes.Pop();
            Nodes.Push(new AccessColumnNode(node.ToString(), string.Empty, node.ReturnType, TextSpan.Empty));
        }
        else
        {
            base.Visit(node);
        }
    }

    private static bool IsAggregateDeclarationMethod(AccessMethodNode node)
    {
        return node.Method?.GetCustomAttribute<AggregateFunctionAttribute>() is not null;
    }

    public override void Visit(AccessCallChainNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new AccessColumnNode(node.ToString(), string.Empty, node.ReturnType, TextSpan.Empty));
    }

    public override void Visit(WindowFunctionNode node)
    {
        Nodes.Push(node);
    }

    public override void Visit(CaseNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (fields.Select(f => f.Expression.ToString()).Contains(node.ToString()))
        {
            for (var i = 0; i < node.WhenThenPairs.Length; i++)
            {
                Nodes.Pop();
                Nodes.Pop();
            }

            Nodes.Pop();
            Nodes.Push(new AccessColumnNode(node.ToString(), string.Empty, node.ReturnType, TextSpan.Empty));
        }
        else
        {
            base.Visit(node);
        }
    }

    public override void Visit(StarNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (TryReplaceWithGroupColumn(node))
            return;

        base.Visit(node);
    }

    public override void Visit(FSlashNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (TryReplaceWithGroupColumn(node))
            return;

        base.Visit(node);
    }

    public override void Visit(ModuloNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (TryReplaceWithGroupColumn(node))
            return;

        base.Visit(node);
    }

    public override void Visit(AddNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (TryReplaceWithGroupColumn(node))
            return;

        base.Visit(node);
    }

    public override void Visit(HyphenNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (TryReplaceWithGroupColumn(node))
            return;

        base.Visit(node);
    }

    private bool TryReplaceWithGroupColumn(BinaryNode node)
    {
        var nodeString = node.ToString();
        if (!fields.Select(ExtractOriginalExpression).Contains(nodeString))
            return false;

        Nodes.Pop();
        Nodes.Pop();
        Nodes.Push(new AccessColumnNode(nodeString, string.Empty, node.ReturnType, TextSpan.Empty));
        return true;
    }
}
