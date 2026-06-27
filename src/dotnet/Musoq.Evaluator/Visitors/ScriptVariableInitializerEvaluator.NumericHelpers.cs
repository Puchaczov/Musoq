using System.Globalization;
using Musoq.Parser.Diagnostics;

namespace Musoq.Evaluator.Visitors;

internal static partial class ScriptVariableInitializerEvaluator
{
    private static ScriptConstantEvaluationResult EvaluateNumeric(
        object left,
        object right,
        ConstantOperatorKind operation)
    {
        if (!ConstantOperatorEvaluator.IsNumeric(left) || !ConstantOperatorEvaluator.IsNumeric(right))
            return InvalidOperandFailure("numeric expression");

        try
        {
            var leftValue = Convert.ToDecimal(left, CultureInfo.InvariantCulture);
            var rightValue = Convert.ToDecimal(right, CultureInfo.InvariantCulture);
            var result = ConstantOperatorEvaluator.EvaluateDecimalArithmetic(operation, leftValue, rightValue);
            return ScriptConstantEvaluationResult.Evaluated(result, typeof(decimal));
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException or DivideByZeroException)
        {
            return ScriptConstantEvaluationResult.Failed(
                DiagnosticCode.MQ3065_InvalidScriptVariableInitializer,
                $"Script variable initializer numeric expression failed: {ex.Message}");
        }
    }

    private static ScriptConstantEvaluationResult EvaluateBitwise(
        object? left,
        object? right,
        ConstantOperatorKind operation)
    {
        if (left == null || right == null)
            return ScriptConstantEvaluationResult.Evaluated(null, typeof(object));

        if (!ConstantOperatorEvaluator.IsInteger(left) || !ConstantOperatorEvaluator.IsInteger(right))
            return InvalidOperandFailure("integer bitwise expression");

        var leftValue = Convert.ToInt64(left, CultureInfo.InvariantCulture);
        var rightValue = Convert.ToInt64(right, CultureInfo.InvariantCulture);
        return ScriptConstantEvaluationResult.Evaluated(
            ConstantOperatorEvaluator.EvaluateIntegralBitwise(operation, leftValue, rightValue),
            typeof(long));
    }

    private static ScriptConstantEvaluationResult EvaluateShift(
        object? left,
        object? right,
        ConstantOperatorKind operation)
    {
        if (left == null || right == null)
            return ScriptConstantEvaluationResult.Evaluated(null, typeof(object));

        if (!ConstantOperatorEvaluator.IsInteger(left) || !ConstantOperatorEvaluator.IsInteger(right))
            return InvalidOperandFailure("integer shift expression");

        var leftValue = Convert.ToInt64(left, CultureInfo.InvariantCulture);
        var rightValue = Convert.ToInt32(right, CultureInfo.InvariantCulture);
        return ScriptConstantEvaluationResult.Evaluated(
            ConstantOperatorEvaluator.EvaluateIntegralShift(operation, leftValue, rightValue),
            typeof(long));
    }
}
