using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Analysis;
using Musoq.Evaluator.Visitors;
using Musoq.Parser;
using Musoq.Parser.Nodes;
using Musoq.Schema;

namespace Musoq.Evaluator.IR.Expressions;
public sealed partial class ExpressionConverter
{
    private int _windowIndex;
    private readonly Func<WindowFunctionNode, WindowFunctionRef> _windowFunctionConverter;
    private readonly Func<string, string, ColumnStability> _columnStabilityResolver;
    private readonly Func<string, string, EnumTypeDescriptor?> _columnEnumTypeResolver;

    public ExpressionConverter(
        Func<WindowFunctionNode, WindowFunctionRef>? windowFunctionConverter = null,
        Func<string, string, ColumnStability>? columnStabilityResolver = null,
        Func<string, string, EnumTypeDescriptor?>? columnEnumTypeResolver = null)
    {
        _windowFunctionConverter = windowFunctionConverter ?? ConvertWindowFunctionCore;
        _columnStabilityResolver = columnStabilityResolver ?? ((_, _) => ColumnStability.Stable);
        _columnEnumTypeResolver = columnEnumTypeResolver ?? ((_, _) => null);
    }

    private ColumnStability ResolveColumnStability(string alias, string columnName) =>
        _columnStabilityResolver(alias, columnName);

    private EnumTypeDescriptor? ResolveColumnEnumType(string alias, string columnName) =>
        _columnEnumTypeResolver(alias, columnName);

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
            NotNode n => ConvertNot(n),

            AccessMethodNode n => ConvertMethodCall(n),
            DotNode n => ConvertDotAccess(n),
            IsNullNode n => new IsNullCheck(Convert(n.Expression), n.IsNegated, RequireReturnType(n)),
            RowPresenceNode n => ConvertRowPresence(n),
            InNode n => ConvertInNode(n),
            CollectionInNode n => ConvertCollectionInNode(n),
            ContainsNode n => ConvertContainsNode(n),

            LikeNode n => new PatternMatch(Convert(n.Left), Convert(n.Right), PatternKind.Like, RequireReturnType(n)),
            RLikeNode n => new PatternMatch(Convert(n.Left), Convert(n.Right), PatternKind.RLike, RequireReturnType(n)),
            BetweenNode n => ConvertBetween(n),
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

    private IrExpression ConvertDotAccess(DotNode node)
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

            return new ColumnRef(indexerAlias, indexerName, RequireReturnType(node))
            {
                Stability = ResolveColumnStability(indexerAlias, indexerName),
                EnumType = ResolveNativeEnumType(node) ?? ResolveColumnEnumType(indexerAlias, indexerName)
            };
        }

        var (alias, name) = ExtractPath(node);

        if (string.IsNullOrWhiteSpace(alias))
            (alias, name) = SplitLeadingAlias(name, alias);

        return new ColumnRef(alias, name, RequireReturnType(node))
        {
            Stability = ResolveColumnStability(alias, name),
            EnumType = ResolveNativeEnumType(node) ?? ResolveColumnEnumType(alias, name)
        };
    }

    private ArrayAccess BuildIndexedAccess(DotNode node, Type indexableType, Literal indexExpr)
    {
        var (alias, name) = ExtractPath(node);
        if (string.IsNullOrWhiteSpace(alias))
            (alias, name) = SplitLeadingAlias(name, alias);

        var arrayExpr = new ColumnRef(alias, name, indexableType)
        {
            Stability = ResolveColumnStability(alias, name)
        };
        var returnType = RequireReturnType(node);
        return new ArrayAccess(arrayExpr, indexExpr, returnType, returnType);
    }

    private BinaryOp ConvertBinaryOp(BinaryNode node, BinaryOpKind kind)
    {
        var left = Convert(node.Left);
        var right = Convert(node.Right);
        return new(kind, left, right, IrExpressionNullSemantics.NullableBooleanResult(kind, left, right) ?? RequireReturnType(node))
        { UsesSqlNullSemantics = IrExpressionNullSemantics.IsSqlComparison(kind) };
    }

    private IrExpression ConvertNot(NotNode node)
    {
        var operand = Convert(node.Expression);
        if (operand is InCheck inCheck)
            return inCheck with { IsNegated = !inCheck.IsNegated };

        return new UnaryOp(
            UnaryOpKind.Not,
            operand,
            IrExpressionNullSemantics.IsNullableBoolean(operand) ? typeof(bool?) : RequireReturnType(node));
    }

    private Between ConvertBetween(BetweenNode node)
    {
        var expression = Convert(node.Expression);
        var low = Convert(node.Min);
        var high = Convert(node.Max);
        return new(expression, low, high,
            IrExpressionNullSemantics.CanBeNull(expression) || IrExpressionNullSemantics.CanBeNull(low) ||
            IrExpressionNullSemantics.CanBeNull(high) ? typeof(bool?) : RequireReturnType(node));
    }

    private IrExpression ConvertMethodCall(AccessMethodNode node)
    {
        var args = node.Arguments.Args.Select(Convert).ToList();
        if (node.Method == null)
            throw new InvalidOperationException($"AccessMethodNode '{node}' is missing Method; cannot lower to IR.");

        if (EnumIntrinsicMethodFacts.TryGetKind(node.Method, out var intrinsic))
            return ConvertEnumIntrinsic(node, args, intrinsic);

        return new MethodCall(node.Method, args, node.Alias, RequireReturnType(node))
        {
            EnumType = ResolveNativeEnumType(node)
        };
    }

    private InCheck ConvertInNode(InNode node) =>
        new InCheck(Convert(node.Left), ((ArgsListNode)node.Right).Args.Select(Convert).ToList(), RequireReturnType(node));

    private InCheck ConvertContainsNode(ContainsNode node) =>
        new InCheck(Convert(node.Left), node.ToCompareExpression.Args.Select(Convert).ToList(), RequireReturnType(node));

    private CaseWhen ConvertCaseNode(CaseNode node)
    {
        var branches = node.WhenThenPairs
            .Select(pair => new CaseWhenBranch(Convert(pair.When), Convert(pair.Then)))
            .ToArray();

        var elseExpr = node.Else is not NullNode
            ? Convert(node.Else)
            : null;

        return new CaseWhen(branches, elseExpr, IrExpressionNullSemantics.CaseResultType(RequireReturnType(node), branches, elseExpr))
        {
            EnumType = ResolveCommonEnumType(
                branches.Select(static branch => (IrExpression?)branch.Result).Append(elseExpr))
        };
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

    private ArrayAccess ConvertAccessObjectArray(AccessObjectArrayNode node)
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
