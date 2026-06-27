namespace Musoq.Evaluator.Visitors;

internal static partial class ScriptVariableInitializerEvaluator
{
    private static ScriptConstantEvaluationResult EvaluateIsDistinctFrom(object? left, object? right)
    {
        return ScriptConstantEvaluationResult.Evaluated(IsDistinctFrom(left, right), typeof(bool));
    }

    private static ScriptConstantEvaluationResult EvaluateIsNotDistinctFrom(object? left, object? right)
    {
        return ScriptConstantEvaluationResult.Evaluated(!IsDistinctFrom(left, right), typeof(bool));
    }

    private static bool IsDistinctFrom(object? left, object? right)
    {
        if (left is null)
            return right is not null;

        if (right is null)
            return true;

        return ConstantOperatorEvaluator.CompareValues(left, right) != 0;
    }
}
