using System.Globalization;
using System.Text;

namespace Musoq.Evaluator.IR.Execution;

internal static class AggregateAccumulatorFingerprint
{
    internal static void Append(StringBuilder builder, AggregateAccumulatorField accumulator)
    {
        builder
            .Append(accumulator.FieldName)
            .Append(':')
            .Append(ExecutionExpressionFingerprint.ForAggregateType(accumulator.Kernel.KernelType))
            .Append(':')
            .Append(ExecutionExpressionFingerprint.ForAggregateType(accumulator.Kernel.StateType))
            .Append(':')
            .Append(ExecutionExpressionFingerprint.ForAggregateType(accumulator.InputType))
            .Append(':')
            .Append(ExecutionExpressionFingerprint.ForAggregateType(accumulator.ResultType))
            .Append(':')
            .Append(accumulator.ParentDepth.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(accumulator.OwnerPrefixLength.ToString(CultureInfo.InvariantCulture))
            .Append(':')
            .Append(accumulator.OwnerFieldName ?? string.Empty)
            .Append(';');
    }
}
