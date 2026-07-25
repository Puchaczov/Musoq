using System;
using Musoq.Schema.Optimization;

namespace Musoq.Evaluator;

/// <summary>
///     Compilation options for query execution.
/// </summary>
/// <param name="parallelizationMode">The parallelization mode to use. Defaults to Full if not specified.</param>
/// <param name="useHashJoin">Whether hash join optimization should be used for eligible joins. Defaults to true.</param>
/// <param name="useSortMergeJoin">
///     Whether sort merge join optimization should be used for eligible joins. Defaults to
///     true.
/// </param>
/// <param name="useCommonSubexpressionElimination">
///     Whether common subexpression elimination (CSE) optimization should be
///     used. Defaults to true.
/// </param>
/// <param name="usePrimitiveTypeValidation">
///     Whether to validate that query expressions only use primitive types. Defaults
///     to true.
/// </param>
/// <param name="useConstantFolding">
///     Whether constant folding optimization should be used. When enabled, constant
///     expressions (e.g. 10 + 20, 'hello' + ' world') are evaluated at compile time.
///     Also detects division/modulo by zero in constant expressions. Defaults to true.
/// </param>
/// <param name="useCteParallelization">
///     Whether CTE parallelization should be used. When enabled, CTEs that do not depend
///     on each other will be executed in parallel. Defaults to true.
/// </param>
/// <param name="useCteSidecarIndexes">
///     Whether materialized CTEs should build eligible hash/keyset sidecar indexes while
///     rows are appended. Defaults to true.
/// </param>
/// <param name="sourceRuntimeSettingsResolver">
///     Resolves source runtime settings required by schemas. Defaults to an empty resolver.
/// </param>
/// <param name="instrumentationMode">
///     Controls whether generated execution includes diagnostics instrumentation. Defaults to disabled.
/// </param>
/// <param name="maxDegreeOfParallelismOverride">
///     Overrides the generated execution max degree of parallelism. Defaults to null, which uses the current machine.
/// </param>
/// <param name="forceTableResultMaterialization">
///     Whether table-mode query results should be materialized before Run returns. Defaults to false.
/// </param>
public class CompilationOptions(
    ParallelizationMode? parallelizationMode = ParallelizationMode.Full,
    bool useHashJoin = true,
    bool useSortMergeJoin = true,
    bool useCommonSubexpressionElimination = true,
    bool useConstantFolding = true,
    bool usePrimitiveTypeValidation = true,
    bool useCteParallelization = true,
    bool useCteSidecarIndexes = true,
    ISourceRuntimeSettingsResolver? sourceRuntimeSettingsResolver = null,
    QueryInstrumentationMode instrumentationMode = QueryInstrumentationMode.Disabled,
    int? maxDegreeOfParallelismOverride = null,
    bool forceTableResultMaterialization = false)
{
    /// <summary>
    ///     Gets the parallelization mode for query execution.
    /// </summary>
    public ParallelizationMode ParallelizationMode { get; } = parallelizationMode ?? ParallelizationMode.Full;

    /// <summary>
    ///     Gets a value indicating whether hash join optimization should be used for eligible joins.
    /// </summary>
    public bool UseHashJoin { get; } = useHashJoin;

    /// <summary>
    ///     Gets a value indicating whether sort merge join optimization should be used for eligible joins.
    /// </summary>
    public bool UseSortMergeJoin { get; } = useSortMergeJoin;

    /// <summary>
    ///     Gets a value indicating whether common subexpression elimination (CSE) optimization should be used.
    ///     When enabled, duplicate expressions are computed once and cached for reuse within a row.
    /// </summary>
    public bool UseCommonSubexpressionElimination { get; } = useCommonSubexpressionElimination;

    /// <summary>
    ///     Gets a value indicating whether constant folding optimization should be used.
    ///     When enabled, constant expressions are evaluated at compile time, reducing runtime work
    ///     and enabling compile-time detection of errors like division by zero.
    /// </summary>
    public bool UseConstantFolding { get; } = useConstantFolding;

    /// <summary>
    ///     Gets a value indicating whether primitive type validation should be enforced.
    ///     When enabled, query expressions (SELECT, WHERE, GROUP BY, HAVING, ORDER BY, SKIP, TAKE)
    ///     must only use primitive types (numeric, string, bool, char, DateTime, DateTimeOffset, Guid, TimeSpan, decimal).
    ///     Complex types like classes, structs, arrays, and collections are not allowed.
    /// </summary>
    public bool UsePrimitiveTypeValidation { get; } = usePrimitiveTypeValidation;

    /// <summary>
    ///     Gets a value indicating whether CTE parallelization should be used.
    ///     When enabled, CTEs that do not depend on each other will be executed in parallel.
    ///     This can improve performance for queries with multiple independent CTEs.
    /// </summary>
    public bool UseCteParallelization { get; } = useCteParallelization;

    /// <summary>
    ///     Gets a value indicating whether materialized CTEs should build eligible hash/keyset sidecar indexes
    ///     during CTE row production. Disable this option to use the legacy post-materialization
    ///     hash/keyset build shape.
    /// </summary>
    public bool UseCteSidecarIndexes { get; } = useCteSidecarIndexes;

    public ISourceRuntimeSettingsResolver SourceRuntimeSettingsResolver { get; } =
        sourceRuntimeSettingsResolver ?? EmptySourceRuntimeSettingsResolver.Instance;

    public QueryInstrumentationMode InstrumentationMode { get; } = instrumentationMode;

    /// <summary>
    ///     Gets the generated execution max degree of parallelism override.
    ///     Null preserves the default runtime behavior and resolves parallelism from the current machine.
    /// </summary>
    public int? MaxDegreeOfParallelismOverride { get; } = maxDegreeOfParallelismOverride switch
    {
        null => null,
        > 0 => maxDegreeOfParallelismOverride,
        _ => throw new ArgumentOutOfRangeException(
            nameof(maxDegreeOfParallelismOverride),
            maxDegreeOfParallelismOverride,
            "Max degree of parallelism override must be a positive integer.")
    };

    /// <summary>
    ///     Gets a value indicating whether table-mode query results should be materialized before Run returns.
    /// </summary>
    public bool ForceTableResultMaterialization { get; } = forceTableResultMaterialization;

    public RecursiveCteExecutionLimits RecursiveCteLimits { get; private init; } = new();

    public bool UsesDefaultSourceRuntimeSettingsResolver =>
        ReferenceEquals(SourceRuntimeSettingsResolver, EmptySourceRuntimeSettingsResolver.Instance);

    public CompilationOptions WithInstrumentationMode(QueryInstrumentationMode mode) =>
        Clone(instrumentationMode: mode);

    public CompilationOptions WithTableResultMaterialization(bool force = true) =>
        Clone(forceTableResultMaterialization: force);

    public CompilationOptions WithRecursiveCteLimits(RecursiveCteExecutionLimits limits)
    {
        ArgumentNullException.ThrowIfNull(limits);

        return Clone(recursiveCteLimits: limits);
    }

    private CompilationOptions Clone(
        QueryInstrumentationMode? instrumentationMode = null,
        bool? forceTableResultMaterialization = null,
        RecursiveCteExecutionLimits? recursiveCteLimits = null)
    {
        return new CompilationOptions(
            ParallelizationMode,
            UseHashJoin,
            UseSortMergeJoin,
            UseCommonSubexpressionElimination,
            UseConstantFolding,
            UsePrimitiveTypeValidation,
            UseCteParallelization,
            UseCteSidecarIndexes,
            SourceRuntimeSettingsResolver,
            instrumentationMode ?? InstrumentationMode,
            MaxDegreeOfParallelismOverride,
            forceTableResultMaterialization ?? ForceTableResultMaterialization)
        {
            RecursiveCteLimits = recursiveCteLimits ?? RecursiveCteLimits
        };
    }
}
