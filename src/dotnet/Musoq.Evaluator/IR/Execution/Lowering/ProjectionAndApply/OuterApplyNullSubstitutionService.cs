using System;
using System.Collections.Generic;
using System.Linq;
using Musoq.Evaluator.IR.Expressions;
using IrExpressionPrinter = Musoq.Evaluator.IR.Expressions.IrExpressionPrinter;

namespace Musoq.Evaluator.IR.Execution;

internal static class OuterApplyNullSubstitutionService
{
    public static OuterApplyNullSubstitutionResult SubstituteRightAlias(
        ExecutionExpression expression,
        string rightAlias)
    {
        return expression switch
        {
            ExecutionFieldRead fieldRead when string.Equals(fieldRead.Alias, rightAlias, StringComparison.OrdinalIgnoreCase) =>
                OuterApplyNullSubstitutionResult.Unknown(),
            ExecutionFieldRead => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionLiteral => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionBinary binary => SubstituteBinary(binary, rightAlias),
            ExecutionUnary unary => SubstituteUnary(unary, rightAlias),
            ExecutionStrictCast strictCast => SubstituteStrictCast(strictCast, rightAlias),
            ExecutionMethodCall method => SubstituteMethodCall(method, rightAlias),
            ExecutionIsNullCheck isNull => SubstituteIsNullCheck(isNull, rightAlias),
            ExecutionRowPresence rowPresence when string.Equals(rowPresence.Alias, rightAlias, StringComparison.OrdinalIgnoreCase) =>
                OuterApplyNullSubstitutionResult.Known(new ExecutionLiteral(!rowPresence.IsPresent, typeof(bool))),
            ExecutionRowPresence => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionInCheck inCheck => SubstituteInCheck(inCheck, rightAlias),
            ExecutionPatternMatch patternMatch => SubstitutePatternMatch(patternMatch, rightAlias),
            ExecutionBetween between => SubstituteBetween(between, rightAlias),
            ExecutionCaseWhen caseWhen => SubstituteCaseWhen(caseWhen, rightAlias),
            ExecutionCoalesce coalesce => SubstituteCoalesce(coalesce, rightAlias),
            ExecutionRowStream => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionScalarRowStream => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionStoredTableRows => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionVariableRead => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionCompositeKey compositeKey => SubstituteCompositeKey(compositeKey, rightAlias),
            ExecutionValueTupleKey valueTupleKey => SubstituteValueTupleKey(valueTupleKey, rightAlias),
            ExecutionWindowValueRead => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionAggregateCall aggregateCall => SubstituteAggregateCall(aggregateCall, rightAlias),
            ExecutionGroupKeyRead => OuterApplyNullSubstitutionResult.Known(expression),
            ExecutionRawExpression raw when ReferencesAlias(raw.Expression, rightAlias) =>
                OuterApplyNullSubstitutionResult.Unsupported(
                    $"Execution IR outer apply lowering cannot null-substitute right-side filter expression {IrExpressionPrinter.Print(raw.Expression)}."),
            ExecutionRawExpression => OuterApplyNullSubstitutionResult.Known(expression),
            _ => OuterApplyNullSubstitutionResult.Unsupported(
                $"Execution IR outer apply lowering cannot null-substitute expression {expression.GetType().Name}.")
        };
    }

    public static BuildResult<ExecutionExpression> NormalizeBooleanOperand(ExecutionExpression expression)
    {
        if (expression.ReturnType == typeof(bool))
            return BuildResult<ExecutionExpression>.Success(expression);

        if (Nullable.GetUnderlyingType(expression.ReturnType) == typeof(bool))
        {
            return BuildResult<ExecutionExpression>.Success(new ExecutionBinary(
                BinaryOpKind.Equal,
                expression,
                new ExecutionLiteral(true, typeof(bool)),
                typeof(bool)));
        }

        return BuildResult<ExecutionExpression>.Unsupported(
            $"Execution IR outer apply lowering expected a boolean expression but found {FormatTypeName(expression.ReturnType)}.");
    }

    private static OuterApplyNullSubstitutionResult SubstituteStrictCast(
        ExecutionStrictCast strictCast,
        string rightAlias)
    {
        var expression = SubstituteRightAlias(strictCast.Expression, rightAlias);
        return expression.Supported
            ? OuterApplyNullSubstitutionResult.Known(strictCast with { Expression = expression.Expression })
            : expression;
    }

    private static OuterApplyNullSubstitutionResult SubstituteBinary(
        ExecutionBinary binary,
        string rightAlias)
    {
        var left = SubstituteRightAlias(binary.Left, rightAlias);
        if (!left.Supported)
            return left;

        var right = SubstituteRightAlias(binary.Right, rightAlias);
        if (!right.Supported)
            return right;

        return binary.Kind switch
        {
            BinaryOpKind.And => SubstituteAnd(left, right),
            BinaryOpKind.Or => SubstituteOr(left, right),
            _ when left.IsUnknown || right.IsUnknown => OuterApplyNullSubstitutionResult.Unknown(),
            _ => OuterApplyNullSubstitutionResult.Known(binary with
            {
                Left = left.Expression,
                Right = right.Expression
            })
        };
    }

    private static OuterApplyNullSubstitutionResult SubstituteAnd(
        OuterApplyNullSubstitutionResult left,
        OuterApplyNullSubstitutionResult right)
    {
        if (left.IsUnknown || right.IsUnknown)
            return OuterApplyNullSubstitutionResult.Unknown();

        var leftExpression = NormalizeBooleanOperand(left.Expression);
        if (!leftExpression.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(leftExpression.UnsupportedReason);

        var rightExpression = NormalizeBooleanOperand(right.Expression);
        if (!rightExpression.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(rightExpression.UnsupportedReason);

        return OuterApplyNullSubstitutionResult.Known(new ExecutionBinary(
            BinaryOpKind.And,
            leftExpression.Value,
            rightExpression.Value,
            typeof(bool)));
    }

    private static OuterApplyNullSubstitutionResult SubstituteOr(
        OuterApplyNullSubstitutionResult left,
        OuterApplyNullSubstitutionResult right)
    {
        if (left.IsUnknown && right.IsUnknown)
            return OuterApplyNullSubstitutionResult.Unknown();

        if (left.IsUnknown)
            return right;

        if (right.IsUnknown)
            return left;

        var leftExpression = NormalizeBooleanOperand(left.Expression);
        if (!leftExpression.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(leftExpression.UnsupportedReason);

        var rightExpression = NormalizeBooleanOperand(right.Expression);
        if (!rightExpression.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(rightExpression.UnsupportedReason);

        return OuterApplyNullSubstitutionResult.Known(new ExecutionBinary(
            BinaryOpKind.Or,
            leftExpression.Value,
            rightExpression.Value,
            typeof(bool)));
    }

    private static OuterApplyNullSubstitutionResult SubstituteUnary(
        ExecutionUnary unary,
        string rightAlias)
    {
        var operand = SubstituteRightAlias(unary.Operand, rightAlias);
        if (!operand.Supported)
            return operand;

        return operand.IsUnknown
            ? OuterApplyNullSubstitutionResult.Unknown()
            : OuterApplyNullSubstitutionResult.Known(unary with { Operand = operand.Expression });
    }

    private static OuterApplyNullSubstitutionResult SubstituteMethodCall(
        ExecutionMethodCall method,
        string rightAlias)
    {
        var arguments = SubstituteArguments(method.Arguments, rightAlias);
        if (!arguments.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(arguments.UnsupportedReason);

        var injectedSource = method.InjectedSource;
        var injectedSourceIsUnknown = false;
        if (method.InjectedSource != null)
        {
            var substitutedSource = SubstituteRightAlias(method.InjectedSource, rightAlias);
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

    private static OuterApplyNullSubstitutionResult SubstituteIsNullCheck(
        ExecutionIsNullCheck isNull,
        string rightAlias)
    {
        var expression = SubstituteRightAlias(isNull.Expression, rightAlias);
        if (!expression.Supported)
            return expression;

        return expression.IsUnknown
            ? OuterApplyNullSubstitutionResult.Known(new ExecutionLiteral(!isNull.IsNegated, typeof(bool)))
            : OuterApplyNullSubstitutionResult.Known(isNull with { Expression = expression.Expression });
    }

    private static OuterApplyNullSubstitutionResult SubstituteInCheck(
        ExecutionInCheck inCheck,
        string rightAlias)
    {
        var expression = SubstituteRightAlias(inCheck.Expression, rightAlias);
        if (!expression.Supported)
            return expression;

        if (expression.IsUnknown)
            return OuterApplyNullSubstitutionResult.Unknown();

        var values = SubstituteArguments(inCheck.Values, rightAlias);
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

    private static OuterApplyNullSubstitutionResult SubstitutePatternMatch(
        ExecutionPatternMatch patternMatch,
        string rightAlias)
    {
        var expression = SubstituteRightAlias(patternMatch.Expression, rightAlias);
        if (!expression.Supported)
            return expression;

        var pattern = SubstituteRightAlias(patternMatch.Pattern, rightAlias);
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

    private static OuterApplyNullSubstitutionResult SubstituteBetween(
        ExecutionBetween between,
        string rightAlias)
    {
        var expression = SubstituteRightAlias(between.Expression, rightAlias);
        if (!expression.Supported)
            return expression;

        var low = SubstituteRightAlias(between.Low, rightAlias);
        if (!low.Supported)
            return low;

        var high = SubstituteRightAlias(between.High, rightAlias);
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

    private static OuterApplyNullSubstitutionResult SubstituteCaseWhen(
        ExecutionCaseWhen caseWhen,
        string rightAlias)
    {
        var branches = new List<ExecutionCaseWhenBranch>(caseWhen.Branches.Count);
        var hasNullResult = false;
        foreach (var branch in caseWhen.Branches)
        {
            var condition = SubstituteRightAlias(branch.Condition, rightAlias);
            if (!condition.Supported)
                return condition;

            if (condition.IsUnknown)
                continue;

            var conditionExpression = NormalizeBooleanOperand(condition.Expression);
            if (!conditionExpression.Supported)
                return OuterApplyNullSubstitutionResult.Unsupported(conditionExpression.UnsupportedReason);

            var result = SubstituteRightAlias(branch.Result, rightAlias);
            if (!result.Supported)
                return result;

            var resultExpression = result.IsUnknown
                ? CreateNullLiteral(caseWhen.ReturnType)
                : result.Expression;
            hasNullResult |= result.IsUnknown;

            branches.Add(new ExecutionCaseWhenBranch(conditionExpression.Value, resultExpression));
        }

        var elseExpression = SubstituteCaseElse(caseWhen.ElseExpression, rightAlias);
        if (!elseExpression.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(elseExpression.UnsupportedReason);

        var resolvedElseExpression = elseExpression.IsUnknown
            ? CreateNullLiteral(caseWhen.ReturnType)
            : elseExpression.Expression;
        hasNullResult |= elseExpression.IsUnknown;

        if (branches.Count == 0)
            return resolvedElseExpression == null
                ? OuterApplyNullSubstitutionResult.Unknown()
                : OuterApplyNullSubstitutionResult.Known(resolvedElseExpression);

        return OuterApplyNullSubstitutionResult.Known(caseWhen with
        {
            Branches = branches,
            ElseExpression = resolvedElseExpression,
            ReturnType = hasNullResult
                ? LiftNullSubstitutionType(caseWhen.ReturnType)
                : caseWhen.ReturnType
        });
    }

    private static OuterApplyCaseElseSubstitutionResult SubstituteCaseElse(
        ExecutionExpression? elseExpression,
        string rightAlias)
    {
        if (elseExpression == null)
            return OuterApplyCaseElseSubstitutionResult.Known(null);

        var substituted = SubstituteRightAlias(elseExpression, rightAlias);
        if (!substituted.Supported)
            return OuterApplyCaseElseSubstitutionResult.Unsupported(substituted.UnsupportedReason);

        return substituted.IsUnknown
            ? OuterApplyCaseElseSubstitutionResult.Unknown()
            : OuterApplyCaseElseSubstitutionResult.Known(substituted.Expression);
    }

    private static OuterApplyNullSubstitutionResult SubstituteCoalesce(
        ExecutionCoalesce coalesce,
        string rightAlias)
    {
        var expressions = new List<ExecutionExpression>(coalesce.Expressions.Count);

        foreach (var expression in coalesce.Expressions)
        {
            var substituted = SubstituteRightAlias(expression, rightAlias);
            if (!substituted.Supported)
                return OuterApplyNullSubstitutionResult.Unsupported(substituted.UnsupportedReason);

            if (!substituted.IsUnknown)
                expressions.Add(substituted.Expression);
        }

        return expressions.Count switch
        {
            0 => OuterApplyNullSubstitutionResult.Unknown(),
            1 => OuterApplyNullSubstitutionResult.Known(expressions[0]),
            _ => OuterApplyNullSubstitutionResult.Known(coalesce with { Expressions = expressions })
        };
    }

    private static OuterApplyNullSubstitutionResult SubstituteCompositeKey(
        ExecutionCompositeKey compositeKey,
        string rightAlias)
    {
        var parts = SubstituteArguments(compositeKey.Parts, rightAlias);
        if (!parts.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(parts.UnsupportedReason);

        return parts.HasUnknown
            ? OuterApplyNullSubstitutionResult.Unknown()
            : OuterApplyNullSubstitutionResult.Known(compositeKey with { Parts = parts.Expressions });
    }

    private static OuterApplyNullSubstitutionResult SubstituteValueTupleKey(
        ExecutionValueTupleKey valueTupleKey,
        string rightAlias)
    {
        var parts = SubstituteArguments(valueTupleKey.Parts, rightAlias);
        if (!parts.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(parts.UnsupportedReason);

        return parts.HasUnknown
            ? OuterApplyNullSubstitutionResult.Unknown()
            : OuterApplyNullSubstitutionResult.Known(valueTupleKey with { Parts = parts.Expressions });
    }

    private static OuterApplyNullSubstitutionResult SubstituteAggregateCall(
        ExecutionAggregateCall aggregateCall,
        string rightAlias)
    {
        var arguments = SubstituteArguments(aggregateCall.Arguments, rightAlias);
        if (!arguments.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(arguments.UnsupportedReason);

        return arguments.HasUnknown
            ? OuterApplyNullSubstitutionResult.Unknown()
            : OuterApplyNullSubstitutionResult.Known(aggregateCall with { Arguments = arguments.Expressions });
    }

    private static OuterApplyArgumentSubstitutionResult SubstituteArguments(
        IReadOnlyList<ExecutionExpression> arguments,
        string rightAlias)
    {
        var expressions = new List<ExecutionExpression>(arguments.Count);
        var hasUnknown = false;

        foreach (var argument in arguments)
        {
            var substituted = SubstituteRightAlias(argument, rightAlias);
            if (!substituted.Supported)
                return OuterApplyArgumentSubstitutionResult.Unsupported(substituted.UnsupportedReason);

            if (substituted.IsUnknown)
            {
                hasUnknown = true;
                continue;
            }

            expressions.Add(substituted.Expression);
        }

        return OuterApplyArgumentSubstitutionResult.Success(expressions, hasUnknown);
    }

    private static ExecutionLiteral CreateNullLiteral(Type returnType)
    {
        return new ExecutionLiteral(null, LiftNullSubstitutionType(returnType));
    }

    private static Type LiftNullSubstitutionType(Type type)
    {
        if (!type.IsValueType || Nullable.GetUnderlyingType(type) != null)
            return type;

        return typeof(Nullable<>).MakeGenericType(type);
    }

    private static string FormatTypeName(Type type)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        return nullableUnderlying == null
            ? type.Name
            : $"{nullableUnderlying.Name}?";
    }

    private static bool ReferencesAlias(IrExpression expression, string alias)
    {
        return AliasRefExtractor.Extract(expression).Any(candidate =>
            string.Equals(candidate, alias, StringComparison.OrdinalIgnoreCase));
    }
}
