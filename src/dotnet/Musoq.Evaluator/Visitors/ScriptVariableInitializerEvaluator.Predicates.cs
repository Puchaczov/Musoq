using System.Collections.Generic;
using Musoq.Parser.Nodes;

namespace Musoq.Evaluator.Visitors;

internal static partial class ScriptVariableInitializerEvaluator
{
    private static ScriptConstantEvaluationResult EvaluateAnd(object? left, object? right)
    {
        if (left is bool leftValue && right is bool rightValue)
            return ScriptConstantEvaluationResult.Evaluated(leftValue && rightValue, typeof(bool));

        return InvalidOperandFailure("boolean AND");
    }

    private static ScriptConstantEvaluationResult EvaluateOr(object? left, object? right)
    {
        if (left is bool leftValue && right is bool rightValue)
            return ScriptConstantEvaluationResult.Evaluated(leftValue || rightValue, typeof(bool));

        return InvalidOperandFailure("boolean OR");
    }

    private static ScriptConstantEvaluationResult EvaluateEquality(object? left, object? right)
    {
        return ScriptConstantEvaluationResult.Evaluated(ConstantOperatorEvaluator.CompareValues(left, right) == 0, typeof(bool));
    }

    private static ScriptConstantEvaluationResult EvaluateDifference(object? left, object? right)
    {
        return ScriptConstantEvaluationResult.Evaluated(ConstantOperatorEvaluator.CompareValues(left, right) != 0, typeof(bool));
    }

    private static ScriptConstantEvaluationResult EvaluateGreater(object? left, object? right)
    {
        return ScriptConstantEvaluationResult.Evaluated(ConstantOperatorEvaluator.CompareValues(left, right) > 0, typeof(bool));
    }

    private static ScriptConstantEvaluationResult EvaluateGreaterOrEqual(object? left, object? right)
    {
        return ScriptConstantEvaluationResult.Evaluated(ConstantOperatorEvaluator.CompareValues(left, right) >= 0, typeof(bool));
    }

    private static ScriptConstantEvaluationResult EvaluateLess(object? left, object? right)
    {
        return ScriptConstantEvaluationResult.Evaluated(ConstantOperatorEvaluator.CompareValues(left, right) < 0, typeof(bool));
    }

    private static ScriptConstantEvaluationResult EvaluateLessOrEqual(object? left, object? right)
    {
        return ScriptConstantEvaluationResult.Evaluated(ConstantOperatorEvaluator.CompareValues(left, right) <= 0, typeof(bool));
    }

    private static ScriptConstantEvaluationResult EvaluateNot(
        Node expression,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? parameters,
        string ownerName)
    {
        var result = EvaluateCore(expression, variables, parameters, ownerName);
        if (!result.Success)
            return result;

        return result.Value is bool boolValue
            ? ScriptConstantEvaluationResult.Evaluated(!boolValue, typeof(bool))
            : InvalidOperandFailure("boolean NOT");
    }

    private static ScriptConstantEvaluationResult EvaluateIsNull(
        IsNullNode expression,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? parameters,
        string ownerName)
    {
        var result = EvaluateCore(expression.Expression, variables, parameters, ownerName);
        if (!result.Success)
            return result;

        var value = result.Value == null;
        return ScriptConstantEvaluationResult.Evaluated(expression.IsNegated ? !value : value, typeof(bool));
    }

    private static ScriptConstantEvaluationResult EvaluateBetween(
        BetweenNode expression,
        IReadOnlyDictionary<string, ScriptVariableDefinition> variables,
        IReadOnlyDictionary<string, ScriptParameterDefinition>? parameters,
        string ownerName)
    {
        var value = EvaluateCore(expression.Expression, variables, parameters, ownerName);
        if (!value.Success)
            return value;

        var low = EvaluateCore(expression.Min, variables, parameters, ownerName);
        if (!low.Success)
            return low;

        var high = EvaluateCore(expression.Max, variables, parameters, ownerName);
        if (!high.Success)
            return high;

        var isBetween = ConstantOperatorEvaluator.CompareValues(value.Value, low.Value) >= 0 &&
                        ConstantOperatorEvaluator.CompareValues(value.Value, high.Value) <= 0;
        return ScriptConstantEvaluationResult.Evaluated(isBetween, typeof(bool));
    }
}
