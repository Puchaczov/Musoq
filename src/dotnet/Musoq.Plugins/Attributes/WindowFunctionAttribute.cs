namespace Musoq.Plugins.Attributes;

/// <summary>
///     Declares a SQL window signature and its registered typed implementation operations.
///     Declaration stubs are never invoked during execution. Unregistered legacy factories
///     remain temporary migration contracts.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class WindowFunctionAttribute : BindableMethodAttribute
{
    /// <summary>Creates a declaration configured through named properties.</summary>
    public WindowFunctionAttribute() { }

    /// <summary>Registers a typed kernel with explicitly selected evaluation modes and empty behavior.</summary>
    public WindowFunctionAttribute(Type kernelType, WindowEvaluationModes modes,
        AggregateEmptyResultBehavior emptyResultBehavior)
    {
        KernelType = kernelType;
        Modes = modes;
        EmptyResultBehavior = emptyResultBehavior;
    }

    /// <summary>
    ///     The SQL function name used with the OVER clause.
    ///     When null, the method name is used as the SQL name.
    ///     Use this to map a C# declaration method name to a different SQL function name
    ///     (e.g., when the desired SQL name collides with an existing aggregate method).
    /// </summary>
    public string? Name { get; set; }

    /// <summary>Gets or sets the concrete typed implementation kernel.</summary>
    public Type? KernelType { get; set; }

    /// <summary>Gets or sets an explicit concrete state type for typed accumulation kernels.</summary>
    public Type? StateType { get; set; }

    /// <summary>Gets or sets the peer context contract used by a typed ranking kernel.</summary>
    public WindowRankingKind RankingKind { get; set; }

    /// <summary>Gets or sets the registered typed frame context definition for a direct Get operation.</summary>
    public Type? ContextType { get; set; }

    /// <summary>Gets or sets whether the declaration requires an ordered partition.</summary>
    public bool RequiresOrderBy { get; set; }

    /// <summary>Declares that the operation evaluates partition position/size independently of SQL frame bounds.</summary>
    public bool IgnoresFrame { get; set; }

    /// <summary>Gets or sets whether ranking operations require peer bounds and dense rank, in addition to position and size.</summary>
    public bool RequiresPeerMetadata { get; set; } = true;

    /// <summary>Authorizes ordinal QUALIFY bounds only when results are the standard positive
    /// row number, rank or dense rank for the declared ranking kind.</summary>
    public bool SupportsOrdinalUpperBound { get; set; }

    /// <summary>Gets or sets an optional typed TryEvaluate batch implementation with a serial-equivalence fallback contract.</summary>
    public Type? BatchKernelType { get; set; }

    /// <summary>Gets or sets the evaluation modes implemented by the typed kernel.</summary>
    public WindowEvaluationModes Modes { get; set; }

    /// <summary>Gets or sets the result behavior of an empty frame.</summary>
    public AggregateEmptyResultBehavior EmptyResultBehavior { get; set; } = AggregateEmptyResultBehavior.Custom;

}
