using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Expressions;

public sealed partial class WindowFunctionRefRewriter(IReadOnlyDictionary<int, int> windowIndexMap) : ExpressionArrayRewriter
{
    private readonly IReadOnlyDictionary<int, int> _windowIndexMap = windowIndexMap ?? throw new ArgumentNullException(nameof(windowIndexMap));

    public static IrExpression Rewrite(IrExpression expression, IReadOnlyDictionary<int, int> windowIndexMap)
    {
        ArgumentNullException.ThrowIfNull(expression);
        var rewriter = new WindowFunctionRefRewriter(windowIndexMap);
        return rewriter.Visit(expression);
    }

    protected override IrExpression VisitColumnRef(ColumnRef node)
    {
        return node;
    }

    protected override IrExpression VisitScriptParameterRef(ScriptParameterRef node)
    {
        return node;
    }

    protected override IrExpression VisitScriptVariableRef(ScriptVariableRef node)
    {
        return node;
    }

    protected override IrExpression VisitLiteral(Literal node)
    {
        return node;
    }

    protected override IrExpression VisitWildcardLiteral(WildcardLiteral node)
    {
        return node;
    }

    protected override IrExpression VisitBinaryOp(BinaryOp node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);

        if (ReferenceEquals(left, node.Left) && ReferenceEquals(right, node.Right))
            return node;

        return new BinaryOp(node.Kind, left, right, node.ReturnType) { UsesSqlNullSemantics = node.UsesSqlNullSemantics };
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
        var arguments = RewriteExpressions(node.Arguments, out var changed);

        if (!changed)
            return node;

        return node with { Arguments = arguments };
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

        return new InCheck(expression, values, node.ReturnType, node.IsNegated);
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

    protected override IrExpression VisitAggregateRef(AggregateRef node)
    {
        return node;
    }

    protected override IrExpression VisitWindowFunctionRef(WindowFunctionRef node)
    {
        if (!_windowIndexMap.TryGetValue(node.WindowIndex, out var windowIndex))
            return node;

        return windowIndex == node.WindowIndex
            ? node
            : new WindowFunctionRef(windowIndex, node.ReturnType);
    }

    protected override IrExpression VisitCteTableRef(CteTableRef node)
    {
        return node;
    }

}
