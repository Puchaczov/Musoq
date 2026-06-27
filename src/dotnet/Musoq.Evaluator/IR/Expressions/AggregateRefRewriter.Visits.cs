namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class AggregateRefRewriter
{
    protected override IrExpression VisitColumnRef(ColumnRef node)
    {
        if (TryResolveColumnAggregate(node, out var binding))
            return new AggregateRef(binding.Identifier, binding.ReturnType);

        return node;
    }

    protected override IrExpression VisitScriptParameterRef(ScriptParameterRef node) => node;

    protected override IrExpression VisitScriptVariableRef(ScriptVariableRef node) => node;

    protected override IrExpression VisitLiteral(Literal node) => node;

    protected override IrExpression VisitWildcardLiteral(WildcardLiteral node) => node;

    protected override IrExpression VisitBinaryOp(BinaryOp node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);
        var returnType = GetBinaryReturnType(node, left, right);

        if (ReferenceEquals(left, node.Left) &&
            ReferenceEquals(right, node.Right) &&
            returnType == node.ReturnType)
            return node;

        return new BinaryOp(node.Kind, left, right, returnType);
    }

    private static Type GetBinaryReturnType(BinaryOp node, IrExpression left, IrExpression right)
    {
        if (!IsNullableAggregateArithmetic(node, left, right))
            return node.ReturnType;

        return typeof(Nullable<>).MakeGenericType(node.ReturnType);
    }

    private static bool IsNullableAggregateArithmetic(BinaryOp node, IrExpression left, IrExpression right)
    {
        if (node.ReturnType == null ||
            !node.ReturnType.IsValueType ||
            Nullable.GetUnderlyingType(node.ReturnType) != null ||
            !IsArithmetic(node.Kind))
            return false;

        return IsNullableAggregateExpression(left) || IsNullableAggregateExpression(right);
    }

    private static bool IsArithmetic(BinaryOpKind kind)
    {
        return kind is BinaryOpKind.Add or BinaryOpKind.Subtract or BinaryOpKind.Multiply or BinaryOpKind.Divide or BinaryOpKind.Modulo;
    }

    private static bool IsNullableAggregateExpression(IrExpression expression)
    {
        return expression is AggregateRef &&
               expression.ReturnType.IsValueType &&
               Nullable.GetUnderlyingType(expression.ReturnType) != null;
    }

    protected override IrExpression VisitUnaryOp(UnaryOp node)
    {
        var operand = Visit(node.Operand);

        if (ReferenceEquals(operand, node.Operand))
            return node;

        return new UnaryOp(node.Kind, operand, node.ReturnType);
    }

    protected override IrExpression VisitMethodCall(MethodCall node)
    {
        var identifier = ExtractBoundIdentifier(node);

        if (identifier is not null &&
            _bindingsByIdentifier.TryGetValue(identifier, out var binding) &&
            ShouldRewriteToAggregateRef(node.Method, binding))
            return new AggregateRef(identifier, binding.ReturnType);

        if (identifier is null &&
            IsAggregateMethod(node.Method) &&
            TryResolveFallbackBinding(node.Method, out var fallbackBinding))
            return new AggregateRef(fallbackBinding.Identifier, fallbackBinding.ReturnType);

        var arguments = RewriteExpressions(node.Arguments, out var changed);

        if (!changed)
            return node;

        return new MethodCall(node.Method, arguments, node.Alias, node.ReturnType);
    }

    protected override IrExpression VisitIsNullCheck(IsNullCheck node)
    {
        var expression = Visit(node.Expression);

        if (ReferenceEquals(expression, node.Expression))
            return node;

        return new IsNullCheck(expression, node.IsNegated, node.ReturnType);
    }

    protected override IrExpression VisitInCheck(InCheck node)
    {
        var expression = Visit(node.Expression);
        var values = RewriteExpressions(node.Values, out var valuesChanged);

        if (ReferenceEquals(expression, node.Expression) && !valuesChanged)
            return node;

        return new InCheck(expression, values, node.ReturnType);
    }

    protected override IrExpression VisitPatternMatch(PatternMatch node)
    {
        var expression = Visit(node.Expression);
        var pattern = Visit(node.Pattern);

        if (ReferenceEquals(expression, node.Expression) && ReferenceEquals(pattern, node.Pattern))
            return node;

        return new PatternMatch(expression, pattern, node.Kind, node.ReturnType);
    }

    protected override IrExpression VisitBetween(Between node)
    {
        var expression = Visit(node.Expression);
        var low = Visit(node.Low);
        var high = Visit(node.High);

        if (ReferenceEquals(expression, node.Expression) &&
            ReferenceEquals(low, node.Low) &&
            ReferenceEquals(high, node.High))
            return node;

        return new Between(expression, low, high, node.ReturnType);
    }

    protected override IrExpression VisitCaseWhen(CaseWhen node)
    {
        var branches = RewriteBranches(node.Branches, out var branchesChanged);
        var elseExpression = node.ElseExpression is null ? null : Visit(node.ElseExpression);

        if (!branchesChanged && ReferenceEquals(elseExpression, node.ElseExpression))
            return node;

        return new CaseWhen(branches, elseExpression, node.ReturnType);
    }

    protected override IrExpression VisitCoalesce(Coalesce node)
    {
        var expressions = RewriteExpressions(node.Expressions, out var changed);

        if (!changed)
            return node;

        return new Coalesce(expressions, node.ReturnType);
    }

    protected override IrExpression VisitAggregateRef(AggregateRef node) => node;

    protected override IrExpression VisitWindowFunctionRef(WindowFunctionRef node) => node;

    protected override IrExpression VisitCteTableRef(CteTableRef node) => node;
}
