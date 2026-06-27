using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Visitors;

internal static partial class ScriptVariableInitializerEvaluator
{
    private static ScriptConstantEvaluationResult EvaluateAdd(object? left, object? right)
    {
        if (left == null || right == null)
            return ScriptConstantEvaluationResult.Evaluated(null, typeof(object));

        if (left is string || right is string || left is char || right is char)
            return ScriptConstantEvaluationResult.Evaluated(string.Concat(left, right), typeof(string));

        return EvaluateNumeric(left, right, ConstantOperatorKind.Add);
    }

    private static ScriptConstantEvaluationResult EvaluateSubtract(object? left, object? right)
    {
        if (left == null || right == null)
            return ScriptConstantEvaluationResult.Evaluated(null, typeof(object));

        return EvaluateNumeric(left, right, ConstantOperatorKind.Subtract);
    }

    private static ScriptConstantEvaluationResult EvaluateMultiply(object? left, object? right)
    {
        if (left == null || right == null)
            return ScriptConstantEvaluationResult.Evaluated(null, typeof(object));

        return EvaluateNumeric(left, right, ConstantOperatorKind.Multiply);
    }

    private static ScriptConstantEvaluationResult EvaluateDivide(object? left, object? right)
    {
        if (left == null || right == null)
            return ScriptConstantEvaluationResult.Evaluated(null, typeof(object));

        if (ConstantOperatorEvaluator.IsZero(right))
            return ScriptConstantEvaluationResult.Failed(
                DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
                "Script variable initializer cannot divide by zero.");

        return EvaluateNumeric(left, right, ConstantOperatorKind.Divide);
    }

    private static ScriptConstantEvaluationResult EvaluateModulo(object? left, object? right)
    {
        if (left == null || right == null)
            return ScriptConstantEvaluationResult.Evaluated(null, typeof(object));

        if (ConstantOperatorEvaluator.IsZero(right))
            return ScriptConstantEvaluationResult.Failed(
                DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
                "Script variable initializer cannot divide by zero.");

        return EvaluateNumeric(left, right, ConstantOperatorKind.Modulo);
    }

    private static ScriptConstantEvaluationResult EvaluateBitwiseAnd(object? left, object? right)
    {
        return EvaluateBitwise(left, right, ConstantOperatorKind.BitwiseAnd);
    }

    private static ScriptConstantEvaluationResult EvaluateBitwiseOr(object? left, object? right)
    {
        return EvaluateBitwise(left, right, ConstantOperatorKind.BitwiseOr);
    }

    private static ScriptConstantEvaluationResult EvaluateBitwiseXor(object? left, object? right)
    {
        return EvaluateBitwise(left, right, ConstantOperatorKind.BitwiseXor);
    }

    private static ScriptConstantEvaluationResult EvaluateLeftShift(object? left, object? right)
    {
        return EvaluateShift(left, right, ConstantOperatorKind.LeftShift);
    }

    private static ScriptConstantEvaluationResult EvaluateRightShift(object? left, object? right)
    {
        return EvaluateShift(left, right, ConstantOperatorKind.RightShift);
    }
}
