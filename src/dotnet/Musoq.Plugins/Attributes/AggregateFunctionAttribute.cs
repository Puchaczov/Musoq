namespace Musoq.Plugins.Attributes;

/// <summary>
///     Declares a SQL aggregate function and the static typed kernel used by runtime-v2 lowering.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class AggregateFunctionAttribute(Type kernelType) : AggregationMethodAttribute
{
    /// <summary>
    ///     Gets the static kernel type that owns State, Set, Get, and optional Merge members.
    /// </summary>
    public Type KernelType { get; } = kernelType ?? throw new ArgumentNullException(nameof(kernelType));

    /// <summary>
    ///     Gets or sets the SQL-facing function name. When omitted, the declaration method name is used.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    ///     Gets or sets an explicit state type when the kernel does not expose a nested State type.
    /// </summary>
    public Type? StateType { get; set; }

    /// <summary>
    ///     Gets or sets whether the renderer may inline the aggregate kernel calls in generated code.
    /// </summary>
    public bool Inline { get; set; }

    /// <summary>
    ///     Gets or sets the empty-input behavior declared by this aggregate.
    /// </summary>
    public AggregateEmptyResultBehavior EmptyResultBehavior { get; set; } = AggregateEmptyResultBehavior.Custom;
}
