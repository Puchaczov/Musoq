namespace Musoq.Plugins.Attributes;

/// <summary>
/// Registers a count kernel whose independently owned partial states can be merged equivalently.
/// This declaration explicitly grants scheduling permission; merge-method discovery grants none.
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = true)]
public sealed class CountAggregateFunctionAttribute : AggregateFunctionAttribute
{
    /// <summary>Registers a concrete count kernel and its explicit execution permissions.</summary>
    public CountAggregateFunctionAttribute(Type kernelType) : base(kernelType)
    {
        Inline = true;
        EmptyResultBehavior = AggregateEmptyResultBehavior.Zero;
        ExecutionPermissions = FunctionExecutionPermissions.ConcurrentInvocations |
            FunctionExecutionPermissions.IndependentGroups | FunctionExecutionPermissions.EquivalentPartialMerge;
    }
}
