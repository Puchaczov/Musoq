using System.Collections.Generic;

namespace Musoq.Evaluator.IR.Execution;

public sealed partial class PhysicalToExecutionPlanBuilder
{
    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyCaseWhen(
        ExecutionCaseWhen caseWhen,
        string rightAlias)
    {
        var branches = new List<ExecutionCaseWhenBranch>(caseWhen.Branches.Count);
        var hasNullResult = false;
        foreach (var branch in caseWhen.Branches)
        {
            var condition = SubstituteOuterApplyRightAlias(branch.Condition, rightAlias);
            if (!condition.Supported)
                return condition;

            if (condition.IsUnknown)
                continue;

            var conditionExpression = NormalizeOuterApplyBooleanOperand(condition.Expression);
            if (!conditionExpression.Supported)
                return OuterApplyNullSubstitutionResult.Unsupported(conditionExpression.UnsupportedReason);

            var result = SubstituteOuterApplyRightAlias(branch.Result, rightAlias);
            if (!result.Supported)
                return result;

            var resultExpression = result.IsUnknown
                ? CreateOuterApplyNullLiteral(caseWhen.ReturnType)
                : result.Expression;
            hasNullResult |= result.IsUnknown;

            branches.Add(new ExecutionCaseWhenBranch(conditionExpression.Value, resultExpression));
        }

        var elseExpression = SubstituteOuterApplyCaseElse(caseWhen.ElseExpression, rightAlias);
        if (!elseExpression.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(elseExpression.UnsupportedReason);

        var resolvedElseExpression = elseExpression.IsUnknown
            ? CreateOuterApplyNullLiteral(caseWhen.ReturnType)
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
                ? LiftOuterApplyNullSubstitutionType(caseWhen.ReturnType)
                : caseWhen.ReturnType
        });
    }

    private static OuterApplyCaseElseSubstitutionResult SubstituteOuterApplyCaseElse(
        ExecutionExpression? elseExpression,
        string rightAlias)
    {
        if (elseExpression == null)
            return OuterApplyCaseElseSubstitutionResult.Known(null);

        var substituted = SubstituteOuterApplyRightAlias(elseExpression, rightAlias);
        if (!substituted.Supported)
            return OuterApplyCaseElseSubstitutionResult.Unsupported(substituted.UnsupportedReason);

        return substituted.IsUnknown
            ? OuterApplyCaseElseSubstitutionResult.Unknown()
            : OuterApplyCaseElseSubstitutionResult.Known(substituted.Expression);
    }

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyCoalesce(
        ExecutionCoalesce coalesce,
        string rightAlias)
    {
        var expressions = new List<ExecutionExpression>(coalesce.Expressions.Count);

        foreach (var expression in coalesce.Expressions)
        {
            var substituted = SubstituteOuterApplyRightAlias(expression, rightAlias);
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

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyCompositeKey(
        ExecutionCompositeKey compositeKey,
        string rightAlias)
    {
        var parts = SubstituteOuterApplyArguments(compositeKey.Parts, rightAlias);
        if (!parts.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(parts.UnsupportedReason);

        return parts.HasUnknown
            ? OuterApplyNullSubstitutionResult.Unknown()
            : OuterApplyNullSubstitutionResult.Known(compositeKey with { Parts = parts.Expressions });
    }

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyValueTupleKey(
        ExecutionValueTupleKey valueTupleKey,
        string rightAlias)
    {
        var parts = SubstituteOuterApplyArguments(valueTupleKey.Parts, rightAlias);
        if (!parts.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(parts.UnsupportedReason);

        return parts.HasUnknown
            ? OuterApplyNullSubstitutionResult.Unknown()
            : OuterApplyNullSubstitutionResult.Known(valueTupleKey with { Parts = parts.Expressions });
    }

    private static OuterApplyNullSubstitutionResult SubstituteOuterApplyAggregateCall(
        ExecutionAggregateCall aggregateCall,
        string rightAlias)
    {
        var arguments = SubstituteOuterApplyArguments(aggregateCall.Arguments, rightAlias);
        if (!arguments.Supported)
            return OuterApplyNullSubstitutionResult.Unsupported(arguments.UnsupportedReason);

        return arguments.HasUnknown
            ? OuterApplyNullSubstitutionResult.Unknown()
            : OuterApplyNullSubstitutionResult.Known(aggregateCall with { Arguments = arguments.Expressions });
    }

    private static OuterApplyArgumentSubstitutionResult SubstituteOuterApplyArguments(
        IReadOnlyList<ExecutionExpression> arguments,
        string rightAlias)
    {
        var expressions = new List<ExecutionExpression>(arguments.Count);
        var hasUnknown = false;

        foreach (var argument in arguments)
        {
            var substituted = SubstituteOuterApplyRightAlias(argument, rightAlias);
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
}
