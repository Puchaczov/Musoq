using System.Collections.Generic;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.IR.Expressions;
public sealed partial class ExpressionConverter
{
    private int _windowIndex;
    private readonly Func<WindowFunctionNode, WindowFunctionRef> _windowFunctionConverter;

    public ExpressionConverter(Func<WindowFunctionNode, WindowFunctionRef>? windowFunctionConverter = null)
    {
        _windowFunctionConverter = windowFunctionConverter ?? ConvertWindowFunctionCore;
    }

    public IrExpression Convert(Node node)
    {
        var expression = node switch
        {
            AccessColumnNode col => ConvertColumnAccess(col),
            ParameterReferenceNode parameter => new ScriptParameterRef(parameter.Name, RequireReturnType(parameter)),
            ScriptVariableReferenceNode variable => new ScriptVariableRef(variable.Name, RequireReturnType(variable)),
            AggregateIdentifierNode n => new Literal(n.ObjValue, RequireReturnType(n), n.DisplayName),
            ConstantValueNode n => new Literal(n.ObjValue, RequireReturnType(n)),
            NullNode n => new Literal(null, RequireReturnType(n)),
            AddNode n => ConvertBinaryOp(n, n.ReturnType == typeof(string) ? BinaryOpKind.StringConcatenate : BinaryOpKind.Add),
            HyphenNode n => ConvertBinaryOp(n, BinaryOpKind.Subtract),
            StarNode n => ConvertBinaryOp(n, BinaryOpKind.Multiply),
            FSlashNode n => ConvertBinaryOp(n, BinaryOpKind.Divide),
            ModuloNode n => ConvertBinaryOp(n, BinaryOpKind.Modulo),
            AndNode n => ConvertBinaryOp(n, BinaryOpKind.And),
            OrNode n => ConvertBinaryOp(n, BinaryOpKind.Or),
            EqualityNode n => ConvertBinaryOp(n, BinaryOpKind.Equal),
            DiffNode n => ConvertBinaryOp(n, BinaryOpKind.NotEqual),
            IsDistinctFromNode n => ConvertBinaryOp(n, n.IsNegated ? BinaryOpKind.IsNotDistinctFrom : BinaryOpKind.IsDistinctFrom),
            GreaterNode n => ConvertBinaryOp(n, BinaryOpKind.GreaterThan),
            LessNode n => ConvertBinaryOp(n, BinaryOpKind.LessThan),
            GreaterOrEqualNode n => ConvertBinaryOp(n, BinaryOpKind.GreaterOrEqual),
            LessOrEqualNode n => ConvertBinaryOp(n, BinaryOpKind.LessOrEqual),

            BitwiseAndNode n => ConvertBinaryOp(n, BinaryOpKind.BitwiseAnd),
            BitwiseOrNode n => ConvertBinaryOp(n, BinaryOpKind.BitwiseOr),
            BitwiseXorNode n => ConvertBinaryOp(n, BinaryOpKind.BitwiseXor),
            LeftShiftNode n => ConvertBinaryOp(n, BinaryOpKind.LeftShift),
            RightShiftNode n => ConvertBinaryOp(n, BinaryOpKind.RightShift),
            CoalesceNode n => ConvertCoalesce(n),
            CastNode n => ConvertCast(n),
            NotNode n => new UnaryOp(UnaryOpKind.Not, Convert(n.Expression), RequireReturnType(n)),

            AccessMethodNode n => ConvertMethodCall(n),
            DotNode n => ConvertDotAccess(n),
            IsNullNode n => new IsNullCheck(Convert(n.Expression), n.IsNegated, RequireReturnType(n)),
            RowPresenceNode n => ConvertRowPresence(n),
            InNode n => ConvertInNode(n),
            CollectionInNode n => ConvertCollectionInNode(n),
            ContainsNode n => ConvertContainsNode(n),

            LikeNode n => new PatternMatch(Convert(n.Left), Convert(n.Right), PatternKind.Like, RequireReturnType(n)),
            RLikeNode n => new PatternMatch(Convert(n.Left), Convert(n.Right), PatternKind.RLike, RequireReturnType(n)),
            BetweenNode n => new Between(Convert(n.Expression), Convert(n.Min), Convert(n.Max), RequireReturnType(n)),
            CaseNode n => ConvertCaseNode(n),
            WindowFunctionNode n => ConvertWindowFunction(n),
            AllColumnsNode => new WildcardLiteral(typeof(void)),
            FieldNode n => Convert(n.Expression),

            ShortCircuitingNodeLeft n => Convert(n.Expression),
            ShortCircuitingNodeRight n => Convert(n.Expression),

            WhenNode n => Convert(n.Expression),
            ThenNode n => Convert(n.Expression),
            ElseNode n => Convert(n.Expression),

            ArrayIndexNode n => ConvertArrayAccess(n),
            AccessObjectArrayNode n => ConvertAccessObjectArray(n),
            AccessObjectKeyNode n => ConvertAccessObjectKey(n),
            IdentifierNode n => new CteTableRef(n.Name),

            _ => throw new UnsupportedIrShapeException($"Cannot convert AST node of type '{node.GetType().Name}' to IR expression.")
        };

        return WithSourceSpan(expression, node);
    }

    private static IrExpression ConvertDotAccess(DotNode node)
    {
        if (node.Expression is AccessObjectKeyNode keyAccess && keyAccess.PropertyInfo != null)
            return BuildIndexedAccess(node, keyAccess.PropertyInfo.PropertyType, new Literal(keyAccess.Token.Key, typeof(string)));

        if (node.Expression is AccessObjectArrayNode arrayAccess && arrayAccess.PropertyInfo != null)
            return BuildIndexedAccess(node, arrayAccess.PropertyInfo.PropertyType, new Literal(arrayAccess.Token.Index, typeof(int)));

        // When an indexer appears in the middle of a dot-chain (for example Complex.Array[0].Id), encode
        // the indexer into the path string so EvaluationHelper can apply it during runtime resolution.
        if (ContainsIndexerNode(node))
        {
            var (indexerAlias, indexerName) = ExtractPathWithIndexers(node);
            if (string.IsNullOrWhiteSpace(indexerAlias))
                (indexerAlias, indexerName) = SplitLeadingAlias(indexerName, indexerAlias);

            return new ColumnRef(indexerAlias, indexerName, RequireReturnType(node));
        }

        var (alias, name) = ExtractPath(node);

        if (string.IsNullOrWhiteSpace(alias))
            (alias, name) = SplitLeadingAlias(name, alias);

        return new ColumnRef(alias, name, RequireReturnType(node));
    }

    private static ArrayAccess BuildIndexedAccess(DotNode node, Type indexableType, Literal indexExpr)
    {
        var (alias, name) = ExtractPath(node);
        if (string.IsNullOrWhiteSpace(alias))
            (alias, name) = SplitLeadingAlias(name, alias);

        var arrayExpr = new ColumnRef(alias, name, indexableType);
        var returnType = RequireReturnType(node);
        return new ArrayAccess(arrayExpr, indexExpr, returnType, returnType);
    }

    private static (string Alias, string Name) ExtractPath(Node node)
    {
        return node switch
        {
            AccessColumnNode column => NormalizeAccessColumn(column),
            PropertyValueNode property => (string.Empty, property.Name),
            IdentifierNode identifier => (string.Empty, identifier.Name),
            WordNode word => (string.Empty, word.Value),
            DotNode dot => MergePath(ExtractPath(dot.Root), ComposePathSegment(ExtractPath(dot.Expression))),
            _ => throw new UnsupportedIrShapeException($"Cannot extract dotted path from AST node of type '{node.GetType().Name}'.")
        };
    }

    private static (string Alias, string Name) ExtractPathWithIndexers(Node node)
    {
        return node switch
        {
            AccessColumnNode column => NormalizeAccessColumn(column),
            PropertyValueNode property => (string.Empty, property.Name),
            AccessObjectArrayNode arrayAccess => (string.Empty, $"{arrayAccess.Name}[{arrayAccess.Token.Index}]"),
            AccessObjectKeyNode keyAccess => (string.Empty, $"{keyAccess.Name}['{keyAccess.Token.Key}']"),
            IdentifierNode identifier => (string.Empty, identifier.Name),
            WordNode word => (string.Empty, word.Value),
            DotNode dot => MergePath(ExtractPathWithIndexers(dot.Root), ComposePathSegment(ExtractPathWithIndexers(dot.Expression))),
            _ => throw new UnsupportedIrShapeException($"Cannot extract dotted path from AST node of type '{node.GetType().Name}'.")
        };
    }

    private static bool ContainsIndexerNode(Node node)
    {
        return node switch
        {
            AccessObjectArrayNode => true,
            AccessObjectKeyNode => true,
            DotNode dot => ContainsIndexerNode(dot.Root) || ContainsIndexerNode(dot.Expression),
            _ => false
        };
    }

    private static string ComposePathSegment((string Alias, string Name) path)
    {
        if (string.IsNullOrWhiteSpace(path.Alias))
            return path.Name;

        if (string.IsNullOrWhiteSpace(path.Name))
            return path.Alias;

        return $"{path.Alias}.{path.Name}";
    }

    private static (string Alias, string Name) NormalizeAccessColumn(AccessColumnNode column)
    {
        if (!string.IsNullOrWhiteSpace(column.Alias))
            return (column.Alias, column.Name);

        return SplitLeadingAlias(column.Name, column.Alias);
    }

    private static (string Alias, string Name) SplitLeadingAlias(string name, string alias)
    {
        var dotIndex = name.IndexOf('.', StringComparison.Ordinal);
        if (dotIndex <= 0 || dotIndex >= name.Length - 1)
            return (alias, name);

        return (name[..dotIndex], name[(dotIndex + 1)..]);
    }

    private static (string Alias, string Name) MergePath((string Alias, string Name) left, string rightSegment)
    {
        if (string.IsNullOrWhiteSpace(rightSegment))
            return left;

        if (string.IsNullOrWhiteSpace(left.Name))
            return (left.Alias, rightSegment);

        return (left.Alias, $"{left.Name}.{rightSegment}");
    }

    private BinaryOp ConvertBinaryOp(BinaryNode node, BinaryOpKind kind) =>
        new(kind, Convert(node.Left), Convert(node.Right), RequireReturnType(node));

    private MethodCall ConvertMethodCall(AccessMethodNode node)
    {
        var args = new List<IrExpression>();
        foreach (var arg in node.Arguments.Args)
            args.Add(Convert(arg));

        if (node.Method == null)
            throw new InvalidOperationException($"AccessMethodNode '{node}' is missing Method; cannot lower to IR.");

        return new MethodCall(node.Method, args, node.Alias, RequireReturnType(node));
    }

    private InCheck ConvertInNode(InNode node)
    {
        var expression = Convert(node.Left);
        var argsListNode = (ArgsListNode)node.Right;
        var values = new List<IrExpression>();
        foreach (var arg in argsListNode.Args)
            values.Add(Convert(arg));

        return new InCheck(expression, values, RequireReturnType(node));
    }

    private InCheck ConvertContainsNode(ContainsNode node)
    {
        var expression = Convert(node.Left);
        var values = new List<IrExpression>();
        foreach (var arg in node.ToCompareExpression.Args)
            values.Add(Convert(arg));

        return new InCheck(expression, values, RequireReturnType(node));
    }

    private CaseWhen ConvertCaseNode(CaseNode node)
    {
        var branches = new CaseWhenBranch[node.WhenThenPairs.Length];
        for (var i = 0; i < node.WhenThenPairs.Length; i++)
        {
            var (whenNode, thenNode) = node.WhenThenPairs[i];
            branches[i] = new CaseWhenBranch(Convert(whenNode), Convert(thenNode));
        }

        var elseExpr = node.Else is not NullNode
            ? Convert(node.Else)
            : null;

        return new CaseWhen(branches, elseExpr, RequireReturnType(node));
    }

    private WindowFunctionRef ConvertWindowFunction(WindowFunctionNode node) => _windowFunctionConverter(node);

    private WindowFunctionRef ConvertWindowFunctionCore(WindowFunctionNode node) =>
        new(_windowIndex++, RequireReturnType(node));

    private ArrayAccess ConvertArrayAccess(ArrayIndexNode node)
    {
        var arrayExpr = Convert(node.Array);
        var indexExpr = Convert(node.Index);

        var returnType = RequireReturnType(node);
        return new ArrayAccess(arrayExpr, indexExpr, returnType, returnType);
    }

    private static ArrayAccess ConvertAccessObjectArray(AccessObjectArrayNode node)
    {
        // AccessObjectArrayNode represents indexed access like Name[0] or str[1]
        var columnAccessNode = new AccessColumnNode(node.ObjectName, node.TableAlias ?? string.Empty, RequireReturnType(node, node.ColumnType), TextSpan.Empty, node.IntendedTypeName);
        var arrayExpr = ConvertColumnAccess(columnAccessNode);
        var indexExpr = new Literal(node.Token.Index, typeof(int));

        var returnType = RequireReturnType(node);
        return new ArrayAccess(arrayExpr, indexExpr, returnType, returnType);
    }

    private static ArrayAccess ConvertAccessObjectKey(AccessObjectKeyNode node)
    {
        if (node.PropertyInfo == null)
            throw new InvalidOperationException($"AccessObjectKeyNode '{node}' is missing PropertyInfo; cannot lower to IR.");

        var indexableType = node.PropertyInfo.PropertyType;
        var arrayExpr = new ColumnRef(string.Empty, node.ObjectName, indexableType);
        var indexExpr = new Literal(node.Token.Key, typeof(string));
        var returnType = RequireReturnType(node);
        return new ArrayAccess(arrayExpr, indexExpr, returnType, returnType);
    }
}
