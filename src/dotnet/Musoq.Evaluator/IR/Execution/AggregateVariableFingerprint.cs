namespace Musoq.Evaluator.IR.Execution;

internal static class AggregateVariableFingerprint
{
    internal static string Create(
        ExecutionVariable variable,
        ExecutionParallelSingleKeyAggregateLoop parallelAggregate)
    {
        if (string.Equals(variable.Name, parallelAggregate.Source.Name, StringComparison.Ordinal))
            return $"$source:{ExecutionExpressionFingerprint.ForAggregateType(variable.Type)}";

        if (string.Equals(variable.Name, parallelAggregate.Group.Name, StringComparison.Ordinal))
            return $"$group:{ExecutionExpressionFingerprint.ForAggregateType(variable.Type)}";

        if (string.Equals(variable.Name, parallelAggregate.RootGroup.Name, StringComparison.Ordinal))
            return $"$root:{ExecutionExpressionFingerprint.ForAggregateType(variable.Type)}";

        if (string.Equals(variable.Name, parallelAggregate.GroupsToFinalize.Name, StringComparison.Ordinal))
            return $"$groupsToFinalize:{ExecutionExpressionFingerprint.ForAggregateType(variable.Type)}";

        return $"{variable.Name}:{ExecutionExpressionFingerprint.ForAggregateType(variable.Type)}";
    }
}
