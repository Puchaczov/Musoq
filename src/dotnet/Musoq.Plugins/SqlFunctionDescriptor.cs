using System.Collections.Immutable;
using System.Reflection;

namespace Musoq.Plugins;

/// <summary>Immutable compilation/discovery metadata for a library SQL declaration.</summary>
public sealed record SqlFunctionDescriptor
{
    /// <summary>Gets the SQL function name.</summary>
    public required string Name { get; init; }
    /// <summary>Gets the SQL evaluation context.</summary>
    public required SqlFunctionKind Kind { get; init; }
    /// <summary>Gets declaration metadata; never a per-row reflection invocation target.</summary>
    public required MethodInfo Declaration { get; init; }
    /// <summary>Gets SQL-visible arguments.</summary>
    public required ImmutableArray<SqlFunctionParameter> Parameters { get; init; }
    /// <summary>Gets the declared SQL result type.</summary>
    public required Type ResultType { get; init; }
    /// <summary>Gets public or compiler-only binding visibility.</summary>
    public required FunctionVisibility Visibility { get; init; }
    /// <summary>Gets explicitly permitted execution strategies.</summary>
    public FunctionExecutionPermissions ExecutionPermissions { get; init; }
    /// <summary>Gets the registered typed kernel, or null for a scalar or temporary legacy declaration.</summary>
    public Type? KernelType { get; init; }
    /// <summary>Gets advertised typed window modes.</summary>
    public WindowEvaluationModes WindowModes { get; init; }
    /// <summary>Gets the concrete peer context contract of a typed ranking declaration.</summary>
    public WindowRankingKind RankingKind { get; init; }
    /// <summary>Gets the registered concrete window context definition.</summary>
    public Type? WindowContextType { get; init; }
    /// <summary>Gets an optional explicitly registered typed batch implementation.</summary>
    public Type? WindowBatchKernelType { get; init; }
    /// <summary>Gets whether the SQL declaration requires ordering inside OVER.</summary>
    public bool RequiresWindowOrdering { get; init; }
    /// <summary>Gets whether a ranking operation requests peer bounds and dense rank.</summary>
    public bool RequiresWindowPeerMetadata { get; init; } = true;
    /// <summary>Gets aggregate or frame empty-result semantics.</summary>
    public AggregateEmptyResultBehavior EmptyResultBehavior { get; init; } = AggregateEmptyResultBehavior.Custom;
}
