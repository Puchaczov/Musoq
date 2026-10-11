namespace Musoq.Plugins;

/// <summary>Declares execution strategies permitted by a function implementation.</summary>
[Flags]
public enum FunctionExecutionPermissions
{
    /// <summary>Requires serial invocation.</summary>
    None = 0,
    /// <summary>Allows concurrent invocations with independently owned state.</summary>
    ConcurrentInvocations = 1,
    /// <summary>Allows independent groups to execute concurrently.</summary>
    IndependentGroups = 2,
    /// <summary>Allows independent window partitions to execute concurrently.</summary>
    IndependentPartitions = 4,
    /// <summary>Allows partial-state merging with equivalent results and failures.</summary>
    EquivalentPartialMerge = 8
}
