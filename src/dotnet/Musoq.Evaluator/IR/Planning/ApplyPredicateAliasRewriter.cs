using System;
using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Planning;

internal sealed class ApplyPredicateAliasRewriter(
    Func<ColumnRef, ColumnRef?> rewriteColumn) : ExpressionArrayRewriter
{
    public IrExpression Rewrite(IrExpression expression) => Visit(expression);

    protected override IrExpression VisitColumnRef(ColumnRef node) => rewriteColumn(node) ?? node;

    protected override IrExpression VisitScriptParameterRef(ScriptParameterRef node) => node;

    protected override IrExpression VisitScriptVariableRef(ScriptVariableRef node) => node;

    protected override IrExpression VisitLiteral(Literal node) => node;

    protected override IrExpression VisitWildcardLiteral(WildcardLiteral node) => node;

    protected override IrExpression VisitBinaryOp(BinaryOp node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);
        return ReferenceEquals(left, node.Left) && ReferenceEquals(right, node.Right)
            ? node
            : new BinaryOp(node.Kind, left, right, node.ReturnType);
    }

    protected override IrExpression VisitUnaryOp(UnaryOp node)
    {
        var operand = Visit(node.Operand);
        return ReferenceEquals(operand, node.Operand)
            ? node
            : new UnaryOp(node.Kind, operand, node.ReturnType);
    }

    protected override IrExpression VisitMethodCall(MethodCall node)
    {
        var arguments = RewriteExpressions(node.Arguments, out var changed);
        return changed
            ? new MethodCall(node.Method, arguments, node.Alias, node.ReturnType)
            : node;
    }

    protected override IrExpression VisitIsNullCheck(IsNullCheck node)
    {
        var expression = Visit(node.Expression);
        return ReferenceEquals(expression, node.Expression)
            ? node
            : new IsNullCheck(expression, node.IsNegated, node.ReturnType);
    }

    protected override IrExpression VisitInCheck(InCheck node)
    {
        var expression = Visit(node.Expression);
        var values = RewriteExpressions(node.Values, out var valuesChanged);
        return ReferenceEquals(expression, node.Expression) && !valuesChanged
            ? node
            : new InCheck(expression, values, node.ReturnType);
    }

    protected override IrExpression VisitPatternMatch(PatternMatch node)
    {
        var expression = Visit(node.Expression);
        var pattern = Visit(node.Pattern);
        return ReferenceEquals(expression, node.Expression) && ReferenceEquals(pattern, node.Pattern)
            ? node
            : new PatternMatch(expression, pattern, node.Kind, node.ReturnType);
    }

    protected override IrExpression VisitBetween(Between node)
    {
        var expression = Visit(node.Expression);
        var low = Visit(node.Low);
        var high = Visit(node.High);
        return ReferenceEquals(expression, node.Expression) &&
               ReferenceEquals(low, node.Low) &&
               ReferenceEquals(high, node.High)
            ? node
            : new Between(expression, low, high, node.ReturnType);
    }

    protected override IrExpression VisitCaseWhen(CaseWhen node)
    {
        var branches = RewriteBranches(node.Branches, out var branchesChanged);
        var elseExpression = node.ElseExpression == null ? null : Visit(node.ElseExpression);
        return !branchesChanged && ReferenceEquals(elseExpression, node.ElseExpression)
            ? node
            : new CaseWhen(branches, elseExpression, node.ReturnType);
    }

    protected override IrExpression VisitCoalesce(Coalesce node)
    {
        var expressions = RewriteExpressions(node.Expressions, out var changed);
        return changed ? new Coalesce(expressions, node.ReturnType) : node;
    }

    protected override IrExpression VisitAggregateRef(AggregateRef node) => node;

    protected override IrExpression VisitWindowFunctionRef(WindowFunctionRef node) => node;

    protected override IrExpression VisitArrayAccess(ArrayAccess node)
    {
        var array = Visit(node.Array);
        var index = Visit(node.Index);
        return ReferenceEquals(array, node.Array) && ReferenceEquals(index, node.Index)
            ? node
            : new ArrayAccess(array, index, node.ElementType, node.ReturnType);
    }

    protected override IrExpression VisitCteTableRef(CteTableRef node) => node;
}
