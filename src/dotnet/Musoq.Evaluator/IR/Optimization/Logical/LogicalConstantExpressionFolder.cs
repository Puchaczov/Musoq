using System.Collections.Generic;
using Musoq.Evaluator.IR.Expressions;
using Musoq.Parser;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.IR.Optimization.Logical;

internal sealed partial class LogicalConstantExpressionFolder(DiagnosticContext? diagnostics) : IrExpressionVisitor<IrExpression>
{
    public int FoldedExpressions { get; private set; }

    protected override IrExpression VisitColumnRef(ColumnRef node) => node;

    protected override IrExpression VisitScriptParameterRef(ScriptParameterRef node) => node;

    protected override IrExpression VisitScriptVariableRef(ScriptVariableRef node) => node;

    protected override IrExpression VisitLiteral(Literal node) => node;

    protected override IrExpression VisitWildcardLiteral(WildcardLiteral node) => node;

    protected override IrExpression VisitBinaryOp(BinaryOp node)
    {
        var left = Visit(node.Left);
        var right = Visit(node.Right);
        var rewritten = ReferenceEquals(left, node.Left) && ReferenceEquals(right, node.Right)
            ? node
            : IrExpressionSourceSpans.CopyFrom(
                new BinaryOp(node.Kind, left, right, node.ReturnType),
                node);

        if (TryFoldBinary(rewritten, out var folded))
        {
            FoldedExpressions++;
            return folded;
        }

        return rewritten;
    }

    protected override IrExpression VisitUnaryOp(UnaryOp node)
    {
        var operand = Visit(node.Operand);
        var rewritten = ReferenceEquals(operand, node.Operand)
            ? node
            : IrExpressionSourceSpans.CopyFrom(
                new UnaryOp(node.Kind, operand, node.ReturnType),
                node);

        if (TryFoldUnary(rewritten, out var folded))
        {
            FoldedExpressions++;
            return folded;
        }

        return rewritten;
    }

    protected override IrExpression VisitMethodCall(MethodCall node)
    {
        var arguments = RewriteExpressions(node.Arguments, out var changed);
        return changed ? node with { Arguments = arguments } : node;
    }

    protected override IrExpression VisitStrictCast(StrictCast node)
    {
        var expression = Visit(node.Expression);
        return ReferenceEquals(expression, node.Expression)
            ? node
            : node with { Expression = expression };
    }

    protected override IrExpression VisitIsNullCheck(IsNullCheck node)
    {
        var expression = Visit(node.Expression);
        if (expression is Literal literal)
        {
            FoldedExpressions++;
            return IrExpressionSourceSpans.CopyFrom(
                new Literal(node.IsNegated ? literal.Value is not null : literal.Value is null, node.ReturnType),
                node);
        }

        return ReferenceEquals(expression, node.Expression)
            ? node
            : node with { Expression = expression };
    }

    protected override IrExpression VisitRowPresence(RowPresence node) => node;

    protected override IrExpression VisitInCheck(InCheck node)
    {
        var expression = Visit(node.Expression);
        var values = RewriteExpressions(node.Values, out var valuesChanged);
        return ReferenceEquals(expression, node.Expression) && !valuesChanged
            ? node
            : node with { Expression = expression, Values = values };
    }

    protected override IrExpression VisitPatternMatch(PatternMatch node)
    {
        var expression = Visit(node.Expression);
        var pattern = Visit(node.Pattern);
        return ReferenceEquals(expression, node.Expression) && ReferenceEquals(pattern, node.Pattern)
            ? node
            : node with { Expression = expression, Pattern = pattern };
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
            : node with { Expression = expression, Low = low, High = high };
    }

    protected override IrExpression VisitCaseWhen(CaseWhen node)
    {
        var branches = new CaseWhenBranch[node.Branches.Length];
        var changed = false;

        for (var index = 0; index < node.Branches.Length; index++)
        {
            var branch = node.Branches[index];
            var condition = Visit(branch.Condition);
            var result = Visit(branch.Result);
            branches[index] = ReferenceEquals(condition, branch.Condition) && ReferenceEquals(result, branch.Result)
                ? branch
                : new CaseWhenBranch(condition, result);
            changed |= !ReferenceEquals(branches[index], branch);
        }

        var elseExpression = node.ElseExpression is null ? null : Visit(node.ElseExpression);
        changed |= node.ElseExpression is not null && !ReferenceEquals(elseExpression, node.ElseExpression);

        return changed ? node with { Branches = branches, ElseExpression = elseExpression } : node;
    }

    protected override IrExpression VisitCoalesce(Coalesce node)
    {
        var expressions = RewriteExpressions(node.Expressions, out var changed);
        var firstLiveExpression = Array.FindIndex(expressions, static expression => expression is not Literal { Value: null });
        if (firstLiveExpression < 0)
        {
            FoldedExpressions++;
            return IrExpressionSourceSpans.CopyFrom(new Literal(null, node.ReturnType), node);
        }

        if (firstLiveExpression > 0)
        {
            expressions = expressions[firstLiveExpression..];
            changed = true;
            FoldedExpressions++;
        }

        if (expressions.Length == 1)
            return IrExpressionSourceSpans.CopyFrom(expressions[0], node);

        return changed ? node with { Expressions = expressions } : node;
    }

    protected override IrExpression VisitAggregateRef(AggregateRef node) => node;

    protected override IrExpression VisitWindowFunctionRef(WindowFunctionRef node) => node;

    protected override IrExpression VisitArrayAccess(ArrayAccess node)
    {
        var array = Visit(node.Array);
        var index = Visit(node.Index);
        return ReferenceEquals(array, node.Array) && ReferenceEquals(index, node.Index)
            ? node
            : node with { Array = array, Index = index };
    }

    protected override IrExpression VisitCteTableRef(CteTableRef node) => node;

    private IrExpression[] RewriteExpressions(IReadOnlyList<IrExpression> expressions, out bool changed)
    {
        var rewritten = new IrExpression[expressions.Count];
        changed = false;

        for (var index = 0; index < expressions.Count; index++)
        {
            rewritten[index] = Visit(expressions[index]);
            changed |= !ReferenceEquals(rewritten[index], expressions[index]);
        }

        return rewritten;
    }

    private void ReportDivisionByZero(IrExpression expression)
    {
        diagnostics?.ReportError(
            DiagnosticCode.MQ3008_DivisionByZero,
            "Division by zero in constant expression.",
            GetSourceSpan(expression));
    }

    private void ReportArithmeticOverflow(IrExpression expression)
    {
        diagnostics?.ReportError(
            DiagnosticCode.MQ3032_ArithmeticOverflow,
            "Arithmetic overflow in constant expression.",
            GetSourceSpan(expression));
    }

    private static bool Succeed(IrExpression expression, out IrExpression folded)
    {
        folded = expression;
        return true;
    }

    private static Literal CreateFoldedLiteral(object? value, IrExpression source)
    {
        return IrExpressionSourceSpans.CopyFrom(new Literal(value, source.ReturnType), source);
    }

    private static TextSpan GetSourceSpan(IrExpression expression)
    {
        return IrExpressionSourceSpans.Get(expression);
    }
}

