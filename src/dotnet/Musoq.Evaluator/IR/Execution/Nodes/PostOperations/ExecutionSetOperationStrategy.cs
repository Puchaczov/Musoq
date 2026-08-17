namespace Musoq.Evaluator.IR.Execution;

public enum ExecutionSetOperationStrategy
{
    [Obsolete("Runtime-v2 uses AppendLoop, HashSet, or GeneratedEqualityLoop. This value is kept for older inspection consumers.")]
    RowComparer,
    AppendLoop,
    GeneratedEqualityLoop,
    HashSet
}
