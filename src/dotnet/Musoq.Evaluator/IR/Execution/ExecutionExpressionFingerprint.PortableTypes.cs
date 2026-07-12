namespace Musoq.Evaluator.IR.Execution;

internal static partial class ExecutionExpressionFingerprint
{
    internal static string ForAggregateType(ExecutionTypeRef type) => ForAggregateType(type.ClrType);
}
