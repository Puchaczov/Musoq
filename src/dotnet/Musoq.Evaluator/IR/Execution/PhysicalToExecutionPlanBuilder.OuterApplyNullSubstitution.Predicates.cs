using Musoq.Evaluator.IR.Expressions;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyBinary(
        ExecutionBinary binary,
        string rightAlias)
    {
        var left = SubstituteOuterApplyRightAlias(binary.Left, rightAlias);
        if (!left.Supported)
            return left;

        var right = SubstituteOuterApplyRightAlias(binary.Right, rightAlias);
        if (!right.Supported)
            return right;

        return binary.Kind switch
        {
            BinaryOpKind.And => SubstituteOuterApplyAnd(left, right),
            BinaryOpKind.Or => SubstituteOuterApplyOr(left, right),
            _ when left.IsUnknown || right.IsUnknown => OuterApplyNullSubstitutionResult.Unknown(),
            _ => OuterApplyNullSubstitutionResult.Known(binary with
            {
                Left = left.Expression,
                Right = right.Expression
            })
        };
    }

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyAnd(
        OuterApplyNullSubstitutionResult left,
        OuterApplyNullSubstitutionResult right)
    {
        if (left.IsUnknown || right.IsUnknown)
            return OuterApplyNullSubstitutionResult.Unknown();

        var leftExpression = NormalizeOuterApplyBooleanOperand(left.Expression);
        if (!leftExpression.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(leftExpression.UnsupportedReason);

        var rightExpression = NormalizeOuterApplyBooleanOperand(right.Expression);
        if (!rightExpression.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(rightExpression.UnsupportedReason);

        return OuterApplyNullSubstitutionResult.Known(new ExecutionBinary(
            BinaryOpKind.And,
            leftExpression.Value,
            rightExpression.Value,
            typeof(bool)));
    }

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyOr(
        OuterApplyNullSubstitutionResult left,
        OuterApplyNullSubstitutionResult right)
    {
        if (left.IsUnknown && right.IsUnknown)
            return OuterApplyNullSubstitutionResult.Unknown();

        if (left.IsUnknown)
            return right;

        if (right.IsUnknown)
            return left;

        var leftExpression = NormalizeOuterApplyBooleanOperand(left.Expression);
        if (!leftExpression.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(leftExpression.UnsupportedReason);

        var rightExpression = NormalizeOuterApplyBooleanOperand(right.Expression);
        if (!rightExpression.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(rightExpression.UnsupportedReason);

        return OuterApplyNullSubstitutionResult.Known(new ExecutionBinary(
            BinaryOpKind.Or,
            leftExpression.Value,
            rightExpression.Value,
            typeof(bool)));
    }

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyUnary(
        ExecutionUnary unary,
        string rightAlias)
    {
        var operand = SubstituteOuterApplyRightAlias(unary.Operand, rightAlias);
        if (!operand.Supported)
            return operand;

        return operand.IsUnknown
            ? OuterApplyNullSubstitutionResult.Unknown()
            : OuterApplyNullSubstitutionResult.Known(unary with { Operand = operand.Expression });
    }

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyMethodCall(
        ExecutionMethodCall method,
        string rightAlias)
    {
        var arguments = SubstituteOuterApplyArguments(method.Arguments, rightAlias);
        if (!arguments.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(arguments.UnsupportedReason);

        var injectedSource = method.InjectedSource;
        var injectedSourceIsUnknown = false;
        if (method.InjectedSource != null)
        {
            var substitutedSource = SubstituteOuterApplyRightAlias(method.InjectedSource, rightAlias);
            if (!substitutedSource.Supported)
                return OuterApplyNullSubstitutionResult.Unsupported(substitutedSource.UnsupportedReason);

            injectedSourceIsUnknown = substitutedSource.IsUnknown;
            injectedSource = substitutedSource.Expression;
        }

        return arguments.HasUnknown || injectedSourceIsUnknown
            ? OuterApplyNullSubstitutionResult.Unknown()
            : OuterApplyNullSubstitutionResult.Known(method with
            {
                Arguments = arguments.Expressions,
                InjectedSource = injectedSource
            });
    }

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyIsNullCheck(
        ExecutionIsNullCheck isNull,
        string rightAlias)
    {
        var expression = SubstituteOuterApplyRightAlias(isNull.Expression, rightAlias);
        if (!expression.Supported)
            return expression;

        return expression.IsUnknown
            ? OuterApplyNullSubstitutionResult.Known(new ExecutionLiteral(!isNull.IsNegated, typeof(bool)))
            : OuterApplyNullSubstitutionResult.Known(isNull with { Expression = expression.Expression });
    }

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyInCheck(
        ExecutionInCheck inCheck,
        string rightAlias)
    {
        var expression = SubstituteOuterApplyRightAlias(inCheck.Expression, rightAlias);
        if (!expression.Supported)
            return expression;

        if (expression.IsUnknown)
            return OuterApplyNullSubstitutionResult.Unknown();

        var values = SubstituteOuterApplyArguments(inCheck.Values, rightAlias);
        if (!values.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(values.UnsupportedReason);

        if (values.Expressions.Count == 0 && values.HasUnknown)
            return OuterApplyNullSubstitutionResult.Unknown();

        return OuterApplyNullSubstitutionResult.Known(inCheck with
        {
            Expression = expression.Expression,
            Values = values.Expressions
        });
    }

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyPatternMatch(
        ExecutionPatternMatch patternMatch,
        string rightAlias)
    {
        var expression = SubstituteOuterApplyRightAlias(patternMatch.Expression, rightAlias);
        if (!expression.Supported)
            return expression;

        var pattern = SubstituteOuterApplyRightAlias(patternMatch.Pattern, rightAlias);
        if (!pattern.Supported)
            return pattern;

        return expression.IsUnknown || pattern.IsUnknown
            ? OuterApplyNullSubstitutionResult.Unknown()
            : OuterApplyNullSubstitutionResult.Known(patternMatch with
            {
                Expression = expression.Expression,
                Pattern = pattern.Expression
            });
    }

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyBetween(
        ExecutionBetween between,
        string rightAlias)
    {
        var expression = SubstituteOuterApplyRightAlias(between.Expression, rightAlias);
        if (!expression.Supported)
            return expression;

        var low = SubstituteOuterApplyRightAlias(between.Low, rightAlias);
        if (!low.Supported)
            return low;

        var high = SubstituteOuterApplyRightAlias(between.High, rightAlias);
        if (!high.Supported)
            return high;

        return expression.IsUnknown || low.IsUnknown || high.IsUnknown
            ? OuterApplyNullSubstitutionResult.Unknown()
            : OuterApplyNullSubstitutionResult.Known(between with
            {
                Expression = expression.Expression,
                Low = low.Expression,
                High = high.Expression
            });
    }
}
