using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

public partial class CloneQueryVisitor
{
    public override void Visit(StringNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new StringNode(node.Value, node.Span));
    }

    public override void Visit(DecimalNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new DecimalNode(node.Value, node.Span));
    }

    public override void Visit(IntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new IntegerNode(node.ObjValue, node.Span));
    }

    public override void Visit(HexIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new HexIntegerNode(node.ObjValue, node.Span));
    }

    public override void Visit(BinaryIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new BinaryIntegerNode(node.ObjValue, node.Span));
    }

    public override void Visit(OctalIntegerNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new OctalIntegerNode(node.ObjValue, node.Span));
    }

    public override void Visit(BooleanNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new BooleanNode(node.Value, node.Span));
    }

    public override void Visit(WordNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new WordNode(node.Value, node.Span));
    }

    public override void Visit(NullNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new NullNode(node.Span));
    }

    public override void Visit(ParameterBlockNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var parameters = new ParameterDeclarationNode[node.Parameters.Length];

        for (var i = node.Parameters.Length - 1; i >= 0; --i)
            parameters[i] = (ParameterDeclarationNode)Nodes.Pop();

        Nodes.Push(new ParameterBlockNode(parameters, node.Span));
    }

    public override void Visit(ParameterDeclarationNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var defaultValue = node.HasDefaultValue ? Nodes.Pop() : null;
        Nodes.Push(new ParameterDeclarationNode(node.Name, node.TypeName, node.IsNullable, defaultValue, node.Span));
    }

    public override void Visit(ParameterReferenceNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new ParameterReferenceNode(node.Name, node.ReturnType, node.Span));
    }

    public override void Visit(ContainsNode node)
    {
        var right = Nodes.Pop();
        var left = Nodes.Pop();
        Nodes.Push(new ContainsNode(
            left,
            right as ArgsListNode ?? throw new InvalidOperationException("Contains clone requires an argument list on the right side.")));
    }

    public override void Visit(AccessMethodNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new AccessMethodNode(node.FunctionToken, (ArgsListNode)Nodes.Pop(), null, node.CanSkipInjectSource,
            node.Method, node.Alias, node.Span, node.IsDistinct)
        { HasFilter = node.HasFilter, IsPivotGenerated = node.IsPivotGenerated });
    }

    public override void Visit(AccessRawIdentifierNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new AccessRawIdentifierNode(node.Name, node.ReturnType));
    }

    public override void Visit(IsNullNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new IsNullNode(Nodes.Pop(), node.IsNegated));
    }

    public override void Visit(AccessRefreshAggregationScoreNode node)
    {
    }

    public override void Visit(AccessColumnNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new AccessColumnNode(node.Name, node.Alias, node.ReturnType, node.Span, node.IntendedTypeName));
    }

    public override void Visit(AllColumnsNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        StarReplaceItemNode[]? clonedReplaceItems = null;
        if (node.ReplaceItems is { Length: > 0 })
        {
            clonedReplaceItems = new StarReplaceItemNode[node.ReplaceItems.Length];
            for (var i = node.ReplaceItems.Length - 1; i >= 0; i--)
            {
                var clonedExpr = Nodes.Pop();
                clonedReplaceItems[i] = new StarReplaceItemNode(clonedExpr, node.ReplaceItems[i].ColumnName);
            }
        }

        Nodes.Push(new AllColumnsNode(
            node.Alias,
            node.LikePattern,
            node.IsNotLike,
            node.ExcludeColumns,
            clonedReplaceItems ?? node.ReplaceItems,
            node.RenameItems).WithSpan(node.Span));
    }

    public override void Visit(IdentifierNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new IdentifierNode(node.Name, node.ReturnType, node.Span));
    }

    public override void Visit(AccessObjectArrayNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        if (node.IsColumnAccess)
            Nodes.Push(new AccessObjectArrayNode(node.Token, node.ColumnType, node.TableAlias, node.IntendedTypeName));
        else
            Nodes.Push(new AccessObjectArrayNode(node.Token, node.PropertyInfo));
    }

    public override void Visit(AccessObjectKeyNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var clonedNode = new AccessObjectKeyNode(node.Token, node.PropertyInfo) { DestinationKind = node.DestinationKind };
        Nodes.Push(clonedNode);
    }

    public override void Visit(PropertyValueNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new PropertyValueNode(node.Name, node.PropertyInfo).WithSpan(node.Span));
    }

    public override void Visit(DotNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var exp = Nodes.Pop();
        var root = Nodes.Pop();

        Nodes.Push(new DotNode(root, exp, node.IsTheMostInner, string.Empty, exp.ReturnType, node.IntendedTypeName));
    }

    public override void Visit(AccessCallChainNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        Nodes.Push(new AccessCallChainNode(node.ColumnName, node.ReturnType, node.Props, node.Alias).WithSpan(node.Span));
    }

    public override void Visit(ArgsListNode node)
    {
        ArgumentNullException.ThrowIfNull(node);
        var args = new Node[node.Args.Length];

        for (var i = node.Args.Length - 1; i >= 0; --i)
            args[i] = Nodes.Pop();

        Nodes.Push(new ArgsListNode(args));
    }
}
