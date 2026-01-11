namespace Musoq.Evaluator;

/// <summary>
/// Compilation options for query execution.
/// </summary>
/// <param name="parallelizationMode">The parallelization mode to use. Defaults to Full if not specified.</param>
/// <param name="useHashJoin">Whether hash join optimization should be used for eligible joins. Defaults to true.</param>
/// <param name="useSortMergeJoin">Whether sort merge join optimization should be used for eligible joins. Defaults to true.</param>
/// <param name="useCommonSubexpressionElimination">Whether common subexpression elimination (CSE) optimization should be used. Defaults to true.</param>
/// <param name="usePrimitiveTypeValidation">Whether to validate that query expressions only use primitive types. Defaults to true.</param>
public class CompilationOptions(
    ParallelizationMode? parallelizationMode = null, 
    bool useHashJoin = true, 
    bool useSortMergeJoin = true, 
    bool useCommonSubexpressionElimination = true,
    bool usePrimitiveTypeValidation = true)
{
    /// <summary>
    /// Gets the parallelization mode for query execution.
    /// </summary>
    public ParallelizationMode ParallelizationMode { get; } = parallelizationMode ?? ParallelizationMode.Full;
    
    /// <summary>
    /// Gets a value indicating whether hash join optimization should be used for eligible joins.
    /// </summary>
    public bool UseHashJoin { get; } = useHashJoin;

    /// <summary>
    /// Gets a value indicating whether sort merge join optimization should be used for eligible joins.
    /// </summary>
    public bool UseSortMergeJoin { get; } = useSortMergeJoin;

    /// <summary>
    /// Gets a value indicating whether common subexpression elimination (CSE) optimization should be used.
    /// When enabled, duplicate expressions are computed once and cached for reuse within a row.
    /// </summary>
    public bool UseCommonSubexpressionElimination { get; } = useCommonSubexpressionElimination;

    /// <summary>
    /// Gets a value indicating whether primitive type validation should be enforced.
    /// When enabled, query expressions (SELECT, WHERE, GROUP BY, HAVING, ORDER BY, SKIP, TAKE)
    /// must only use primitive types (numeric, string, bool, char, DateTime, DateTimeOffset, Guid, TimeSpan, decimal).
    /// Complex types like classes, structs, arrays, and collections are not allowed.
    /// </summary>
    public bool UsePrimitiveTypeValidation { get; } = usePrimitiveTypeValidation;
}